using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Bizanc.io.Matching.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Bizanc.io.Matching.Infra;
using Bizanc.io.Matching.Infra.Repository;
using Bizanc.io.Matching.Infra.Connector;
using System.Net.Sockets;

namespace Bizanc.io.Matching.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static Miner StartMiner()
        {
            var miner = new Miner(new PeerListener(), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            new WithdrawalRepository(), new TradeRepository(), new WithdrawInfoRepository(),
            new CryptoConnector());

            miner.Start(true).Wait();

            var seednode = new Peer(new TcpClient("localhost", 3001));
            miner.Connect(seednode);

            return miner;
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder()
                .UseUrls("http://0.0.0.0:5000")
                .UseLibuv(c => c.ThreadCount = 4)
                .ConfigureServices(servicesCollection =>
                    {
                        servicesCollection.AddSingleton<IChainRepository>(StartMiner());
                    })
                .UseStartup<Startup>()
                .Build();


        public static void Start(IChainRepository repository)
        {
            WebHost.CreateDefaultBuilder()
                .UseUrls("http://0.0.0.0:5001")
                .ConfigureServices(servicesCollection =>
                    {
                        servicesCollection.AddSingleton<IChainRepository>(repository);
                    })
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
