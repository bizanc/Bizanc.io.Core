using System;
using Xunit;
using FluentAssertions;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using RestSharp;
using System.Threading;

namespace Bizanc.io.Matching.Test
{
    public class OfferTest
    {
        decimal p = 333;
        [Fact]
        public void Ask()
        {
            var pvtKey = "jqWoAa/jvXSc3QQVl0lI/s9MbeqkIBXOvVx08EKyfNk=";
            var pubKey = "6ZfNdq3EtXLJWX2kJAAgMoIgUUOZEKoJaOWl3e0muZM=";
            // var pvtKey = "KyfOaCCa7wBaMjMsw1JmTYeBObpYQYPBmj+dsuPf1sU=";
            // var pubKey = "BKOJ0U4+VgUTXkfEPjCst4+N+cgxVU6taNALpUnJnA/zbfAkcV7cd6INvBKWH3xT0wr0uHXfWYtKuUowwOG2DeI=";
            decimal quantity = 2;
            decimal price = p;
            var of = new Offer();

            of.Wallet = pubKey;
            of.Asset = "ETH";
            of.Quantity = quantity;
            of.Price = price;
            of.BuildHash();
            of.Signature = CryptoHelper.Sign(of.ToString(), pvtKey);

            RestClient client = new RestClient("http://localhost:5000/");
            var request = new RestRequest("api/offerbook/offer", Method.POST);
            request.AddParameter("wallet", pubKey);
            request.AddParameter("asset", of.Asset);


            request.AddParameter("quantity", quantity);
            request.AddParameter("price", price);
            request.AddParameter("signature", of.Signature);

            var response = client.Post(request);
            Console.WriteLine(response.ToString());

        }

        // [Fact]
        // public void Bid()
        // {
        //     var pvtKey = "O+sqrb4w/CCzuKmb3QXRHlAAG4DOwQs6Ryz/ac3HG0w=";
        //     var pubKey = "BDAspn9v8TqUUMUpboTakMYDlvTkDbz0wlBIAEaOr52zjS3R3WxkR+iHdLIvU7XQG5owT2qwdblLb6nBjpPtm6c=";
        //     // var pvtKey = "KyfOaCCa7wBaMjMsw1JmTYeBObpYQYPBmj+dsuPf1sU=";
        //     // var pubKey = "BKOJ0U4+VgUTXkfEPjCst4+N+cgxVU6taNALpUnJnA/zbfAkcV7cd6INvBKWH3xT0wr0uHXfWYtKuUowwOG2DeI=";
        //     decimal quantity = 2;
        //     decimal price = p;


        //         var of = new Offer();

        //         of.Id = Guid.NewGuid();
        //         of.Wallet = pubKey;
        //         of.Quantity = quantity;
        //         of.Price = price;
        //         of.Type = OfferType.Bid;

        //         (of.SignR, of.SignS) = CryptoHelper.Sign(of.ToString(), pvtKey);

        //         RestClient client = new RestClient("http://localhost:5000/");
        //         var request = new RestRequest("api/offerbook/offer", Method.POST);
        //         request.AddParameter("sourceWallet", pubKey);
        //         request.AddParameter("type", of.Type);
        //         request.AddParameter("quantity", quantity);
        //         request.AddParameter("price", price);
        //         request.AddParameter("signR", of.SignR);
        //         request.AddParameter("signS", of.SignS);

        //         var response = client.Post(request);
        //         Console.WriteLine(response.ToString());

        // }
    }
}