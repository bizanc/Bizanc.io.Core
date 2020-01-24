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
using Microsoft.Extensions.Configuration;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Core;

namespace Bizanc.io.Matching.App
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
        public int Threads { get; set; }
        public bool PersistState { get; set; } = true;
        public int PersistStateInterval { get; set; } = 1000;

        public bool PersistQueryData { get; set; } = false;
        public string ApiEndpoint { get; set; }
        public string MinerAddress { get; set; }

        public string RavenDB { get; set; }
    }
    class Program
    {
        private static async void StartApi(Miner miner, string endpoint)
        {
            await Task.Factory.StartNew(() =>
            {
                Api.Program.Start(miner, endpoint);
            }, TaskCreationOptions.LongRunning);
        }

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddCommandLine(args);

            IConfigurationRoot configuration = builder.Build();

            var conf = new NodeConfig();
            configuration.GetSection("Node").Bind(conf);

            var miner = new Miner(new PeerListener(conf.ListenPort), new WalletRepository(conf.RavenDB),
            new BlockRepository(conf.RavenDB), new BalanceRepository(conf.RavenDB), new BookRepository(conf.RavenDB),
            new DepositRepository(conf.RavenDB), new OfferRepository(conf.RavenDB), new TransactionRepository(conf.RavenDB),
            new WithdrawalRepository(conf.RavenDB), new TradeRepository(conf.RavenDB), new WithdrawInfoRepository(conf.RavenDB),
            new CryptoConnector(conf.OracleETHAddres, conf.OracleBTCAddres, conf.ETHEndpoint, conf.BTCEndpoint, conf.Network), conf.ListenPort,
            conf.PersistState, conf.PersistStateInterval, conf.PersistQueryData, conf.Threads);

            await miner.Start(!conf.Mine, conf.MinerAddress);

            if (!string.IsNullOrEmpty(conf.SeedAddress) && conf.SeedPort > 0)
            {
                try
                {
                    Log.Information("Connecting to peer: " + conf.SeedAddress + ":" + conf.SeedPort);
                    var seednode = new Peer(new TcpClient(conf.SeedAddress, conf.SeedPort));
                    miner.StartSynch();
                    miner.Connect(seednode);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            StartApi(miner, conf.ApiEndpoint);
            await miner.StartListener();

            await Task.Delay(Timeout.Infinite);
        }
    }
}
