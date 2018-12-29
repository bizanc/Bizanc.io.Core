using System;
using Xunit;
using FluentAssertions;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using Bizanc.io.Matching.Infra.Connector;

namespace Bizanc.io.Matching.Test
{
    public class CryptoTest
    {
        [Fact]
        public void Test()
        {
            var pubKey = "BJ954IPZsIkawNGZw5CQjFBYPl4M/H96aqaqLjbvk70O5Jwk37OPy62VIdAEIsRecsVjWXBBYj/UALNE0ulgMUc=";
            var targetWallet = "TTTTTTTTTTTTTT";
            var size = 10;
            var signR = "KvGWwWcTXP4g61EeiwnlzbZtiRwll4yE/aH2EVXkcIw=";
            var transaction = new Transaction();
            transaction.Wallet = pubKey;
            transaction.Outputs.Add(new TransactionOutput() { Wallet = targetWallet, Size = size });
            CryptoHelper.IsValidSignature(transaction.ToString(), pubKey, signR).Should().BeTrue();
        }

        [Fact]
        public void Deposit()
        {
            var con = new CryptoConnector();
            var test = con.DepositBtc("mzVr7Pk8gjWQBGqkGdcn5MZRS9ToxPWTXj", "n4nbUmxSRkSPDuRMTeuLV24pPQZdhqfjKN", 0.001m);
        }
    }
}