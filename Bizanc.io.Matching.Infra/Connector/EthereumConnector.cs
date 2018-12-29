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
    public class EthereumConnector
    {
        private static string privateKeyEth = "0x2952a82db5058f4b61cb283db47a767cf8f7157fe0144b9575b1b117495241f3";
        static string publicKeyEth = "0x046CDC17FfBd317600c7503Cb70Cd65161b41297";
        private static Account account = new Account(privateKeyEth);
        private static Web3Geth web3 = new Web3Geth(account, "https://rinkeby.infura.io/v3/f2478498dd8c423ea9065f07a0c110ca"); // Testnet

        private static string abi = @"[{'constant':false,'inputs':[{'name':'destination','type':'string'},{'name':'token','type':'address'}],'name':'depositERC20','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'to','type':'address'},{'name':'origin','type':'address'},{'name':'value','type':'uint256'},{'name':'token','type':'address'}],'name':'withdrawERC20','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_address','type':'address'}],'name':'denyAccess','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'_address','type':'address'}],'name':'allowAccess','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'constant':false,'inputs':[{'name':'destination','type':'string'}],'name':'depositEth','outputs':[],'payable':true,'stateMutability':'payable','type':'function'},{'constant':false,'inputs':[{'name':'to','type':'address'},{'name':'origin','type':'address'},{'name':'value','type':'uint256'}],'name':'withdrawEth','outputs':[],'payable':false,'stateMutability':'nonpayable','type':'function'},{'anonymous':false,'inputs':[{'indexed':false,'name':'from','type':'address'},{'indexed':false,'name':'destination','type':'string'},{'indexed':false,'name':'amount','type':'uint256'},{'indexed':false,'name':'currency','type':'string'}],'name':'logDeposit','type':'event'},{'anonymous':false,'inputs':[{'indexed':false,'name':'to','type':'address'},{'indexed':false,'name':'origin','type':'address'},{'indexed':false,'name':'amount','type':'uint256'},{'indexed':false,'name':'curency','type':'string'}],'name':'logWithdrawal','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'_address','type':'address'}],'name':'AllowAccessEvent','type':'event'},{'anonymous':false,'inputs':[{'indexed':true,'name':'_address','type':'address'}],'name':'DenyAccessEvent','type':'event'}]";
        private static string contractAddress = "0x27634e638c7131c9fa010fede3b77f878967f000";
        private static Contract contract = web3.Eth.GetContract(abi, contractAddress);

        private static Function depositEth = contract.GetFunction("depositEth");
        private static Event logDepositEvent = contract.GetEvent("logDeposit");
        private static NewFilterInput depositFilter = logDepositEvent.CreateFilterInput();

        private static Function withdrawEth = contract.GetFunction("withdrawEth");

        private static Function withdrawERC20 = contract.GetFunction("withdrawERC20");
        private static Event logWithdrawalEvent = contract.GetEvent("logWithdrawal");
        private static NewFilterInput WithdrawalFilter = logWithdrawalEvent.CreateFilterInput();

        private Nethereum.Hex.HexTypes.HexBigInteger currentBlock;
        private Nethereum.RPC.Eth.DTOs.NewFilterInput startupFilter;
        private List<Nethereum.Contracts.EventLog<Bizanc.io.Matching.Infra.Connector.LogDepositEvent>> startupLog;

        public async Task<List<Deposit>> Startup()
        {
            try
            {
                //var blockPeriod = 5 * 60 * 20 * 30;  // Amount of blocks to scan for events
                var deposits = new List<Deposit>();

                try
                {
                    currentBlock = new HexBigInteger((await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value - 5);
                    startupFilter = logDepositEvent.CreateFilterInput(new BlockParameter(new HexBigInteger(currentBlock.Value - currentBlock.Value + 1)),  new BlockParameter(currentBlock));
                    startupLog = await logDepositEvent.GetAllChanges<LogDepositEvent>(startupFilter);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                foreach (var d in startupLog)
                {
                    var deposit = new Deposit();
                    deposit.TargetWallet = d.Event.Destination;
                    deposit.Asset = d.Event.Symbol;
                    deposit.Quantity = Web3Geth.Convert.FromWei(d.Event.Value);
                    deposit.TxHash = d.Log.TransactionHash;
                    currentBlock = d.Log.BlockNumber;
                    deposits.Add(deposit);
                }

                return deposits;
            }
            catch (Exception e)
            {
                Console.WriteLine("Ethereum Connector Startp Failed: " + e.ToString());
            }

            return await Task.FromResult((List<Deposit>)null);
        }

        public async Task<List<Deposit>> GetEthDeposit()
        {
            var deposits = new List<Deposit>();
            depositFilter = logDepositEvent.CreateFilterInput();
            depositFilter.FromBlock = new BlockParameter(currentBlock);
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            depositFilter.ToBlock = new BlockParameter(new HexBigInteger(lastBlock.Value - 5));
            var log = await logDepositEvent.GetAllChanges<LogDepositEvent>(depositFilter);

            if (log.Count > 0)
            {
                foreach (var d in log)
                {
                    currentBlock = d.Log.BlockNumber;

                    //Read Ethereum events data.

                    var deposit = new Deposit();
                    deposit.TargetWallet = d.Event.Destination;
                    deposit.Asset = d.Event.Symbol;
                    deposit.Quantity = Web3Geth.Convert.FromWei(d.Event.Value);
                    deposit.TxHash = d.Log.TransactionHash;

                    deposits.Add(deposit);
                }
            }

            return deposits;
        }

        public async Task WithdrawEth(string recipient, decimal amount, string symbol)
        {
            try
            {
                if (symbol == "ETH")
                {
                    Console.WriteLine("Sending ETH Withdrawal...");
                    var receipt = await withdrawEth.SendTransactionAndWaitForReceiptAsync(publicKeyEth,             // Sender
                                                                                        new HexBigInteger(900000),  // Gas
                                                                                        null,
                                                                                        null,
                                                                                        recipient,      // Recipient
                                                                                        publicKeyEth,   // Sender
                                                                                        Web3Geth.Convert.ToWei(amount));

                    var log = await logWithdrawalEvent.GetAllChanges<LogWithdrawalEvent>(WithdrawalFilter);

                    if (log.Count > 0)
                    {
                        // withdrawal successful
                        Console.WriteLine("Withdrawal Successful");
                    }
                    else
                        Console.WriteLine("No logs returned from withdrawal...");
                } else 
                if (symbol == "TBRL")
                {
                    Console.WriteLine("Sending ETH Withdrawal...");
                    var receipt = await withdrawERC20.SendTransactionAndWaitForReceiptAsync(publicKeyEth,             // Sender
                                                                                        new HexBigInteger(900000),  // Gas
                                                                                        null,
                                                                                        null,
                                                                                        recipient,      // Recipient
                                                                                        publicKeyEth,   // Sender
                                                                                        Web3Geth.Convert.ToWei(amount),
                                                                                        "0x7a094dfD89893d204436Bf331a51d80F5C48a2Eb");

                    var log = await logWithdrawalEvent.GetAllChanges<LogWithdrawalEvent>(WithdrawalFilter);

                    if (log.Count > 0)
                    {
                        // withdrawal successful
                        Console.WriteLine("Withdrawal Successful");
                    }
                    else
                        Console.WriteLine("No logs returned from withdrawal...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }

    public class LogDepositEvent
    {
        [Parameter("address", "from", 1, false)]
        public string Sender { get; set; }

        [Parameter("string", "destination", 2, false)]
        public string Destination { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger Value { get; set; }

        [Parameter("string", "currency", 4, false)]
        public string Symbol { get; set; }
    }

    public class LogWithdrawalEvent
    {
        [Parameter("address", "to", 1, false)]
        public string Destination { get; set; }

        [Parameter("address", "origin", 2, false)]
        public string Sender { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger Value { get; set; }

        [Parameter("string", "currency", 4, false)]
        public string Symbol { get; set; }
    }
}