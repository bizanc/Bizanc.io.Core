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

namespace Bizanc.io.Matching.Infra.Connector
{
    public class BitcoinOracleConnector
    {
        private ExplorerClient client;

        private NetworkType network;

        private Dictionary<string, UTXO> usedInputs = new Dictionary<string, UTXO>();

        private SemaphoreSlim locker = new SemaphoreSlim(1, 1);

        private NBitcoin.PubKey pubKey;

        public BitcoinOracleConnector(string network, string endpoint)
        {
            this.network = network == "testnet" ? NetworkType.Testnet : NetworkType.Mainnet;
            client = new ExplorerClient(new NBXplorerNetworkProvider(this.network).GetBTC(), new Uri(endpoint));
            pubKey = new NBitcoin.PubKey("04c37e0df81851f99775c0f223e2aa1f6e0ebeae7d63b6942ff5c8c01114a3eb13f2d0f3dafbc950898327bf1bd3d933b6ab1d48332867f952875142d8c408c04a");
        }

        public async Task<WithdrawInfo> WithdrawBtc(string withdrawHash, string recipient, decimal amount)
        {
            var coins = new List<UTXO>();
            var coinsUsed = new List<UTXO>();
            var estimatedFee = await client.GetFeeRateAsync(3);

            try
            {
                await locker.WaitAsync();

                while (coinsUsed.Sum(c => c.AsCoin().Amount.ToDecimal(MoneyUnit.BTC)) <= amount)
                {
                    var txOperations = await client.GetUTXOsAsync(TrackedSource.Create(pubKey.GetAddress(Network.Main)));
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
                                        .SetChange(pubKey.GetAddress(Network.Main))
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
                            session.Login(CKU.CKU_USER, "nodeuser:#$4567bizanc9923!~");

                            // Specify signing mechanism
                            Net.Pkcs11Interop.HighLevelAPI.IMechanism mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_ECDSA_SHA256);

                            List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute> publicKeyAttributes = new List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute>();
                            publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_LABEL, "newBtcKey"));
                            publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_SIGN, true));


                            Net.Pkcs11Interop.HighLevelAPI.IObjectHandle key = session.FindAllObjects(publicKeyAttributes).FirstOrDefault();

                            foreach (var c in coinsUsed)
                            {
                                byte[] sourceData = tx.GetSignatureHash(c.AsCoin()).ToBytes();
                                Console.WriteLine("sourceData: " + tx.ToHex());

                                byte[] signature = session.Sign(mechanism, key, sourceData);
                                Console.WriteLine("signature: " + signature.ToString());

                                var sig = new NBitcoin.TransactionSignature(signature);
                                builder = builder.AddKnownSignature(pubKey, sig);
                                Console.WriteLine("tx: " + tx);
                            }

                            tx = builder.BuildTransaction(false);
                            TransactionPolicyError[] errors = null;
                            if (builder.Verify(tx, out errors))
                            {
                                var broadcastResult = await client.BroadcastAsync(tx);
                                broadcastResult.ToString();
                                Console.WriteLine("broadcast: " + tx.GetHash().ToString());
                                session.Logout();
                                return new WithdrawInfo() { TxHash = tx.GetHash().ToString(), Timestamp = DateTime.Now };
                            }
                            else
                                Console.WriteLine("Verify transaction failed");

                            if (errors != null)
                            {
                                errors.ToString();
                            }

                            session.Logout();
                        }
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Log.Error("Failed to send BTC Withdraw"+e.ToString());
                throw e;
            }
            finally
            {
                locker.Release();
            }
        }
    }
}