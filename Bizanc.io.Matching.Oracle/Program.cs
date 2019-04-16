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

namespace Bizanc.io.Matching.Oracle
{
    class Program
    {
        private static string privateKeyEth = "0x2952a82db5058f4b61cb283db47a767cf8f7157fe0144b9575b1b117495241f3";

        private static async void StartApi(Miner miner)
        {
            await Task.Run(() =>
            {
                Api.Program.Start(miner);
            });
        }

        private static WithdrawInfoRepository repository;

        private static async void WithdrawListener(Miner miner, CryptoConnector connector)
        {
            var withdrwawalDictionary = new Dictionary<string, Withdrawal>();
            var ethConnector = new EthereumOracleConnector();

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
                                        result = await connector.WithdrawBtc(wd.HashStr, wd.TargetWallet, wd.Size);
                                    else
                                        result = await ethConnector.WithdrawEth(privateKeyEth, wd.HashStr, wd.TargetWallet, wd.Size, wd.Asset);

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
            repository = new WithdrawInfoRepository();
            var miner = new Miner(new PeerListener(), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            new WithdrawalRepository(), new TradeRepository(), repository,
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
                var seednode = new Peer(new TcpClient("localhost", 3001));
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
