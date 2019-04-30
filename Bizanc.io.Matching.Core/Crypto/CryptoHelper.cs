using System;
using System.Security.Cryptography;
using System.Text;
using static System.Convert;
using NSec.Cryptography;
using SimpleBase;

namespace Bizanc.io.Matching.Core.Crypto
{
    public static class CryptoHelper
    {
        private static Ed25519 algorithm = SignatureAlgorithm.Ed25519;

        public static (string, string) CreateKeyPair()
        {
            using (var key = Key.Create(algorithm, new KeyCreationParameters() { ExportPolicy = KeyExportPolicies.AllowPlaintextExport }))
            {
                var pvt = Base58.Bitcoin.Encode(new Span<Byte>(key.Export(KeyBlobFormat.RawPrivateKey)));
                var pbc = Base58.Bitcoin.Encode(new Span<Byte>(key.Export(KeyBlobFormat.RawPublicKey)));

                return (pvt, pbc);
            }
        }

        public static string Sign(string source, string privateKey)
        {
            using (var key = Key.Import(algorithm, Base58.Bitcoin.Decode(privateKey), KeyBlobFormat.RawPrivateKey))
            {
                var signature = algorithm.Sign(key, Encoding.UTF8.GetBytes(source));
                return Base58.Bitcoin.Encode(new Span<Byte>(signature));
            }
        }

        public static bool IsValidSignature(string source, string publicKey, string signature)
        {
            var pubKey = PublicKey.Import(algorithm, Base58.Bitcoin.Decode(publicKey), KeyBlobFormat.RawPublicKey);

            return algorithm.Verify(pubKey, Encoding.UTF8.GetBytes(source), Base58.Bitcoin.Decode(signature));
        }

        public static byte[] Hash(string str)
        {
            var value = Encoding.UTF8.GetBytes(str);
            byte[] hash;
            using (var algorithm = SHA256.Create())
            {
                hash = algorithm.ComputeHash(value);
            }

            return hash;
        }

        public static bool IsValidHash(int diff, byte[] hash)
        {
            bool result = true;
            for (int i = 0; i <= diff / 8 && result; i++)
            {
                byte compare = (i < (diff / 8)) ? (byte)0xff : (byte)((0xff >> (8 - (diff % 8)) << (8 - (diff % 8))));
                result = result && ((compare & hash[i]) == compare);
            }

            return result;
        }
    }
}