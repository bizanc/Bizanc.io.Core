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

        private static string abi = @"[ { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' }, { 'name': 'token', 'type': 'address' } ], 'name': 'depositERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' }, { 'name': 'token', 'type': 'address' } ], 'name': 'withdrawERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' } ], 'name': 'withdrawEth', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'denyAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'allowAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' } ], 'name': 'depositEth', 'outputs': [], 'payable': true, 'stateMutability': 'payable', 'type': 'function' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'from', 'type': 'address' }, { 'indexed': false, 'name': 'destination', 'type': 'string' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logDeposit', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'withdrawHash', 'type': 'string' }, { 'indexed': false, 'name': 'to', 'type': 'address' }, { 'indexed': false, 'name': 'origin', 'type': 'address' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logWithdrawal', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'AllowAccessEvent', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'DenyAccessEvent', 'type': 'event' } ]";
        private static Contract contract;
        private static Event logDepositEvent;
        private static Event logWithdrawEvent;
        private Nethereum.Hex.HexTypes.HexBigInteger currentBlockDeposits = null;
        private Nethereum.Hex.HexTypes.HexBigInteger currentBlockWithdraws = null;

        private NewFilterInput depositFilter;

        private NewFilterInput withdrawFilter;

        private Dictionary<string, string> assetDictionary = new Dictionary<string, string>();

        public EthereumConnector(string oracleAddress, string endpoint)
        {
            web3 = new Web3Geth(endpoint);
            contract = web3.Eth.GetContract(abi, oracleAddress);
            logDepositEvent = contract.GetEvent("logDeposit");
            logWithdrawEvent = contract.GetEvent("logWithdrawal");

            assetDictionary.Add("0x0000000000000000000000000000000000000000", "ETH");
            assetDictionary.Add("0xdac17f958d2ee523a2206206994597c13d831ec7", "USDT");
            assetDictionary.Add("0xF433089366899D83a9f26A773D59ec7eCF30355e", "MTL");
            assetDictionary.Add("0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48", "USDC");
            assetDictionary.Add("0x0d8775f648430679a709e98d2b0cb6250d2887ef", "BAT");
            assetDictionary.Add("0xa0b73e1ff0b80914ab6fe0444e65848c4c34450b", "CRO");
            assetDictionary.Add("0x497bAEF294c11a5f0f5Bea3f2AdB3073DB448B56", "DEX");
            assetDictionary.Add("0x0000000000085d4780B73119b644AE5ecd22b376", "TUSD");
            assetDictionary.Add("0x89d24a6b4ccb1b6faa2625fe562bdd9a23260359", "DAI");
            assetDictionary.Add("0x8e1b448ec7adfc7fa35fc2e885678bd323176e34", "EGT");
            assetDictionary.Add("0xf629cbd94d3791c9250152bd8dfbdf380e2a3b9c", "ENJ");
            assetDictionary.Add("0x6f259637dcd74c767781e37bc6133cd6a68aa161", "HT");
            assetDictionary.Add("0x17aa18a4b64a55abed7fa543f2ba4e91f2dce482", "INB");
            assetDictionary.Add("0x039b5649a59967e3e936d7471f9c3700100ee1ab", "KCS");
            assetDictionary.Add("0xbf2179859fc6D5BEE9Bf9158632Dc51678a4100e", "ELF");
            assetDictionary.Add("0xe41d2489571d322189246dafa5ebde1f4699f498", "ZRX");
            assetDictionary.Add("0x514910771af9ca656af840dff83e8264ecf986ca", "LINK");
            assetDictionary.Add("0x0f5d2fb29fb7d3cfee444a200298f468908cc942", "MANA");
            assetDictionary.Add("0x7D1AfA7B718fb893dB30A3aBc0Cfc608AaCfeBB0", "MATIC");
            assetDictionary.Add("0xb63b606ac810a52cca15e44bb630fd42d8d1d83d", "MCO");
            assetDictionary.Add("0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2", "MKR");
            assetDictionary.Add("0xbd0793332e9fb844a52a205a233ef27a5b34b927", "ZB");
            assetDictionary.Add("0xd26114cd6EE289AccF82350c8d8487fedB8A0C07", "OMG");
            assetDictionary.Add("0x8e870d67f660d95d5be530380d0ec0bd388289e1", "PAX");
            assetDictionary.Add("0xb5a73f5fc8bbdbce59bfd01ca8d35062e0dad801", "PERL");
            assetDictionary.Add("0x8971f9fd7196e5cee2c1032b50f656855af7dd26", "LAMB");
            assetDictionary.Add("0xb1eef147028e9f480dbc5ccaa3277d417d1b85f0", "SEELE");
            assetDictionary.Add("0x1985365e9f78359a9B6AD760e32412f4a445E862", "REP");
            assetDictionary.Add("0x46cc7ec70746f4cbd56ce5fa9bb7d648398eaa5c", "REALT");
            assetDictionary.Add("0x420412e765bfa6d85aaac94b4f7b708c89be2e2b", "BRZ");
            assetDictionary.Add("0x744d70fdbe2ba4cf95131626614a1763df805b9e", "SNT");
            assetDictionary.Add("0xa15c7ebe1f07caf6bff097d8a589fb8ac49ae5b3", "NXPS");
            assetDictionary.Add("0x056fd409e1d7a124bd7017459dfea2f387b6d5cd", "GUSD");
            assetDictionary.Add("0xB8c77482e45F1F44dE1745F52C74426C631bDD52", "BNB");
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
                    if (assetDictionary.ContainsKey(d.Event.AssetId))
                    {
                        decimal value = 0;
                        if(assetDictionary[d.Event.AssetId] == "ETH")
                            value = Web3Geth.Convert.FromWei(d.Event.Value);
                        else
                        {
                            var token = web3.Eth.GetContract(ERC20ABI, d.Event.AssetId);
                            var decimals = await token.GetFunction("decimals").CallAsync<BigInteger>();

                            value = Convert.ToDecimal(double.Parse(d.Event.Value.ToString()) / Math.Pow(10, double.Parse(decimals.ToString())));
                            decimals.ToString();
                        }

                        var deposit = new Deposit();
                        deposit.TargetWallet = d.Event.Destination;
                        deposit.Asset = assetDictionary[d.Event.AssetId];
                        deposit.AssetId = d.Event.AssetId;
                        deposit.Quantity = value;
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
                    if (assetDictionary.ContainsKey(d.Event.AssetId))
                    {
                        var withdraw = new WithdrawInfo();
                        withdraw.HashStr = d.Event.WithdrawHash;
                        withdraw.TxHash = d.Log.TransactionHash;
                        withdraw.BlockNumber = d.Log.BlockNumber.HexValue;
                        withdraw.Timestamp = DateTime.Now;
                        withdraw.Asset = assetDictionary[d.Event.AssetId];
                        withdraws.Add(withdraw);
                    }
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

        public string ERC20ABI = @"[
                    {
                        'constant': true,
                        'inputs': [],
                        'name': 'name',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'string'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'constant': false,
                        'inputs': [
                            {
                                'name': '_spender',
                                'type': 'address'
                            },
                            {
                                'name': '_value',
                                'type': 'uint256'
                            }
                        ],
                        'name': 'approve',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'bool'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'constant': true,
                        'inputs': [],
                        'name': 'totalSupply',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'uint256'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'constant': false,
                        'inputs': [
                            {
                                'name': '_from',
                                'type': 'address'
                            },
                            {
                                'name': '_to',
                                'type': 'address'
                            },
                            {
                                'name': '_value',
                                'type': 'uint256'
                            }
                        ],
                        'name': 'transferFrom',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'bool'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'constant': true,
                        'inputs': [],
                        'name': 'decimals',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'uint8'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'constant': true,
                        'inputs': [
                            {
                                'name': '_owner',
                                'type': 'address'
                            }
                        ],
                        'name': 'balanceOf',
                        'outputs': [
                            {
                                'name': 'balance',
                                'type': 'uint256'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'constant': true,
                        'inputs': [],
                        'name': 'symbol',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'string'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'constant': false,
                        'inputs': [
                            {
                                'name': '_to',
                                'type': 'address'
                            },
                            {
                                'name': '_value',
                                'type': 'uint256'
                            }
                        ],
                        'name': 'transfer',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'bool'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'nonpayable',
                        'type': 'function'
                    },
                    {
                        'constant': true,
                        'inputs': [
                            {
                                'name': '_owner',
                                'type': 'address'
                            },
                            {
                                'name': '_spender',
                                'type': 'address'
                            }
                        ],
                        'name': 'allowance',
                        'outputs': [
                            {
                                'name': '',
                                'type': 'uint256'
                            }
                        ],
                        'payable': false,
                        'stateMutability': 'view',
                        'type': 'function'
                    },
                    {
                        'payable': true,
                        'stateMutability': 'payable',
                        'type': 'fallback'
                    },
                    {
                        'anonymous': false,
                        'inputs': [
                            {
                                'indexed': true,
                                'name': 'owner',
                                'type': 'address'
                            },
                            {
                                'indexed': true,
                                'name': 'spender',
                                'type': 'address'
                            },
                            {
                                'indexed': false,
                                'name': 'value',
                                'type': 'uint256'
                            }
                        ],
                        'name': 'Approval',
                        'type': 'event'
                    },
                    {
                        'anonymous': false,
                        'inputs': [
                            {
                                'indexed': true,
                                'name': 'from',
                                'type': 'address'
                            },
                            {
                                'indexed': true,
                                'name': 'to',
                                'type': 'address'
                            },
                            {
                                'indexed': false,
                                'name': 'value',
                                'type': 'uint256'
                            }
                        ],
                        'name': 'Transfer',
                        'type': 'event'
                    }
                ]";
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

        [Parameter("address", "assetId", 4, false)]
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

        [Parameter("address", "assetId", 5, false)]
        public string AssetId { get; set; }
    }
}