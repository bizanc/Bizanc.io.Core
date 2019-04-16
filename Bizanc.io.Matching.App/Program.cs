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

namespace Bizanc.io.Matching.App
{
    class Program
    {
        private static async void StartApi(Miner miner)
        {
            await Task.Factory.StartNew(() =>
            {
                Api.Program.Start(miner);
            }, TaskCreationOptions.LongRunning);
        }

        static async Task Main()
        {
            var miner = new Miner(new PeerListener(), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            new WithdrawalRepository(), new TradeRepository(), new WithdrawInfoRepository(),
            new CryptoConnector());

            await miner.Start();

            StartApi(miner);

            var master = Environment.GetEnvironmentVariable("MASTER");
            if (master != null)
            {
                if (master.ToUpper() != "TRUE")
                {
                    try
                    {
                        Console.WriteLine("Connectig to masternode...");
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
                // var seednode = new Peer(new TcpClient("seed.bizanc.io", 5556));
                // miner.Connect(seednode);
    
                // var seednode = new Peer(new TcpClient("localhost", 3001));
                // miner.Connect(seednode);

                // try
                // {
                //     var seednode = new Peer(new TcpClient("bizanc.io", 443));    
                //     await miner.Connect(seednode);
                // }
                // catch (Exception e)
                // {
                //     Console.WriteLine("Failed to conn"ect seed: \n"+e.ToString());
                // }

            }

            await Task.Delay(Timeout.Infinite);
        }
    }
}
