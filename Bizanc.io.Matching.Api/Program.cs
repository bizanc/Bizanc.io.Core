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
using Serilog;
using Serilog.Events;

namespace Bizanc.io.Matching.Api
{
    class NodeConfig
    {
        public string Network { get; set; }
        public int ListenPort { get; set; }
        public string SeedAddress { get; set; }
        public int SeedPort { get; set; }
        public string OracleBTCAddres { get; set; }
        public string OracleETHAddres { get; set; }
        public string BTCEndpoint { get; set; }
        public string ETHEndpoint { get; set; }
        public bool Mine { get; set; }
        public string ApiEndpoint { get; set; }
    }

    public class Program
    {

        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static Miner StartMiner(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddCommandLine(args);

            IConfigurationRoot configuration = builder.Build();

            var conf = new NodeConfig();
            configuration.GetSection("Node").Bind(conf);
            conf.ToString();

            var miner = new Miner(new PeerListener(conf.ListenPort), new WalletRepository(),
             new BlockRepository(), new BalanceRepository(), new BookRepository(),
             new DepositRepository(), new OfferRepository(), new TransactionRepository(),
             new WithdrawalRepository(), new TradeRepository(), new WithdrawInfoRepository(),
             new CryptoConnector(conf.OracleETHAddres, conf.OracleBTCAddres, conf.ETHEndpoint, conf.BTCEndpoint, conf.Network), conf.ListenPort);

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
                        servicesCollection.AddSingleton<IChainRepository>(StartMiner(args));
                    })
                .UseSerilog()
                .UseStartup<Startup>()
                .Build();


        public static void Start(IChainRepository repository, string endpoint)
        {
            WebHost.CreateDefaultBuilder()
                .UseUrls(endpoint)
                .ConfigureServices(servicesCollection =>
                    {
                        servicesCollection.AddSingleton<IChainRepository>(repository);
                    })
                .UseStartup<Startup>()
                .UseSerilog()
                .Build()
                .Run();
        }
    }
}
