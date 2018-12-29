using System;
using Xunit;
using FluentAssertions;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using Bizanc.io.Matching.Infra.Connector;
using RestSharp;
using System.Threading;

namespace Bizanc.io.Matching.Test
{
    public class BtcWithdrawTest
    {
        [Fact]
        public void test()
        {
            var pvtKey = "jqWoAa/jvXSc3QQVl0lI/s9MbeqkIBXOvVx08EKyfNk=";
            var pubKey = "6ZfNdq3EtXLJWX2kJAAgMoIgUUOZEKoJaOWl3e0muZM=";
            // var pvtKey = "KyfOaCCa7wBaMjMsw1JmTYeBObpYQYPBmj+dsuPf1sU=";
            // var pubKey = "BKOJ0U4+VgUTXkfEPjCst4+N+cgxVU6taNALpUnJnA/zbfAkcV7cd6INvBKWH3xT0wr0uHXfWYtKuUowwOG2DeI=";
            decimal quantity = 2;
            var wd = new Withdrawal();

            wd.SourceWallet = pubKey;
            wd.TargetWallet = "asdfasdf";
            wd.Asset = "BTC";
            wd.Size = quantity;
            wd.Signature = CryptoHelper.Sign(wd.ToString(), pvtKey);

            wd.BuildHash();

            RestClient client = new RestClient("http://localhost:5000/");
            var request = new RestRequest("api/withdrawal", Method.POST);
            request.AddParameter("sourceWallet", pubKey);
            request.AddParameter("targetWallet", wd.TargetWallet);
            request.AddParameter("asset", wd.Asset);
            request.AddParameter("quantity", quantity);
            request.AddParameter("signature", wd.Signature);

            var response = client.Post(request);
        }
    }
}