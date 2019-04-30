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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Serilog;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class CryptoConnector : IConnector
    {
        private EthereumConnector ethConnector;
        private BitcoinConnector btcConnector;
        private Channel<Deposit> depositStream;
        private Channel<WithdrawInfo> withdrawStream;
        public ChannelReader<Deposit> GetDepositsReader() => depositStream.Reader;
        public ChannelReader<WithdrawInfo> GetWithdrawsReader() => withdrawStream.Reader;

        public CryptoConnector(string oracleETHAddress, string oracleBTCAddress, string ETHEndpoint, string BTCEndpoint)
        {
            depositStream = Channel.CreateUnbounded<Deposit>();
            withdrawStream = Channel.CreateUnbounded<WithdrawInfo>();
            btcConnector = new BitcoinConnector(oracleBTCAddress, BTCEndpoint, depositStream, withdrawStream);
            ethConnector = new EthereumConnector(oracleETHAddress, ETHEndpoint);
        }

        public async Task<(IEnumerable<Deposit>, IEnumerable<WithdrawInfo>)> Start(string ethDepositBlockNumber, string ethWithdrawBlockNumber, string btcDepositBlockNumber, string btcWithdrawBlockNumber)
        {
            var deposits = new List<Deposit>();
            var withdraws = new List<WithdrawInfo>();

            deposits.AddRange(await LoadETHDeposits(ethDepositBlockNumber));
            withdraws.AddRange(await LoadETHWithdraws(ethWithdrawBlockNumber));
            var (d, w) = await LoadBTC(btcDepositBlockNumber, btcWithdrawBlockNumber);

            deposits.AddRange(d);
            withdraws.AddRange(w);
            ProcessETHDeposits();
            ProcessETHWithdraws();

            return (deposits, withdraws);
        }

        private async Task<List<Deposit>> LoadETHDeposits(string blockNumber)
        {
            var ethStartup = await ethConnector.StartupDeposits(blockNumber);

            while (ethStartup == null)
                ethStartup = await ethConnector.StartupDeposits(blockNumber);

            Log.Information(ethStartup.Count + " ETH deposits loaded");

            return ethStartup;
        }

        private async Task<(List<Deposit>, List<WithdrawInfo>)> LoadBTC(string btcDepositBlockNumber, string btcWithdrawBlockNumber)
        {
            var (deposits, withdraws) = await btcConnector.Start(btcDepositBlockNumber, btcWithdrawBlockNumber);

            while (deposits == null)
                (deposits, withdraws) = await btcConnector.Start(btcDepositBlockNumber, btcWithdrawBlockNumber);

            Log.Information(deposits.Count + " BTC deposits loaded");
            Log.Information(withdraws.Count + " BTC withdraws loaded");

            return (deposits, withdraws);
        }

        private async void ProcessETHDeposits()
        {
            while (true)
            {
                var success = true;
                try
                {
                    var ethDeposits = await ethConnector.GetDeposits();
                    ethDeposits.ForEach(async d => await depositStream.Writer.WriteAsync(d));
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    success = false;
                    await Task.Delay(2000);
                }

                if (success)
                    await Task.Delay(3000);
            }
        }

        private async Task<List<WithdrawInfo>> LoadETHWithdraws(string blockNumber)
        {
            var ethStartup = await ethConnector.StartupWithdraws(blockNumber);

            while (ethStartup == null)
                ethStartup = await ethConnector.StartupWithdraws(blockNumber);

            Log.Information(ethStartup.Count + " ETH withdraws loaded");

            return ethStartup;
        }

        private async void ProcessETHWithdraws()
        {
            while (true)
            {
                var success = true;
                try
                {
                    var ethWithdaws = await ethConnector.GetWithdaws();
                    ethWithdaws.ForEach(async d => await withdrawStream.Writer.WriteAsync(d));
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                    success = false;
                    await Task.Delay(2000);
                }

                if (success)
                    await Task.Delay(3000);
            }
        }
    }
}