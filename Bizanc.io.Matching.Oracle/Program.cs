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
        public bool Reprocess { get; set; } = false;
    }

    class PKCS
    {
        public string User { get; set; }
        public string BtcKey { get; set; }
        public string EthKey { get; set; }
    }

    class Program
    {
        private static WithdrawInfoRepository repository;

        private static ChannelReader<Chain> reader;

        private static EthereumOracleConnector ethConnector;

        private static BitcoinOracleConnector btcConnector;

        private static Dictionary<string, Withdrawal> withdrwawalDictionary = new Dictionary<string, Withdrawal>();

        private static async Task<bool> ProcessWithdraw(Withdrawal wd)
        {
            WithdrawInfo result = new WithdrawInfo();
            if (await repository.Contains(wd.HashStr))
                result = await repository.Get(wd.HashStr);
            else
            {
                result.Asset = wd.Asset;
                result.HashStr = wd.HashStr;
                result.Timestamp = DateTime.Now;
                await repository.Save(result);
            }

            if (wd.Asset == "BTC")
                result.TxHash = await btcConnector.WithdrawBtc(wd.HashStr, wd.TargetWallet, wd.Size);
            else
                await ethConnector.WithdrawEth(wd.HashStr, wd.TargetWallet, wd.Size, wd.Asset);

            result.Status = WithdrawStatus.Sent;

            withdrwawalDictionary.Add(wd.HashStr, wd);
            await repository.Save(result);
            return true;
        }

        private static async void WithdrawListener(Miner miner, CryptoConnector connector, NodeConfig conf)
        {
            var withdrwawalDictionary = new Dictionary<string, Withdrawal>();

            while (await reader.WaitToReadAsync())
            {
                var chain = await reader.ReadAsync();
                if (chain != null)
                {
                    foreach (var wd in chain.CurrentBlock.Withdrawals)
                    {
                        try
                        {
                            if (!withdrwawalDictionary.ContainsKey(wd.HashStr) && !(await repository.Contains(wd.HashStr)))
                                await ProcessWithdraw(wd);
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
                .AddJsonFile("pkcscfg.json", optional: false, reloadOnChange: true)
                .AddCommandLine(args);


            IConfigurationRoot configuration = builder.Build();
            Log.Debug("Building Config");

            var conf = new NodeConfig();
            configuration.GetSection("Node").Bind(conf);
            var pkcsConf = new PKCS();
            configuration.GetSection("PKCS").Bind(pkcsConf);
            Log.Debug("NodeConfig created");

            repository = new WithdrawInfoRepository();
            var wdRepository = new WithdrawalRepository();
            var connector = new CryptoConnector(conf.OracleETHAddres, conf.OracleBTCAddres, conf.ETHEndpoint, conf.BTCEndpoint, conf.Network);
            var miner = new Miner(new PeerListener(conf.ListenPort), new WalletRepository(),
            new BlockRepository(), new BalanceRepository(), new BookRepository(),
            new DepositRepository(), new OfferRepository(), new TransactionRepository(),
            wdRepository, new TradeRepository(), repository,
            connector, conf.ListenPort,true, 1000, true);

            ethConnector = new EthereumOracleConnector(conf.ETHEndpoint, conf.OracleETHAddres, pkcsConf.User, pkcsConf.EthKey);
            btcConnector = new BitcoinOracleConnector(conf.Network, conf.BTCEndpoint, pkcsConf.User, pkcsConf.BtcKey);

            if (conf.Reprocess)
            {
                Log.Information("Reprocessing withdraws: ");
                foreach (var wdid in await repository.ListToReprocess())
                {
                    Log.Information("Reprocessing withdraw: " + wdid);
                    var wd = await wdRepository.Get(wdid);
                    
                    await ProcessWithdraw(wd);
                }
            }

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


