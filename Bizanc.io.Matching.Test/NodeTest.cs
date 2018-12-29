using System;
using System.Linq;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Infra;
using FluentAssertions;
using Xunit;

namespace Bizanc.io.Matching.Test
{
    public class NodeTest
    {
        // [Fact]
        // public async void Test()
        // {
        //     var node1 = new Node("node1");
        //     var node2 = new Node("node2");

        //     await node1.Connect(new RemoteNode(new RemoteNode(null, node2), node2));
        //     await node2.Connect(new RemoteNode(new RemoteNode(null, node1), node1));

        //     for (int i = 0; i < 10; i++)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+i,
        //             Quantity = 100,
        //             Type = OfferType.Bid
        //         };

        //         await node1.Message(offer);
        //     }

        //     for (int i = 10; i > 0; i--)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+1,
        //             Quantity = 100,
        //             Type = OfferType.Ask
        //         };

        //         await node2.Message(offer);
        //     }

        //     var node3 = new Node("node3");

        //     await node1.Connect(new RemoteNode(new RemoteNode(null, node3), node3));
        //     await node2.Connect(new RemoteNode(new RemoteNode(null, node3), node3));

        //     await node3.Connect(new RemoteNode(new RemoteNode(null, node1), node1));
        //     await node3.Connect(new RemoteNode(new RemoteNode(null, node2), node2));

        //     for (int i = 0; i < 10; i++)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+i,
        //             Quantity = 100,
        //             Type = OfferType.Bid
        //         };

        //         await node3.Message(offer);
        //     }

        //     for (int i = 10; i > 0; i--)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+1,
        //             Quantity = 100,
        //             Type = OfferType.Ask
        //         };

        //         await node3.Message(offer);
        //     }

        //     var node4 = new Node("node4");

        //     await node1.Connect(new RemoteNode(new RemoteNode(null, node4), node4));
        //     await node2.Connect(new RemoteNode(new RemoteNode(null, node4), node4));
        //     await node3.Connect(new RemoteNode(new RemoteNode(null, node4), node4));

        //     await node4.Connect(new RemoteNode(new RemoteNode(null, node1), node1));
        //     await node4.Connect(new RemoteNode(new RemoteNode(null, node2), node2));
        //     await node4.Connect(new RemoteNode(new RemoteNode(null, node3), node3));

        //     for (int i = 0; i < 10; i++)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+i,
        //             Quantity = 100,
        //             Type = OfferType.Bid
        //         };

        //         await node4.Message(offer);
        //     }

        //     for (int i = 10; i > 0; i--)
        //     {
        //         var offer = new Offer()
        //         {
        //             Id = Guid.NewGuid(),
        //             Price = 10+1,
        //             Quantity = 100,
        //             Type = OfferType.Ask
        //         };

        //         await node4.Message(offer);
        //     }
        // }
    }
}