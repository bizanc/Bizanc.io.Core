using System;
using System.Threading.Tasks;
using Nethereum.Signer;
using Nethereum.Signer.Crypto;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI80;
using System.Collections.Generic;
using System.Linq;
using Bizanc.io.Matching.Core.Crypto;

namespace Bizanc.io.Matching.Infra.Connector
{
    public class HSMExternalEthSigner : EthExternalSignerBase
    {
        public override ExternalSignerTransactionFormat ExternalSignerTransactionFormat { get; protected set; } = ExternalSignerTransactionFormat.Hash;
        public override bool CalculatesV { get; protected set; } = false;

        public override async Task SignAsync(Transaction transaction)
        {
            await SignHashTransactionAsync(transaction).ConfigureAwait(false);
        }

        public override async Task SignAsync(TransactionChainId transaction)
        {
            await SignHashTransactionAsync(transaction).ConfigureAwait(false);
        }

        protected override async Task<byte[]> GetPublicKeyAsync()
        {
            return await Task.FromResult(CryptoHelper.StringToByteArray("040d701643c7c8cf247e37f8f20b315f588bec69bf3d71ef4d5f53f379ef16246e0150013aee41d6b344b1313f7ac811ed3d14d3e97b647831bf52e9eb9a2a321f"));
                                                                                                                                                    
        }

        protected override async Task<ECDSASignature> SignExternallyAsync(byte[] bytes)
        {
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
                        Net.Pkcs11Interop.HighLevelAPI.IMechanism mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_ECDSA);

                        List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute> publicKeyAttributes = new List<Net.Pkcs11Interop.HighLevelAPI.IObjectAttribute>();
                        publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_LABEL, "newEthKey"));
                        publicKeyAttributes.Add(new Net.Pkcs11Interop.HighLevelAPI80.ObjectAttribute(CKA.CKA_SIGN, true));

                        Net.Pkcs11Interop.HighLevelAPI.IObjectHandle key = session.FindAllObjects(publicKeyAttributes).FirstOrDefault();


                        byte[] signature = session.Sign(mechanism, key, bytes);
                        Console.WriteLine("signature: " + BitConverter.ToString(signature));
                        session.Logout();

                        return await Task.FromResult(ECDSASignatureFactory.FromComponents(signature).MakeCanonical());
                    }
                }
            }
            return null;
        }
    }
}