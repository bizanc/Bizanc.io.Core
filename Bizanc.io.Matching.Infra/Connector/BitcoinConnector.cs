using System;
using System.Numerics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Connector;
using Bizanc.io.Matching.Core.Util;
using System.Threading;
using NBitcoin;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NBitcoin.Policy;
using NBXplorer;
using NBXplorer.Models;
using System.Threading.Channels;
using Serilog;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class BitcoinConnector
    {
        private AddressTrackedSource oracleAddress;

        ExplorerClient client;
        WebsocketNotificationSession session;

        private int? lastBlockDeposits = 0;
        private int? lastBlockWithdraws = 0;

        private Channel<Deposit> depositStream;
        private Channel<WithdrawInfo> withdrawStream;
        private NetworkType network;
        private CancellationToken cancel;
        public BitcoinConnector(string oracleAddress, string endpoint, Channel<Deposit> depositStream, Channel<WithdrawInfo> withdrawStream, string  network)
        {
            this.depositStream = depositStream;
            this.withdrawStream = withdrawStream;
            this.oracleAddress = TrackedSource.Create(new BitcoinPubKeyAddress(oracleAddress, Network.Main));
            this.network = network == "testnet" ? NetworkType.Testnet : NetworkType.Mainnet;
            client = new ExplorerClient(new NBXplorerNetworkProvider(this.network).GetBTC(), new Uri(endpoint));
            client.SetNoAuth();
        }

        public async Task<(List<Deposit>, List<WithdrawInfo>)> Start(string blockNumberDeposits, string blockNumberWithdraws)
        {
            cancel = new CancellationToken();
            return await LoadOps(blockNumberDeposits);
        }

        private async Task<(List<Deposit>, List<WithdrawInfo>)> LoadOps(string blockNumberDeposits)
        {
            var listenerStarted = false;

            while (!listenerStarted)
            {
                try
                {
                    await client.WaitServerStartedAsync();
                    session = await client.CreateWebsocketNotificationSessionAsync();
                    session.ListenTrackedSources(new[] { oracleAddress });
                    ProcessNotifications();
                    listenerStarted = true;
                }
                catch (Exception e)
                {
                    Log.Error("Failed to stablish bitcoin websocket connector ");
                    Log.Error(e.ToString());
                    await Task.Delay(3000);
                }
            }


            var opLoaded = false;

            while (!opLoaded)
            {
                try
                {
                    var oldTx = (await client.GetTransactionsAsync(oracleAddress)).ConfirmedTransactions.Transactions;

                    opLoaded = true;
                    int heigth = 0;
                    if (!string.IsNullOrEmpty(blockNumberDeposits))
                        heigth = int.Parse(blockNumberDeposits);

                    if (heigth > 0)
                    {
                        oldTx = oldTx.Where(t => t.Height > heigth).ToList();
                        return ProcessOperations(oldTx);
                    }
                    else
                        return ProcessOperations(oldTx);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to load transactions");
                    Log.Error(e.ToString());

                    if (opLoaded)
                        throw;

                    await Task.Delay(3000);
                }
            }

            return (new List<Deposit>(), new List<WithdrawInfo>());
        }

        private async void ProcessNotifications()
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var evt = (NewTransactionEvent)(await session.NextEventAsync(cancel));

                    if (evt.TransactionData.Confirmations > 0)
                        WaitConfirmations(evt.TransactionData.TransactionHash);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to retrieve from websocket: \n" + e.ToString());
                    break;
                }
            }

            if (!cancel.IsCancellationRequested)
            {
                await LoadOps(this.lastBlockDeposits.ToString());
            }
        }

        private async void WaitConfirmations(uint256 tx)
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var op = await client.GetTransactionAsync(oracleAddress, tx, cancel);

                    if (op.BalanceChange < Money.Zero)
                    {
                        await withdrawStream.Writer.WriteAsync(ProcessWithdraw(op));
                        return;
                    }
                    else if (op.Confirmations >= 3)
                    {
                        await depositStream.Writer.WriteAsync(ProcessDeposit(op));
                        return;
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failde to load transaction confirmation: " + e.ToString());
                }
                await Task.Delay(3000);
            }
        }

        public (List<Deposit>, List<WithdrawInfo>) ProcessOperations(List<TransactionInformation> txOperations)
        {
            var deposits = new List<Deposit>();
            var withdraws = new List<WithdrawInfo>();

            foreach (var op in txOperations)
                if (op.BalanceChange > Money.Zero)
                {
                    if (op.Confirmations >= 3)
                        deposits.Add(ProcessDeposit(op));
                    else
                        WaitConfirmations(op.TransactionId);
                }
                else if (op.BalanceChange < Money.Zero)
                    withdraws.Add(ProcessWithdraw(op));

            return (deposits, withdraws);
        }

        private Deposit ProcessDeposit(TransactionInformation transaction)
        {
            try
            {
                var deposit = new Deposit();
                deposit.TargetWallet = GetScriptString(transaction.Transaction);
                deposit.Asset = "BTC";
                deposit.AssetId = "0x0";
                deposit.Quantity = transaction.BalanceChange.ToDecimal(MoneyUnit.BTC);
                deposit.TxHash = transaction.TransactionId.ToString();
                deposit.Timestamp = DateTime.Now;
                deposit.BlockNumber = transaction.Height.ToString();
                lastBlockDeposits = transaction.Height;
                return deposit;
            }
            catch (Exception e)
            {
                Log.Error("Failed to process deposit : " + e.ToString());
            }

            return null;
        }

        public WithdrawInfo ProcessWithdraw(TransactionInformation transaction)
        {
            try
            {
                var withdraw = new WithdrawInfo();
                withdraw.HashStr = GetScriptString(transaction.Transaction);
                withdraw.TxHash = transaction.TransactionId.ToString();
                withdraw.BlockNumber = transaction.Height.ToString();
                withdraw.Timestamp = DateTime.Now;
                withdraw.Asset = "BTC";
                withdraw.Status = WithdrawStatus.Confirmed;
                lastBlockWithdraws = transaction.Height;
                return withdraw;
            }
            catch (Exception e)
            {
                Log.Error("Failed to process withdraw : " + e.ToString());
            }

            return null;
        }

        private string GetScriptString(NBitcoin.Transaction transaction)
        {
            var output = transaction.Outputs.FirstOrDefault(c => c.Value.Equals(Money.Zero));
            if (output == null)
                transaction.ToString(); //TODO: UnCOmment exception
            //throw new Exception("Transaction without integration output:  " + transaction.GetHash().ToString());
            else
            {
                try
                {
                    var coin = new NBitcoin.Coin(transaction, output);
                    var script = coin.GetScriptCode().ToString();
                    script = script.Replace("OP_RETURN ", "");
                    return HexToString(script);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to process transaction output", e);
                }
            }

            return "";
        }

        public static string HexToString(string InputText)
        {

            byte[] bb = Enumerable.Range(0, InputText.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(InputText.Substring(x, 2), 16))
                             .ToArray();
            return System.Text.Encoding.UTF8.GetString(bb);
        }
    }
}