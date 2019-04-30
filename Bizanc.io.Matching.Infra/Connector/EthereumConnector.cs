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
using Serilog;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class EthereumConnector
    {
        private static Web3Geth web3; 

        private static string abi = @"[ { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' }, { 'name': 'token', 'type': 'address' } ], 'name': 'depositERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' }, { 'name': 'token', 'type': 'address' } ], 'name': 'withdrawERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' } ], 'name': 'withdrawEth', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'denyAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'allowAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' } ], 'name': 'depositEth', 'outputs': [], 'payable': true, 'stateMutability': 'payable', 'type': 'function' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'from', 'type': 'address' }, { 'indexed': false, 'name': 'destination', 'type': 'string' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logDeposit', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'withdrawHash', 'type': 'string' }, { 'indexed': false, 'name': 'to', 'type': 'address' }, { 'indexed': false, 'name': 'origin', 'type': 'address' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logWithdrawal', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'AllowAccessEvent', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'DenyAccessEvent', 'type': 'event' } ]";
        private static Contract contract;
        private static Event logDepositEvent;
        private static Event logWithdrawEvent;
        private Nethereum.Hex.HexTypes.HexBigInteger currentBlockDeposits = null;
        private Nethereum.Hex.HexTypes.HexBigInteger currentBlockWithdraws = null;

        private NewFilterInput depositFilter;

        private NewFilterInput withdrawFilter;


        public EthereumConnector(string oracleAddress, string endpoint){
            web3 = new Web3Geth(endpoint);
            contract = web3.Eth.GetContract(abi, oracleAddress);
            logDepositEvent = contract.GetEvent("logDeposit");
            logWithdrawEvent = contract.GetEvent("logWithdrawal");
        }

        public async Task<List<Deposit>> StartupDeposits(string blockNumber)
        {
            List<Deposit> deposits = null;
            try
            {
                var startupLog = new List<Nethereum.Contracts.EventLog<Bizanc.io.Matching.Infra.Connector.LogDepositEvent>>();

                var parameter = BlockParameter.CreateEarliest();

                if (!string.IsNullOrEmpty(blockNumber))
                {
                    Log.Information("Reading Eth Events from block " + blockNumber);
                    currentBlockDeposits = new HexBigInteger(blockNumber);
                    parameter = new BlockParameter(currentBlockDeposits);
                }

                var lastBlock = new HexBigInteger((await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value - 5);
                depositFilter = logDepositEvent.CreateFilterInput(parameter, new BlockParameter(lastBlock));
                deposits = await GetDeposits(depositFilter);
                currentBlockDeposits = lastBlock;
            }
            catch (Exception e)
            {
                Log.Error("Ethereum Connector Startp Failed: " + e.ToString());
            }

            return deposits;
        }

        public async Task<List<WithdrawInfo>> StartupWithdraws(string blockNumber)
        {
            List<WithdrawInfo> withdraws = null;
            try
            {
                var startupLog = new List<Nethereum.Contracts.EventLog<Bizanc.io.Matching.Infra.Connector.LogDepositEvent>>();

                var parameter = BlockParameter.CreateEarliest();

                if (!string.IsNullOrEmpty(blockNumber))
                {
                    Log.Information("Reading Eth Events from block " + blockNumber);
                    currentBlockWithdraws = new HexBigInteger(blockNumber);
                    parameter = new BlockParameter(currentBlockWithdraws);
                }

                var lastBlock = new HexBigInteger(await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                withdrawFilter = logWithdrawEvent.CreateFilterInput(parameter, new BlockParameter(lastBlock));
                withdraws = await GetWithdraws(withdrawFilter);
                currentBlockWithdraws = lastBlock;
            }
            catch (Exception e)
            {
                Log.Error("Ethereum Connector Startp Failed: " + e.ToString());
            }

            return withdraws;
        }

        private async Task<List<Deposit>> GetDeposits(NewFilterInput filter)
        {
            var deposits = new List<Deposit>();
            var log = await logDepositEvent.GetAllChanges<LogDepositEvent>(filter);

            if (log.Count > 0)
            {
                foreach (var d in log)
                {
                    if (d.Event.AssetId == "0x0000000000000000000000000000000000000000"
                        || d.Event.AssetId == "0x7a094dfd89893d204436bf331a51d80f5c48a2eb")
                    {
                        var deposit = new Deposit();
                        deposit.TargetWallet = d.Event.Destination;
                        deposit.Asset = d.Event.Symbol;
                        deposit.AssetId = d.Event.AssetId;
                        deposit.Quantity = Web3Geth.Convert.FromWei(d.Event.Value);
                        deposit.TxHash = d.Log.TransactionHash;
                        deposit.BlockNumber = d.Log.BlockNumber.HexValue;
                        deposit.Timestamp = DateTime.Now;

                        deposits.Add(deposit);
                    }
                }
            }

            return deposits;
        }

        private async Task<List<WithdrawInfo>> GetWithdraws(NewFilterInput filter)
        {
            var withdraws = new List<WithdrawInfo>();
            var log = await logWithdrawEvent.GetAllChanges<LogWithdrawalEvent>(filter);

            if (log.Count > 0)
            {
                foreach (var d in log)
                {
                    var withdraw = new WithdrawInfo();
                    withdraw.HashStr = d.Event.WithdrawHash;
                    withdraw.TxHash = d.Log.TransactionHash;
                    withdraw.BlockNumber = d.Log.BlockNumber.HexValue;
                    withdraw.Timestamp = DateTime.Now;
                    withdraw.Asset = d.Event.Symbol;
                    withdraws.Add(withdraw);
                }
            }

            return withdraws;
        }

        public async Task<List<Deposit>> GetDeposits()
        {
            depositFilter.FromBlock = new BlockParameter(currentBlockDeposits);
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            lastBlock = new HexBigInteger(lastBlock.Value - 5);
            depositFilter.ToBlock = new BlockParameter(lastBlock);
            var result = await GetDeposits(depositFilter);
            currentBlockDeposits = lastBlock;
            return result;
        }

        public async Task<List<WithdrawInfo>> GetWithdaws()
        {
            withdrawFilter.FromBlock = new BlockParameter(currentBlockWithdraws);
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            withdrawFilter.ToBlock = new BlockParameter(lastBlock);
            var result = await GetWithdraws(withdrawFilter);
            currentBlockWithdraws = lastBlock;
            return result;
        }
    }

    [Event("logDeposit")]
    public class LogDepositEvent
    {
        [Parameter("address", "from", 1, false)]
        public string Sender { get; set; }

        [Parameter("string", "destination", 2, false)]
        public string Destination { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger Value { get; set; }

        [Parameter("string", "asset", 4, false)]
        public string Symbol { get; set; }

        [Parameter("address", "assetId", 5, false)]
        public string AssetId { get; set; }
    }

    [Event("logWithdrawal")]
    public class LogWithdrawalEvent
    {
        [Parameter("string", "withdrawHash", 1, false)]
        public string WithdrawHash { get; set; }

        [Parameter("address", "to", 2, false)]
        public string Destination { get; set; }

        [Parameter("address", "origin", 3, false)]
        public string Sender { get; set; }

        [Parameter("uint256", "amount", 4, false)]
        public BigInteger Value { get; set; }

        [Parameter("string", "asset", 5, false)]
        public string Symbol { get; set; }

        [Parameter("address", "assetId", 6, false)]
        public string AssetId { get; set; }
    }
}