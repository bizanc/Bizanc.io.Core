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
                
                var rawPub = new Span<Byte>(key.Export(KeyBlobFormat.RawPublicKey));
                var check = CalculateCheckSum(rawPub, 4);
                var pub = new Span<byte>(new byte[rawPub.Length + check.Length]);
                rawPub.CopyTo(pub);
                for (int i = 0; i < check.Length; i++)
                    pub[rawPub.Length + i] = check[i];
                
                var pbc = Base58.Bitcoin.Encode(pub);

                Console.WriteLine(pbc);
                Console.WriteLine(pvt);
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
            if(!IsValidBizancAddress(publicKey))
                return false;

            var decoded = Base58.Bitcoin.Decode(publicKey);
            var pubKey = PublicKey.Import(algorithm, decoded.Slice(0, decoded.Length - 4), KeyBlobFormat.RawPublicKey);

            return algorithm.Verify(pubKey, Encoding.UTF8.GetBytes(source), Base58.Bitcoin.Decode(signature));
        }

        public static byte[] Hash(string str)
        {
            return Hash(Encoding.UTF8.GetBytes(str));
        }

        public static byte[] Hash(byte[] value)
        {
            byte[] hash;
            using (var algorithm = SHA256.Create())
            {
                hash = algorithm.ComputeHash(value);
            }

            return hash;
        }

        public static bool IsValidCheckSum(string address, int checkLeng)
        {
            var decoded = Base58.Bitcoin.Decode(address);
            return IsValidCheckSum(decoded, checkLeng);
        }

        public static bool IsValidCheckSum(Span<byte> decoded, int checkLeng)
        {
            var check = CalculateCheckSum(decoded.Slice(0, decoded.Length - checkLeng), checkLeng);
            return decoded.Slice(decoded.Length - checkLeng, checkLeng).SequenceEqual(check);
        }

        public static Span<byte> CalculateCheckSum(Span<byte> data, int checkLeng)
        {
            var d1 = Hash(data.ToArray());
            var d2 = new Span<byte>(Hash(d1));
            return d2.Slice(0, checkLeng);
        }

        public static bool IsValidBitcoinAddress(string address)
        {
            if (address.Length < 26 || address.Length > 35)
                return false;

            return IsValidCheckSum(address, 4);
        }

        public static bool IsValidEthereumAddress(string address)
        {
            return Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(address)
                && Nethereum.Util.AddressUtil.Current.IsChecksumAddress(address);
        }

        public static bool IsValidBizancAddress(string address)
        {
            var decoded = Base58.Bitcoin.Decode(address);
            if (decoded.Length != 36)
                return false;

            return IsValidCheckSum(decoded, 4);
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