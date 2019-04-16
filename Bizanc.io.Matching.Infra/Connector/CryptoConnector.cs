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
        private Channel<Deposit> depositStream;
        private Channel<WithdrawInfo> withdrawStream;
        public ChannelReader<Deposit> GetDepositsReader() => depositStream.Reader;
        public ChannelReader<WithdrawInfo> GetWithdrawsReader() => withdrawStream.Reader;

        public CryptoConnector()
        {
            depositStream = Channel.CreateUnbounded<Deposit>();
            withdrawStream = Channel.CreateUnbounded<WithdrawInfo>();
        }

        public async Task<IEnumerable<Deposit>> StartDeposits(string ethBlockNumber)
        {
            var eth = await LoadETHDeposits(ethBlockNumber);
            //var btc = await LoadBTC();
            ProcessEthDeposits();
            //ProcessBtc();

            //eth.AddRange(btc);
            return eth;
        }

        private async Task<List<Deposit>> LoadETHDeposits(string blockNumber)
        {
            var ethStartup = await ethConnector.StartupDeposits(blockNumber);

            while (ethStartup == null)
                ethStartup = await ethConnector.StartupDeposits(blockNumber);

            Console.WriteLine(ethStartup.Count + " ETH deposits loaded");

            return ethStartup;
        }

        private async void ProcessEthDeposits()
        {
            while (true)
            {
                var success = true;
                try
                {
                    Console.WriteLine("Reading ETH Deposits.....");
                    var ethDeposits = await ethConnector.GetEthDeposit();
                    Console.WriteLine(ethDeposits.Count + " ETH Deposits found.");
                    ethDeposits.ForEach(async d => await depositStream.Writer.WriteAsync(d));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    success = false;
                    await Task.Delay(2000);
                }

                if (success)
                    await Task.Delay(3000);
            }
        }

        public async Task<IEnumerable<WithdrawInfo>> StartWithdraws(string ethBlockNumber)
        {
            var eth = await LoadETHWithdraws(ethBlockNumber);
            //var btc = await LoadBTC();
            ProcessEthWithdraws();
            //ProcessBtc();

            //eth.AddRange(btc);
            return eth;
        }

        private async Task<List<WithdrawInfo>> LoadETHWithdraws(string blockNumber)
        {
            var ethStartup = await ethConnector.StartupWithdraws(blockNumber);

            while (ethStartup == null)
                ethStartup = await ethConnector.StartupWithdraws(blockNumber);

            Console.WriteLine(ethStartup.Count + " ETH withdraws loaded");

            return ethStartup;
        }



        private async void ProcessEthWithdraws()
        {
            while (true)
            {
                var success = true;
                try
                {
                    Console.WriteLine("Reading ETH Withdraws.....");
                    var ethWithdaws = await ethConnector.GetEthWithdaws();
                    Console.WriteLine(ethWithdaws.Count + " ETH Withdraws found.");
                    ethWithdaws.ForEach(async d => await withdrawStream.Writer.WriteAsync(d));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    success = false;
                    await Task.Delay(2000);
                }

                if (success)
                    await Task.Delay(3000);
            }
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

        private async void ProcessBtc()
        {
            while (true)
            {
                var success = true;
                try
                {
                    var btcDeposits = await LoadBTC();
                    btcDeposits.ForEach(async d => await depositStream.Writer.WriteAsync(d));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    success = false;
                    await Task.Delay(2000);
                }

                if (success)
                    await Task.Delay(15000);
            }
        }

        public async Task<WithdrawInfo> WithdrawBtc(string withdrawHash, string recipient, decimal amount)
        {
            return await btcConnector.WithdrawBtc(withdrawHash, recipient, amount);
        }

        public string DepositBtc(string btcPubKey, string recipient, decimal amount)
        {
            return btcConnector.DepositBtc(btcPubKey, recipient, amount);
        }
    }
}