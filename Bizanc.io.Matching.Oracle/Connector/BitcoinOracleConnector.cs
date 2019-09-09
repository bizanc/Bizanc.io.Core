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

namespace Bizanc.io.Matching.Infra.Connector
{
    public class BitcoinOracleConnector
    {
        private BitcoinSecret wallet;

        private ExplorerClient client;

        private NetworkType network;

        private Dictionary<string, UTXO> usedInputs = new Dictionary<string, UTXO>();

        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        public BitcoinOracleConnector(string network, string endpoint, string secret)
        {
            this.network = network == "testnet" ? NetworkType.Testnet : NetworkType.Mainnet;
            client = new ExplorerClient(new NBXplorerNetworkProvider(this.network).GetBTC(), new Uri(endpoint));
            wallet = new BitcoinSecret(secret);
        }

        public async Task<WithdrawInfo> WithdrawBtc(string withdrawHash, string recipient, decimal amount)
        {
            var coins = new List<UTXO>();
            var coinsUsed = new List<UTXO>();

            try
            {
                await locker.WaitAsync();

                while (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) < amount)
                {
                    var txOperations = await client.GetUTXOsAsync(TrackedSource.Create(wallet.GetAddress()));
                    foreach (var op in txOperations.Confirmed.UTXOs)
                    {
                        if (!usedInputs.ContainsKey(op.TransactionHash.ToString() + op.Value.Satoshi.ToString()))
                            coins.Add(op);
                    }

                    coins.Sort(delegate (UTXO x, UTXO y)
                    {
                        return -x.AsCoin().Amount.CompareTo(y.AsCoin().Amount);
                    });

                    foreach (var item in coins)
                    {
                        if (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) < amount)
                        {
                            coinsUsed.Add(item);
                            usedInputs.Add(item.TransactionHash.ToString() + item.Value.Satoshi.ToString(), item);
                        }
                        else
                            break;
                    }

                    if (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) < amount)
                        await Task.Delay(5000);
                }

                TransactionBuilder builder = null;
                BitcoinPubKeyAddress destination = null;
                if (network == NetworkType.Testnet)
                {
                    builder = Network.TestNet.CreateTransactionBuilder();
                    destination = new BitcoinPubKeyAddress(recipient, Network.TestNet);
                }
                else
                {
                    builder = Network.Main.CreateTransactionBuilder();
                    destination = new BitcoinPubKeyAddress(recipient, Network.Main);
                }

                NBitcoin.Transaction tx = builder
                                    .AddCoins(coinsUsed.Select(c => c.AsCoin()))
                                    .AddKeys(wallet)
                                    .Send(destination, Money.Coins(amount))
                                    //SCRIPT 6a2c37325a6344445a3764346231766243626d517a4a546b63767479446d6a54547572416953764753794853787a	     0.        
                                    .Send(TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(withdrawHash)), Money.Zero)
                                    .SetChange(wallet)
                                    .SendEstimatedFees((await client.GetFeeRateAsync(3)).FeeRate)
                                    .BuildTransaction(sign: true);

                TransactionPolicyError[] errors = null;
                if (builder.Verify(tx, out errors))
                {
                    var broadcastResult = await client.BroadcastAsync(tx);
                    broadcastResult.ToString();
                    return new WithdrawInfo() { TxHash = tx.GetHash().ToString(), Timestamp = DateTime.Now };
                }

                if (errors != null)
                {
                    errors.ToString();
                }

                return null;
            }
            catch(Exception e)
            {
                e.ToString();
                return null;
            }
            finally
            {
                locker.Release();
            }
        }
    }
}