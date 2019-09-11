using System;
using System.Linq;
using System.Collections.Generic;
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
using Microsoft.Extensions.Configuration;
using System.IO;
using Serilog;
using Serilog.Events;
using System.Threading.Channels;
using Amazon.CloudHSM;
using SimpleBase;
using NBitcoin;
using Nethereum.Web3.Accounts;

namespace Bizanc.io.Matching.Oracle
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

    class Program
    {
        private static WithdrawInfoRepository repository;

        private static ChannelReader<Chain> reader;

        private static async void WithdrawListener(Miner miner, CryptoConnector connector, NodeConfig conf)
        {
            var withdrwawalDictionary = new Dictionary<string, Withdrawal>();
            var ethConnector = new EthereumOracleConnector(conf.ETHEndpoint, conf.OracleETHAddres);
            var btcConnector = new BitcoinOracleConnector(conf.Network, conf.BTCEndpoint);

            while (await reader.WaitToReadAsync())
            {
                var chain = await reader.ReadAsync();
                chain = chain.Get(5);
                if (chain != null)
                {
                    foreach (var wd in chain.CurrentBlock.Withdrawals)
                    {
                        try
                        {
                            if (!withdrwawalDictionary.ContainsKey(wd.HashStr) && !(await repository.Contains(wd.HashStr)))
                            {
                                WithdrawInfo result = null;

                                if (wd.Asset == "BTC")
                                    result = await btcConnector.WithdrawBtc(wd.HashStr, wd.TargetWallet, wd.Size);
                                else
                                    result = await ethConnector.WithdrawEth(wd.HashStr, wd.TargetWallet, wd.Size, wd.Asset);

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
                            Log.Error("Failed to withdrawal... \n" + e.ToString());
                        }
                    }
                }
            }
        }

        async static Task Main(string[] args)
        {
            Console.WriteLine("Starting Oracle");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddCommandLine(args);


            IConfigurationRoot configuration = builder.Build();
            Console.WriteLine("Building Config");

            var conf = new NodeConfig();
            configuration.GetSection("Node").Bind(conf);
            Console.WriteLine("NodeConfig created");

            // var btcConnector = new BitcoinOracleConnector(conf.Network, conf.BTCEndpoint);
            // Console.WriteLine("BTCConnector created");

            // await btcConnector.WithdrawBtc("0xabc", "1HtGPy2cHwFFy5DnQkHXzCXBYt7iGmUrzi", 0.0005m);
            // Console.WriteLine("withdraw made");

            // var ethConnector = new EthereumOracleConnector(conf.ETHEndpoint, conf.OracleETHAddres);
            // Console.WriteLine("ETHConnector created");

            // await ethConnector.WithdrawEth("0xabc", "0x6Bc94245f365C721F4285E06Bc97a5E999Cd816C", 0.0005m, "ETH");
            // Console.WriteLine("withdraw made");

            repository = new WithdrawInfoRepository();
            var connector = new CryptoConnector(conf.OracleETHAddres, conf.OracleBTCAddres, conf.ETHEndpoint, conf.BTCEndpoint, conf.Network);
            var miner = new Miner(new PeerListener(conf.ListenPort), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            new WithdrawalRepository(), new TradeRepository(), repository,
            connector, conf.ListenPort);

            reader = miner.GetChainStream();

            await miner.Start(true);

            WithdrawListener(miner, connector, conf);

            if (!string.IsNullOrEmpty(conf.SeedAddress) && conf.SeedPort > 0)
            {
                try
                {
                    var seednode = new Peer(new TcpClient(conf.SeedAddress, conf.SeedPort));
                    miner.Connect(seednode);
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    throw;
                }
            }
            await Task.Delay(Timeout.Infinite);
        }
    }
}


