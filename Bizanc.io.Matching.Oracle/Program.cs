using System;
using Bizanc.io.Matching.Core.Domain;
using System.Threading.Tasks;
using Bizanc.io.Matching.Infra;
using Bizanc.io.Matching.Core.Domain.Messages;
using System.Net.Sockets;
using System.Threading;
using Bizanc.io.Matching.Core.Crypto;
using Bizanc.io.Matching.Infra.Repository;
using Bizanc.io.Matching.Infra.Connector;
using System.Collections.Generic;
using Bizanc.io.Matching.Oracle.Repository;

namespace Bizanc.io.Matching.Oracle
{
    class Program
    {
        private static async void StartApi(Miner miner)
        {
            await Task.Run(() =>
            {
                Api.Program.Start(miner);
            });
        }

        private static async void WithdrawListener(Miner miner, CryptoConnector connector)
        {
            var withdrwawalDictionary = new Dictionary<string, Withdrawal>();
            var repository = new WithdrawInfoRepository();
            await Task.Run(async () =>
            {
                while (true)
                {
                    var withdrawals = await miner.ListWithdrawals(100, 5);
                    if (withdrawals.Count > 0)
                    {
                        foreach (var wd in withdrawals)
                        {
                            try
                            {
                                if (!withdrwawalDictionary.ContainsKey(wd.HashStr) && !(await repository.Contains(wd.HashStr)))
                                {
                                    WithdrawInfo result = null;

                                    if (wd.Asset == "BTC")
                                        result = await connector.WithdrawBtc(wd.TargetWallet, wd.Size);
                                    else
                                        result = await connector.WithdrawEth(wd.TargetWallet, wd.Size, wd.Asset);

                                    if (result != null)
                                    {
                                        withdrwawalDictionary.Add(wd.HashStr, wd);
                                        result.HashStr = wd.HashStr;
                                        await repository.Save(result);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Failed to withdrawal... \n" + e.ToString());
                            }
                        }
                    }

                    Thread.Sleep(1000);
                }
            });
        }

        async static Task Main()
        {
            var CryptoConnector = new CryptoConnector();
            var miner = new Miner(new PeerListener(), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            new WithdrawalRepository(), new TradeRepository(),
            CryptoConnector);

            await miner.Start(true);

            WithdrawListener(miner, CryptoConnector);

            var master = Environment.GetEnvironmentVariable("MASTER");
            if (master != null)
            {
                if (master.ToUpper() != "TRUE")
                {
                    try
                    {
                        var seednode = new Peer(new TcpClient("masternode", 5556));
                        miner.Connect(seednode);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            else
            {
                Console.WriteLine("No Master Variable");
                var seednode = new Peer(new TcpClient("localhost", 5556));
                miner.Connect(seednode);

                // try
                // {
                //     var seednode = new Peer(new TcpClient("bizanc.io", 443));
                //     await miner.Connect(seednode);
                // }
                // catch (Exception e)
                // {
                //     Console.WriteLine("Failed to connect seed: \n"+e.ToString());
                // }

            }

            await Task.Delay(Timeout.Infinite);
        }
    }
}
