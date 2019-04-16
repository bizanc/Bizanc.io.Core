using System;
using System.Numerics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Connector;
using Bizanc.io.Matching.Core.Util;
using System.Threading;
using NBitcoin;
using QBitNinja.Client;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NBitcoin.Policy;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class BitcoinConnector
    {
        private BitcoinSecret testAddress = new BitcoinSecret("cThzd6sBfn1Cq2VG5XUDg1LETHCKWGFcCuyXAXvSA4fHiSiuc4Xy");

        private QBitNinjaClient QClient = new QBitNinjaClient(Network.TestNet);

        public async Task<List<Deposit>> GetBtcDeposit()
        {
            return await Task.Run<List<Deposit>>(delegate
            {
                var deposits = new List<Deposit>();
                var txOperations = QClient.GetBalance(dest: testAddress).Result.Operations;
                var isDeposit = false;

                foreach (var op in txOperations)
                {
                    if (op.SpentCoins.Any(c => ((Coin)c).ScriptPubKey.ToString().Contains(testAddress.PubKeyHash.ToString())))
                    {
                        var tx = QClient.GetTransaction(op.TransactionId).Result;
                        foreach (var coin in tx.ReceivedCoins)
                        {
                            if (!isDeposit && coin.Amount.Equals(Money.Zero))
                            {
                                var script = coin.GetScriptCode().ToString();
                                script = script.Replace("OP_RETURN ", "");
                                var wd = HexToString(script);
                            }
                        }
                        continue;
                    }

                    var address = "";
                    var transaction = QClient.GetTransaction(op.TransactionId).Result;
                    foreach (var coin in transaction.ReceivedCoins)
                    {
                        if (!isDeposit && coin.Amount.Equals(Money.Zero))
                        {
                            var script = coin.GetScriptCode().ToString();
                            script = script.Replace("OP_RETURN ", "");
                            address = HexToString(script);
                            isDeposit = true;
                        }
                    }

                    if (isDeposit)
                    {
                        var deposit = new Deposit();
                        deposit.TargetWallet = address;
                        deposit.Asset = "BTC";
                        deposit.AssetId = "0x0";
                        deposit.Quantity = op.Amount.ToDecimal((MoneyUnit)100000000);
                        deposit.TxHash = op.TransactionId.ToString();
                        deposit.Timestamp = DateTime.Now;
                        deposit.BlockNumber = transaction.Block.Height.ToString();
                        deposits.Add(deposit);
                    }
                }

                return deposits;
            });
        }

        public async Task<WithdrawInfo> WithdrawBtc(string withdrawHash, string recipient, decimal amount)
        {
            return await Task.Run<WithdrawInfo>(delegate
            {
                var txOperations = QClient.GetBalance(dest: testAddress, unspentOnly: true).Result.Operations;

                var coins = new List<Coin>();

                foreach (var op in txOperations)
                    op.ReceivedCoins.ForEach(c => coins.Add((Coin)c));

                coins.Sort(delegate (Coin x, Coin y)
                {
                    return -x.Amount.CompareTo(y.Amount);
                });

                var coinSum = 0m;

                for (int i = 0; coinSum < amount; i++)
                {
                    coinSum += coins[i].Amount.ToDecimal(MoneyUnit.BTC);
                    if (coinSum >= amount)
                        coins.RemoveRange(i + 1, coins.Count - (i + 1));
                }

                var builder = new TransactionBuilder();
                var destination = new BitcoinPubKeyAddress(recipient, Network.TestNet);
                NBitcoin.Transaction tx = builder
                                    .AddCoins(coins)
                                    .AddKeys(testAddress)
                                    .Send(destination, Money.Coins(amount))
                                    .Send(TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(withdrawHash)), Money.Zero)
                                    .SetChange(testAddress)
                                    .SendFees(Money.Coins(0.0001m))
                                    .BuildTransaction(sign: true);

                TransactionPolicyError[] errors = null;
                if (builder.Verify(tx, out errors))
                {
                    var broadcastResult = QClient.Broadcast(tx).Result;
                    broadcastResult.ToString();
                    return new WithdrawInfo() { TxHash = tx.GetHash().ToString(), Timestamp = DateTime.Now };
                }

                if (errors != null)
                {
                    errors.ToString();
                }

                return null;
            });
        }

        public string DepositBtc(string btcPubKey, string recipient, decimal amount)
        {
            var address = new BitcoinPubKeyAddress(btcPubKey, Network.TestNet);
            //var address = new BitcoinSecret(btcPubKey);
            var txOperations = QClient.GetBalance(dest: address, unspentOnly: true).Result.Operations;

            var coins = new List<Coin>();

            foreach (var op in txOperations)
                op.ReceivedCoins.ForEach(c => coins.Add((Coin)c));

            coins.Sort(delegate (Coin x, Coin y)
            {
                return -x.Amount.CompareTo(y.Amount);
            });

            var coinSum = 0m;

            for (int i = 0; coinSum < amount; i++)
            {
                coinSum += coins[i].Amount.ToDecimal(MoneyUnit.BTC);
                if (coinSum >= amount)
                    coins.RemoveRange(i + 1, coins.Count - (i + 1));
            }

            var builder = new TransactionBuilder();
            var destination = new BitcoinPubKeyAddress(recipient, Network.TestNet);
            NBitcoin.Transaction tx = builder
                                .AddCoins(coins)
                                //.AddKeys(testAddress)
                                .Send(destination, Money.Coins(amount))
                                .SetChange(address)
                                .SendFees(Money.Coins(0.0001m))
                                .BuildTransaction(sign: false);

            tx.Outputs.Add(new TxOut
            {
                Value = Money.Zero,
                ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(recipient))
            });

            foreach (var input in tx.Inputs)
            {
                input.ScriptSig = address.ScriptPubKey;
            }

            var txhash = tx.ToHex();

            if (builder.Verify(tx))
            {
                var broadcastResult = QClient.Broadcast(tx).Result;
            }

            return tx.ToHex();
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