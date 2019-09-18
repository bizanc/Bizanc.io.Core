using System;
using System.Numerics;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Connector;
using Bizanc.io.Matching.Core.Util;
using System.Threading;
using NBitcoin;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using NBitcoin.Policy;
using NBXplorer;
using NBXplorer.Models;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI80;
using Serilog;
using Nethereum.Signer;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class BitcoinOracleConnector
    {
        private ExplorerClient client;

        private NetworkType network;

        private Dictionary<string, UTXO> usedInputs = new Dictionary<string, UTXO>();

        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        private NBitcoin.PubKey pubKey;

        private string pkcsUser;
        private string hsmKey;


        public BitcoinOracleConnector(string network, string endpoint, string pkcsUser, string hsmKey)
        {
            this.pkcsUser = pkcsUser;
            this.hsmKey = hsmKey;
            this.network = network == "testnet" ? NetworkType.Testnet : NetworkType.Mainnet;
            client = new ExplorerClient(new NBXplorerNetworkProvider(this.network).GetBTC(), new Uri(endpoint));
            pubKey = new NBitcoin.PubKey("045138d46c0e99a3b94a49551581097bae7162bdbd70dbddc580963f98b2771bbee5ecf367dfa34b2c9952269691f87153cbf2a1589c177dfd66ac6735b92f6cf7");
        }

        public async Task<string> WithdrawBtc(string withdrawHash, string recipient, decimal amount)
        {
            var coins = new List<UTXO>();
            var coinsUsed = new List<UTXO>();
            var estimatedFee = await client.GetFeeRateAsync(3);

            try
            {
                await locker.WaitAsync();

                while (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) <= amount)
                {
                    var txOperations = await client.GetUTXOsAsync(TrackedSource.Create(pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main)));
                    foreach (var op in txOperations.Confirmed.UTXOs)
                    {
                        if (!usedInputs.ContainsKey(op.TransactionHash.ToString() + op.Value.Satoshi.ToString()))
                            coins.Add(op);
                    }

                    coins.Sort(delegate (UTXO x, UTXO y)
                    {
                        return -x.AsCoin().Amount.CompareTo(y.AsCoin().Amount);
                    });

                    foreach (var item in coins)
                    {
                        if (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) <= amount)
                        {
                            coinsUsed.Add(item);
                            usedInputs.Add(item.TransactionHash.ToString() + item.Value.Satoshi.ToString(), item);
                        }
                        else
                            break;
                    }

                    if (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) < amount)
                        await Task.Delay(5000);
                }

                TransactionBuilder builder = null;
                BitcoinPubKeyAddress destination = null;
                if (network == NetworkType.Testnet)
                {
                    builder = Network.TestNet.CreateTransactionBuilder();
                    destination = new BitcoinPubKeyAddress(recipient, Network.TestNet);
                }
                else
                {
                    builder = Network.Main.CreateTransactionBuilder();
                    destination = new BitcoinPubKeyAddress(recipient, Network.Main);
                }

                NBitcoin.Transaction tx = builder
                                        .AddCoins(coinsUsed.Select(c => c.AsCoin()/* .ScriptPubKey.ToBytes()*/))
                                        .Send(destination, Money.Coins(amount))
                                        .Send(TxNullDataTemplate.Instance.GenerateScriptPubKey(Encoding.UTF8.GetBytes(withdrawHash)), Money.Zero)
                                        .SetChange(pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main))
                                        .SendEstimatedFees((await client.GetFeeRateAsync(3)).FeeRate)
                                        .BuildTransaction(sign: false);

                // Specify the path to unmanaged PKCS#11 library provided by the cryptographic device vendor
                string pkcs11LibraryPath = @"/opt/cloudhsm/lib/libcloudhsm_pkcs11_standard.so";

                // Create factories used by Pkcs11Interop library
                Net.Pkcs11Interop.HighLevelAPI.Pkcs11InteropFactories factories = new Net.Pkcs11Interop.HighLevelAPI.Pkcs11InteropFactories();

                // Load unmanaged PKCS#11 library
                using (Net.Pkcs11Interop.HighLevelAPI.IPkcs11Library pkcs11Library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, pkcs11LibraryPath, AppType.MultiThreaded))
                {
                    // Show general information about loaded library
                    Net.Pkcs11Interop.HighLevelAPI.ILibraryInfo libraryInfo = pkcs11Library.GetInfo();

                    Console.WriteLine("Library");
                    Console.WriteLine("  Manufacturer:       " + libraryInfo.ManufacturerId);
                    Console.WriteLine("  Description:        " + libraryInfo.LibraryDescription);
                    Console.WriteLine("  Version:            " + libraryInfo.LibraryVersion);

                    // Get list of all available slots
                    foreach (Net.Pkcs11Interop.HighLevelAPI.ISlot slot in pkcs11Library.GetSlotList(SlotsType.WithOrWithoutTokenPresent))
                    {
                        // Show basic information about slot
                        Net.Pkcs11Interop.HighLevelAPI.ISlotInfo slotInfo = slot.GetSlotInfo();

                        Console.WriteLine();
                        Console.WriteLine("Slot");
                        Console.WriteLine("  Manufacturer:       " + slotInfo.ManufacturerId);
                        Console.WriteLine("  Description:        " + slotInfo.SlotDescription);
                        Console.WriteLine("  Token present:      " + slotInfo.SlotFlags.TokenPresent);

                        if (slotInfo.SlotFlags.TokenPresent)
                        {
                            // Show basic information about token present in the slot
                            Net.Pkcs11Interop.HighLevelAPI.ITokenInfo tokenInfo = slot.GetTokenInfo();

                            Console.WriteLine("Token");
                            Console.WriteLine("  Manufacturer:       " + tokenInfo.ManufacturerId);
                            Console.WriteLine("  Model:              " + tokenInfo.Model);
                            Console.WriteLine("  Serial number:      " + tokenInfo.SerialNumber);
                            Console.WriteLine("  Label:              " + tokenInfo.Label);

                            // Show list of mechanisms (algorithms) supported by the token
                            Console.WriteLine("Supported mechanisms: ");
                            foreach (CKM mechanism in slot.GetMechanismList())
                                Console.WriteLine("  " + mechanism);

                        }

                        using (Net.Pkcs11Interop.HighLevelAPI.ISession session = slot.OpenSession(SessionType.ReadWrite))
                        {
                            session.Login(CKU.CKU_USER, pkcsUser);

                            // Specify signing mechanism
                            Net.Pkcs11Interop.HighLevelAPI.IMechanism mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_ECDSA);

                            List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute> publicKeyAttributes = new List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute>();
                            publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_LABEL, hsmKey));
                            publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_SIGN, true));


                            Net.Pkcs11Interop.HighLevelAPI.IObjectHandle key = session.FindAllObjects(publicKeyAttributes).FirstOrDefault();

                            uint i = 0;
                            foreach (var c in tx.Inputs.AsIndexedInputs())
                            {
                                byte[] sourceData = c.GetSignatureHash(coinsUsed.First(cu => cu.Outpoint.Hash == c.PrevOut.Hash).AsCoin()).ToBytes();
                                Console.WriteLine("sourceData: " + tx.ToHex());

                                byte[] signature = session.Sign(mechanism, key, sourceData);
                                Console.WriteLine("signature: " + BitConverter.ToString(signature));
                                var canSig = ECDSASignatureFactory.FromComponents(signature).MakeCanonical();
                                var sig = new NBitcoin.TransactionSignature(new NBitcoin.Crypto.ECDSASignature(new NBitcoin.BouncyCastle.Math.BigInteger(canSig.R.ToByteArray()), new NBitcoin.BouncyCastle.Math.BigInteger(canSig.S.ToByteArray())), SigHash.Single);
                                builder = builder.AddKnownSignature(pubKey, sig, c.PrevOut);
                                TransactionSignature sig2 = null;
                                if (builder.TrySignInput(tx, i, SigHash.Single, out sig2))
                                {
                                    Console.WriteLine("Input Signed");
                                    Console.WriteLine(BitConverter.ToString(sig2.ToBytes()));
                                }
                                else
                                    Console.WriteLine("Input Not Signed");

                                Console.WriteLine("tx: " + tx);
                                i++;

                                tx = builder.SignTransactionInPlace(tx);
                            }


                            Console.WriteLine("tx: " + tx);
                            TransactionPolicyError[] errors = null;
                            if (builder.Verify(tx, out errors))
                            {
                                var broadcastResult = await client.BroadcastAsync(tx);
                                broadcastResult.ToString();
                                Console.WriteLine("broadcast: " + tx.GetHash().ToString());
                                session.Logout();
                                return tx.GetHash().ToString();
                            }
                            else
                                Console.WriteLine("Verify transaction failed");
                                
                            if (errors != null)
                            {
                                foreach(var e in errors)
                                    Console.WriteLine(e.ToString());
                            }

                            session.Logout();
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error("Failed to send BTC Withdraw" + e.ToString());
                throw e;
            }
            finally
            {
                locker.Release();
            }
        }
    }
}