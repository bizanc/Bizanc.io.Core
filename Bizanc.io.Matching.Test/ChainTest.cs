using System;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using FluentAssertions;
using Xunit;

namespace Bizanc.io.Matching.Test
{
    public class ChainTest
    {
        // [Fact]
        // public async void Should_Mine_Genesis()
        // {
        //     var chain = new Chain();
        //     await chain.Initialize();

        //     chain.Blocks.Count.Should().Be(1);
        // }

        // [Fact]
        // public async void Should_Mine_After_5_Offers()
        // {
        //     var book = new OfferBook();
        //     var chain = new Chain();
        //     await chain.Initialize();
        //     var block = new Block();

        //     for (int i = 1; i <= 5; i++)
        //     {
        //         block = await chain.Append(await book.AddAsk(12, 100));
                
        //         if(block.Offers.Count < 5)
        //             block.Header.Status.Should().Be(BlockStatus.Open);
        //         else
        //             block.Header.Status.Should().Be(BlockStatus.Mined);
        //     }          
        // }
    }
}