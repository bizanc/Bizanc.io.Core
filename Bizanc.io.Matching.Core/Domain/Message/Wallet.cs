using System;

namespace Bizanc.io.Matching.Core.Domain
{
    public class Wallet
    {
        public String Id { get; set; } = Guid.NewGuid().ToString();
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }
}