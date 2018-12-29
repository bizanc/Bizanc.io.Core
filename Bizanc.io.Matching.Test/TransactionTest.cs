using System;
using Xunit;
using FluentAssertions;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using RestSharp;
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Bizanc.io.Matching.Api.Model;
using Bizanc.io.Matching.Core.Util;

namespace Bizanc.io.Matching.Test
{
    public class TransactionTest
    {

        private void SendTx(string pubKey, string pvtKey, string targetWallet, decimal txSize)
        {
            var tx = new Transaction();

            tx.TimeStampTicks = DateTime.Now.ToUniversalTime().Ticks;
            tx.Version = "1";
            tx.Wallet = pubKey;
            tx.Asset = "BIZ";
            tx.Outputs.Add(new TransactionOutput()
            {
                Wallet = targetWallet,
                Size = txSize
            });

            tx.Signature = CryptoHelper.Sign(tx.ToString(), pvtKey);

            tx.BuildHash();
            RestClient client = new RestClient("http://localhost:4000/");
            // RestClient client = new RestClient("https://bizanc.io/");
            var request = new RestRequest("api/transactions", Method.POST);
            request.AddJsonBody(new TransactionModel()
            {
                Asset = tx.Asset,
                SourceWallet = tx.Wallet,
                TargetWallet = targetWallet,
                Size = txSize,
                Timestamp = tx.TimeStampTicks,
                Signature = tx.Signature
            });

            var response = client.Post(request);
            response.ToString();

            // if( i> 1 && (i% 10000 ==0))
            //     Thread.Sleep(30000);

            // // if(i%30 == 0)
            //     Thread.Sleep(1000);

        }
        [Fact]
        public void Test()

        {
            var pvtKey = "FhzTgFqFx5zFj6mhoJkXjYoErZcQ3E8SN815VwBRtK5K";
            var pubKey = "GjfedL2V2EwM4jrCkV3ybyJX3Gwb7x3eaXQHCgS6etQV";
            // var pvtKey = "4FJgvaCjSAgCx6ontQB21GqJFWKNijTDA1ZqighwXwUC";
            // var pubKey = "wcFwVp9bQK1kPP5SDi5hyRGzPhDMWB41FgUinR3YbJr";
            var targetWallet = "C1Qb9w9KGtmFoUb6uyNCnZzw4mZU9aAXV6FjgWWLFFfu";
            decimal size = 0.0001M;

            var tasks = new List<Task>();
            for (int i = 0; i < 1_000_000; i++)
            {

                var txSize = size + (i / 1000000);

                SendTx(pubKey, pvtKey, targetWallet, txSize);

                // if( i> 1 && (i% 10000 ==0))
                //     Thread.Sleep(60000);

                // // if(i%30 == 0)
                //     Thread.Sleep(1000);
            }

            Task.WaitAll(tasks.ToArray());
        }

        [Fact]
        public void TestPool()
        {
            var locker = new ReadWriteLockAsync();
            Task.Run(async () =>
            {
                await locker.EnterReadLock();
                await Task.Delay(1000);
                locker.ExitReadLock();
            });

            Task.Run(async () =>
            {
                await locker.EnterReadLock();
                await Task.Delay(1000);
                locker.ExitReadLock();
            });

            Task.Run(async () =>
            {
                await locker.EnterReadLock();
                await Task.Delay(1000);
                locker.ExitReadLock();
            });

            Task.Run(async () =>
            {
                await locker.EnterWriteLock();
                await Task.Delay(1000);
                locker.ExitWriteLock();
            });

            Task.Run(async () =>
                        {
                            await locker.EnterReadLock();
                            await Task.Delay(1000);
                            locker.ExitReadLock();
                        });
        }
    }
}