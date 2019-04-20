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

namespace Bizanc.io.Matching.Infra.Connector
{
    public class EthereumOracleConnector
    {
        private static string abi = @"[ { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' }, { 'name': 'token', 'type': 'address' } ], 'name': 'depositERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' }, { 'name': 'token', 'type': 'address' } ], 'name': 'withdrawERC20', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'withdrawHash', 'type': 'string' }, { 'name': 'to', 'type': 'address' }, { 'name': 'origin', 'type': 'address' }, { 'name': 'value', 'type': 'uint256' } ], 'name': 'withdrawEth', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'denyAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': '_address', 'type': 'address' } ], 'name': 'allowAccess', 'outputs': [], 'payable': false, 'stateMutability': 'nonpayable', 'type': 'function' }, { 'constant': false, 'inputs': [ { 'name': 'destination', 'type': 'string' } ], 'name': 'depositEth', 'outputs': [], 'payable': true, 'stateMutability': 'payable', 'type': 'function' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'from', 'type': 'address' }, { 'indexed': false, 'name': 'destination', 'type': 'string' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logDeposit', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': false, 'name': 'withdrawHash', 'type': 'string' }, { 'indexed': false, 'name': 'to', 'type': 'address' }, { 'indexed': false, 'name': 'origin', 'type': 'address' }, { 'indexed': false, 'name': 'amount', 'type': 'uint256' }, { 'indexed': false, 'name': 'asset', 'type': 'string' }, { 'indexed': false, 'name': 'assetId', 'type': 'address' } ], 'name': 'logWithdrawal', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'AllowAccessEvent', 'type': 'event' }, { 'anonymous': false, 'inputs': [ { 'indexed': true, 'name': '_address', 'type': 'address' } ], 'name': 'DenyAccessEvent', 'type': 'event' } ]";
        private static string contractAddress = "0xf246b043f492e66eBae298508f3e6f4a643242F9";

        public async Task<WithdrawInfo> WithdrawEth(string pvtKey, string withdrawHash, string recipient, decimal amount, string symbol)
        {
            Account account = new Account(pvtKey);
            Web3Geth web3 = new Web3Geth(account, "https://rinkeby.infura.io/v3/f2478498dd8c423ea9065f07a0c110ca");
            Contract contract = web3.Eth.GetContract(abi, contractAddress);
            TransactionReceipt receipt = null;
            try
            {
                if (symbol == "ETH")
                {
                    Function withdrawEth = contract.GetFunction("withdrawEth");
                    Console.WriteLine("Sending ETH Withdrawal...");
                    receipt = await withdrawEth.SendTransactionAndWaitForReceiptAsync(account.Address,             // Sender
                                                                                        new HexBigInteger(900000),  // Gas
                                                                                        null,
                                                                                        null,
                                                                                        withdrawHash,      // WithdrawHash
                                                                                        recipient,      // Recipient
                                                                                        account.Address,   // Sender
                                                                                        Web3Geth.Convert.ToWei(amount));
                }
                else
                if (symbol == "TBRL")
                {
                    Function withdrawERC20 = contract.GetFunction("withdrawERC20");
                    Console.WriteLine("Sending TBRL Withdrawal...");
                    receipt = await withdrawERC20.SendTransactionAndWaitForReceiptAsync(account.Address,             // Sender
                                                                                        new HexBigInteger(900000),  // Gas
                                                                                        null,
                                                                                        null,
                                                                                        withdrawHash,      // WithdrawHash
                                                                                        recipient,      // Recipient
                                                                                        account.Address,   // Sender
                                                                                        Web3Geth.Convert.ToWei(amount),
                                                                                        "0x7a094dfD89893d204436Bf331a51d80F5C48a2Eb");
                }

                if (receipt != null)
                {
                    if (receipt.HasErrors() != null && ((bool)receipt.HasErrors()))
                        Console.WriteLine("Withdrawal Error, Hash: " + withdrawHash);
                    else
                        Console.WriteLine("Withdrawal Success: " + withdrawHash);

                    return new WithdrawInfo() { Asset = symbol, HashStr = withdrawHash, TxHash = receipt.TransactionHash, Timestamp = DateTime.Now, BlockNumber = receipt.BlockNumber.HexValue };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return null;
        }
    }
}