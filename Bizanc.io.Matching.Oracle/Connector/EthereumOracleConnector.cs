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
using Nethereum.RPC.Accounts;
using Nethereum.RPC.NonceServices;
using RestSharp;
using Newtonsoft.Json.Linq;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class EthereumOracleConnector
    {
        private string abi = @"[ { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' }, { 'name': 'token', 'type': 'address' } ], 'name': 'depositERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' }, { 'name': 'token', 'type': 'address' } ], 'name': 'withdrawERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' } ], 'name': 'withdrawEth', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'denyAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'allowAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' } ], 'name': 'depositEth', 'outputs': [], 'payable': true, 'stateMutability': 'payable', 'type': 'function' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'from', 'type': 'address' }, { 'indexed': false, 'name': 'destination', 'type': 'string' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logDeposit', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'withdrawHash', 'type': 'string' }, { 'indexed': false, 'name': 'to', 'type': 'address' }, { 'indexed': false, 'name': 'origin', 'type': 'address' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logWithdrawal', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'AllowAccessEvent', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'DenyAccessEvent', 'type': 'event' } ]";

        private string contractAddress;
        private ExternalAccount account;
        private Web3Geth web3;
        private Contract contract;

        private Dictionary<string, string> tokenDictionary = new Dictionary<string, string>();
        public EthereumOracleConnector(string endpoint, string contractAddress, string pkcsUser, string key)
        {
            this.contractAddress = contractAddress;
            var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(endpoint));
            account = new ExternalAccount("0x0147059dfda73109414014E939bFbc69C791FD18", new HSMExternalEthSigner(pkcsUser, key), 1);
            account.NonceService = new InMemoryNonceService("0x0147059dfda73109414014E939bFbc69C791FD18", client);
            account.InitialiseDefaultTransactionManager(client);

            web3 = new Web3Geth(account, endpoint);
            contract = web3.Eth.GetContract(abi, contractAddress);

            tokenDictionary.Add("USDT", "0xdac17f958d2ee523a2206206994597c13d831ec7");
            tokenDictionary.Add("MTL", "0xF433089366899D83a9f26A773D59ec7eCF30355e");
            tokenDictionary.Add("USDC", "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48");
            tokenDictionary.Add("BAT", "0x0d8775f648430679a709e98d2b0cb6250d2887ef");
            tokenDictionary.Add("CRO", "0xa0b73e1ff0b80914ab6fe0444e65848c4c34450b");
            tokenDictionary.Add("DEX", "0x497bAEF294c11a5f0f5Bea3f2AdB3073DB448B56");
            tokenDictionary.Add("TUSD", "0x0000000000085d4780B73119b644AE5ecd22b376");
            tokenDictionary.Add("DAI", "0x89d24a6b4ccb1b6faa2625fe562bdd9a23260359");
            tokenDictionary.Add("EGT", "0x8e1b448ec7adfc7fa35fc2e885678bd323176e34");
            tokenDictionary.Add("ENJ", "0xf629cbd94d3791c9250152bd8dfbdf380e2a3b9c");
            tokenDictionary.Add("HT", "0x6f259637dcd74c767781e37bc6133cd6a68aa161");
            tokenDictionary.Add("INB", "0x17aa18a4b64a55abed7fa543f2ba4e91f2dce482");
            tokenDictionary.Add("KCS", "0x039b5649a59967e3e936d7471f9c3700100ee1ab");
            tokenDictionary.Add("ELF", "0xbf2179859fc6D5BEE9Bf9158632Dc51678a4100e");

            tokenDictionary.Add("ZRX", "0xe41d2489571d322189246dafa5ebde1f4699f498");
            tokenDictionary.Add("LINK", "0x514910771af9ca656af840dff83e8264ecf986ca");
            tokenDictionary.Add("MANA", "0x0f5d2fb29fb7d3cfee444a200298f468908cc942");
            tokenDictionary.Add("MATIC", "0x7D1AfA7B718fb893dB30A3aBc0Cfc608AaCfeBB0");
            tokenDictionary.Add("MCO", "0xb63b606ac810a52cca15e44bb630fd42d8d1d83d");
            tokenDictionary.Add("MKR", "0x9f8f72aa9304c8b593d555f12ef6589cc3a579a2");
            tokenDictionary.Add("ZB", "0xbd0793332e9fb844a52a205a233ef27a5b34b927");
            tokenDictionary.Add("OMG", "0xd26114cd6EE289AccF82350c8d8487fedB8A0C07");
            tokenDictionary.Add("PAX", "0x8e870d67f660d95d5be530380d0ec0bd388289e1");
            tokenDictionary.Add("PERL", "0xb5a73f5fc8bbdbce59bfd01ca8d35062e0dad801");
            tokenDictionary.Add("LAMB", "0x8971f9fd7196e5cee2c1032b50f656855af7dd26");
            tokenDictionary.Add("SEELE", "0xb1eef147028e9f480dbc5ccaa3277d417d1b85f0");
            tokenDictionary.Add("REP", "0x1985365e9f78359a9B6AD760e32412f4a445E862");
            tokenDictionary.Add("REALT", "0x46cc7ec70746f4cbd56ce5fa9bb7d648398eaa5c");
            tokenDictionary.Add("BRZ", "0x420412e765bfa6d85aaac94b4f7b708c89be2e2b");
            tokenDictionary.Add("SNT", "0x744d70fdbe2ba4cf95131626614a1763df805b9e");
            tokenDictionary.Add("NXPS", "0xa15c7ebe1f07caf6bff097d8a589fb8ac49ae5b3");
            tokenDictionary.Add("GUSD", "0x056fd409e1d7a124bd7017459dfea2f387b6d5cd");
            tokenDictionary.Add("BNB", "0xB8c77482e45F1F44dE1745F52C74426C631bDD52");
        }

        public async Task WithdrawEth(string withdrawHash, string recipient, decimal amount, string symbol)
        {
            Contract contract = web3.Eth.GetContract(abi, contractAddress);

            if (contract == null)
                Console.WriteLine("ContractNull");

            if (contract == null)
                Console.WriteLine("Acount null");

            var gasPrice = await GetGasPrice(TransferPriority.Fast);
            if (symbol == "ETH")
            {
                Function withdrawEth = contract.GetFunction("withdrawEth");
                Log.Warning("Sending ETH Withdrawal...");
                if (withdrawEth == null)
                    Console.WriteLine("Function Null");
                await withdrawEth.SendTransactionAsync(account.Address,             // Sender
                                                                                    new HexBigInteger(900000),  // Gas
                                                                                    new HexBigInteger(gasPrice),
                                                                                    null,
                                                                                    withdrawHash,      // WithdrawHash
                                                                                    recipient,      // Recipient
                                                                                    account.Address,   // Sender
                                                                                    Web3Geth.Convert.ToWei(amount));
            }
            else
            if (tokenDictionary.ContainsKey(symbol))
            {
                Function withdrawERC20 = contract.GetFunction("withdrawERC20");

                var token = web3.Eth.GetContract(ERC20ABI, tokenDictionary[symbol]);
                var decimals = await token.GetFunction("decimals").CallAsync<BigInteger>();

                var power = 1;

                for (int i = 0; i < decimals; i++)
                    power = 10 * power;

                BigInteger value = new BigInteger(amount * power);
                decimals.ToString();

                Log.Warning("Sending "+symbol +" Withdrawal...");
                await withdrawERC20.SendTransactionAsync(account.Address,             // Sender
                                                                                    new HexBigInteger(900000),  // Gas
                                                                                    new HexBigInteger(gasPrice),
                                                                                    null,
                                                                                    withdrawHash,      // WithdrawHash
                                                                                    recipient,      // Recipient
                                                                                    account.Address,   // Sender
                                                                                    value,
                                                                                    tokenDictionary[symbol]);
            }

            Log.Warning("Withdrawal Sent: " + withdrawHash);
        }

        public enum TransferPriority
        {
            Low,
            Average,
            Fast,
            Fastest
        }

        public async Task<BigInteger> GetGasPrice(TransferPriority priority)
        {
            var price = ((BigInteger)(await GetGasPriceGwei(priority) * 100000000));

            if (price == 0)
                price = (await web3.Eth.GasPrice.SendRequestAsync());

            return price;
        }

        public async Task<decimal> GetGasPriceGwei(TransferPriority priority)
        {
            try
            {
                RestClient client = new RestClient("https://ethgasstation.info/");
                var request = new RestRequest("json/ethgasAPI.json", Method.GET);

                var response = await client.ExecuteGetTaskAsync(request);
                var result = JObject.Parse(response.Content);

                switch (priority)
                {
                    case TransferPriority.Average:
                        return result["average"].ToObject<decimal>();
                    case TransferPriority.Fast:
                        return result["fast"].ToObject<decimal>();
                    case TransferPriority.Fastest:
                        return result["fastest"].ToObject<decimal>();
                    case TransferPriority.Low:
                        return result["safeLow"].ToObject<decimal>();
                    default:
                        return 0;
                }
            }
            catch (Exception e)
            {
                return 0;
            }
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
}