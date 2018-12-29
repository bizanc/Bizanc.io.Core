using System;
using Xunit;
using FluentAssertions;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Crypto;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Bizanc.io.Matching.Test
{
    public class ImmutableTest
    {
        [Fact]
        public void Test()
        {

            var imutDic = new Dictionary<Guid, object>().ToImmutableDictionary();
            var dic = new Dictionary<Guid, object>();

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 10000000; i++)
            {
                imutDic = imutDic.Add(Guid.NewGuid(), i);
                // dic.Add(Guid.NewGuid(), i);
            }

            sw.Stop();
            sw.Elapsed.ToString();



        }
    }
}