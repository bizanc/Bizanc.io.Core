using System;
using System.Numerics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Connector;
using Bizanc.io.Matching.Core.Util;
using Nethereum.Geth;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Threading;
using Nethereum.Hex.HexTypes;
using NBitcoin;
using QBitNinja.Client;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class CryptoConnector : IConnector
    {
        private EthereumConnector ethConnector = new EthereumConnector();
        private BitcoinConnector btcConnector = new BitcoinConnector();
        private Channel<Deposit> stream;
        public ChannelReader<Deposit> GetChannelReader()
        {
            return stream.Reader;
        }

        public CryptoConnector()
        {
            stream = Channel.CreateUnbounded<Deposit>();
        }

        public async Task<IEnumerable<Deposit>> Start()
        {
            var eth = await LoadETH();
            var btc = await LoadBTC();
            ProcessEth();
            ProcessBtc();

            eth.AddRange(btc);
            return eth;
        }

        private async Task<List<Deposit>> LoadETH()
        {
            var ethStartup = await ethConnector.Startup();

            while (ethStartup == null)
                ethStartup = await ethConnector.Startup();

            Console.WriteLine(ethStartup.Count + " ETH deposits loaded");

            return ethStartup;
        }

        private async Task<List<Deposit>> LoadBTC()
        {
            List<Deposit> btcDeposits = null;

            while (btcDeposits == null)
            {
                try
                {
                    Console.WriteLine("Reading BTC Deposits.....");
                    btcDeposits = await btcConnector.GetBtcDeposit();
                    Console.WriteLine(btcDeposits.Count + " BTC Deposits found.");
                    return btcDeposits;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

            return new List<Deposit>();
        }

        private async void ProcessEth()
        {
            var success = true;
            try
            {
                Console.WriteLine("Reading ETH Deposits.....");
                var ethDeposits = await ethConnector.GetEthDeposit();
                Console.WriteLine(ethDeposits.Count + " ETH Deposits found.");
                ethDeposits.ForEach(async d => await stream.Writer.WriteAsync(d));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                success = false;
            }

            if (success)
                await Task.Delay(15000);

            ProcessEth();
        }

        private async void ProcessBtc()
        {
            var success = true;
            try
            {
                var btcDeposits = await LoadBTC();
                btcDeposits.ForEach(async d => await stream.Writer.WriteAsync(d));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                success = false;
            }

            if (success)
                await Task.Delay(15000);

            ProcessBtc();
        }

        public async Task<WithdrawInfo> WithdrawEth(string recipient, decimal amount, string symbol)
        {
            return await ethConnector.WithdrawEth(recipient, amount, symbol);
        }

        public async Task<WithdrawInfo> WithdrawBtc(string recipient, decimal amount)
        {
            return await btcConnector.WithdrawBtc(recipient, amount);
        }

        public string DepositBtc(string btcPubKey, string recipient, decimal amount)
        {
            return btcConnector.DepositBtc(btcPubKey, recipient, amount);
        }
    }
}