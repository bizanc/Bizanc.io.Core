// using System;
// using System.Linq;
// using System.Threading.Tasks;
// using Bizanc.io.Matching.Core.Domain;
// using FluentAssertions;
// using Xunit;

// namespace Bizanc.io.Matching.Test
// {
//     public class OfferBookTest
//     {
//         [Fact]
//         public async void Should_Add_Bid()
//         {
//             var book = new OfferBook();
//             var offer = await book.AddBid(10, 100);

//             book.Bids.Any(o => o.Id == offer.Id).Should().BeTrue();
//         }

//         [Fact]
//         public async void Should_Add_Ask()
//         {
//             var book = new OfferBook();
//             var offer = await book.AddAsk(10, 100);

//             book.Asks.Any(o => o.Id == offer.Id).Should().BeTrue();
//         }

//         private async Task<(OfferBook, Offer, Offer, Offer)> CreateThreeOrderedAsks()
//         {
//             var book = new OfferBook();
//             var ask1 = await book.AddAsk(10, 100);
//             var ask2 = await book.AddAsk(11, 100);
//             var ask3 = await book.AddAsk(12, 100);

//             return (book, ask1, ask2, ask3);
//         }

//         [Fact]
//         public async void Should_Add_Sort_Three_Bids()
//         {
//             var (book, bid1, bid2, bid3) = await CreateThreeOrderedBids();

//             book.Bids[0].Id.Should().Be(bid3.Id);
//             book.Bids[1].Id.Should().Be(bid2.Id);
//             book.Bids[2].Id.Should().Be(bid1.Id);
//         }

//         [Fact]
//         public async void Should_Add_Three_Bids_With_Zero_Trades_ExecQuantity()
//         {
//             var (_, bid1, bid2, bid3) = await CreateThreeOrderedBids();

//             bid1.ExecQuantity.Should().Be(0);
//             bid2.ExecQuantity.Should().Be(0);
//             bid3.ExecQuantity.Should().Be(0);
//             bid1.Trades.Count.Should().Be(0);
//             bid2.Trades.Count.Should().Be(0);
//             bid3.Trades.Count.Should().Be(0);
//         }

//         [Fact]
//         public async void Should_Add_Three_Bids_With_NotEmpty_ID_Quantity_Price()
//         {
//             var (_, bid1, bid2, bid3) = await CreateThreeOrderedBids();

//             bid1.Id.Should().NotBeEmpty();
//             bid2.Id.Should().NotBeEmpty();
//             bid3.Id.Should().NotBeEmpty();
//             bid1.Price.Should().Be(10);
//             bid2.Price.Should().Be(11);
//             bid3.Price.Should().Be(12);
//             bid1.Quantity.Should().Be(100);
//             bid2.Quantity.Should().Be(100);
//             bid3.Quantity.Should().Be(100);
//         }

//         [Fact]
//         public async void Should_Add_Three_Bids_With_NotEmpty_LeavesQuantity_Status_Type()
//         {
//             var (_, bid1, bid2, bid3) = await CreateThreeOrderedBids();

//             bid1.LeavesQuantity.Should().Be(100);
//             bid2.LeavesQuantity.Should().Be(100);
//             bid3.LeavesQuantity.Should().Be(100);
//             bid1.Status.Should().Be(OfferStatus.New);
//             bid2.Status.Should().Be(OfferStatus.New);
//             bid3.Status.Should().Be(OfferStatus.New);
//             bid1.Type.Should().Be(OfferType.Bid);
//             bid2.Type.Should().Be(OfferType.Bid);
//             bid3.Type.Should().Be(OfferType.Bid);
//         }

//         private async Task<(OfferBook, Offer, Offer, Offer)> CreateThreeOrderedBids()
//         {
//             var book = new OfferBook();
//             var bid1 = await book.AddBid(10, 100);
//             var bid2 = await book.AddBid(11, 100);
//             var bid3 = await book.AddBid(12, 100);

//             return (book, bid1, bid2, bid3);
//         }

//         [Fact]
//         public async void Should_Add_Sort_Three_Asks()
//         {
//             var (book, ask1, ask2, ask3) = await CreateThreeOrderedAsks();

//             book.Asks[0].Id.Should().Be(ask1.Id);
//             book.Asks[1].Id.Should().Be(ask2.Id);
//             book.Asks[2].Id.Should().Be(ask3.Id);
//         }

//         [Fact]
//         public async void Should_Add_Three_Asks_With_Zero_Trades_ExecQuantity()
//         {
//             var (_, ask1, ask2, ask3) = await CreateThreeOrderedAsks();

//             ask1.ExecQuantity.Should().Be(0);
//             ask2.ExecQuantity.Should().Be(0);
//             ask3.ExecQuantity.Should().Be(0);
//             ask1.Trades.Count.Should().Be(0);
//             ask2.Trades.Count.Should().Be(0);
//             ask3.Trades.Count.Should().Be(0);
//         }

//         [Fact]
//         public async void Should_Add_Three_Asks_With_NotEmpty_ID_Quantity_Price()
//         {
//             var (_, ask1, ask2, ask3) = await CreateThreeOrderedAsks();

//             ask1.Id.Should().NotBeEmpty();
//             ask2.Id.Should().NotBeEmpty();
//             ask3.Id.Should().NotBeEmpty();
//             ask1.Price.Should().Be(10);
//             ask2.Price.Should().Be(11);
//             ask3.Price.Should().Be(12);
//             ask1.Quantity.Should().Be(100);
//             ask2.Quantity.Should().Be(100);
//             ask3.Quantity.Should().Be(100);
//         }

//         [Fact]
//         public async void Should_Add_Three_Asks_With_NotEmpty_LeavesQuantity_Status_Type()
//         {
//             var (_, ask1, ask2, ask3) = await CreateThreeOrderedAsks();

//             ask1.LeavesQuantity.Should().Be(100);
//             ask2.LeavesQuantity.Should().Be(100);
//             ask3.LeavesQuantity.Should().Be(100);
//             ask1.Status.Should().Be(OfferStatus.New);
//             ask2.Status.Should().Be(OfferStatus.New);
//             ask3.Status.Should().Be(OfferStatus.New);
//             ask1.Type.Should().Be(OfferType.Ask);
//             ask2.Type.Should().Be(OfferType.Ask);
//             ask3.Type.Should().Be(OfferType.Ask);
//         }

//         private async Task<(OfferBook, Offer, Offer)> CreateMatchedTakeSamePriceQuantity()
//         {
//             var book = new OfferBook();
//             var ask = await book.AddAsk(10, 100);
//             var bid = await book.AddBid(11, 100);

//             return (book, bid, ask);
//         }

//         [Fact]
//         public async void Should_Match_Take_Same_Price_Quantity_Clean_Book()
//         {
//             var (book, _, _) = await CreateMatchedTakeSamePriceQuantity();

//             book.Bids.Count.Should().Be(0);
//             book.Asks.Count.Should().Be(0);
//         }

//         [Fact]
//         public async void Should_Match_Take_Same_Price_Quantity_With_Ask_Empty_Trades_Quantities()
//         {
//             var (_, _, ask) = await CreateMatchedTakeSamePriceQuantity();

//             ask.Trades.Count.Should().Be(0);
//             ask.ExecQuantity.Should().Be(0);
//             ask.LeavesQuantity.Should().Be(100);
//             ask.Status.Should().Be(OfferStatus.New);
//         }

//         [Fact]
//         public async void Should_Match_Take_Same_Price_Quantity_With_Bid_Trades_Quantities()
//         {
//             var (_, bid, ask) = await CreateMatchedTakeSamePriceQuantity();

//             bid.ExecQuantity.Should().Be(100);
//             bid.LeavesQuantity.Should().Be(0);
//             bid.Status.Should().Be(OfferStatus.Filled);
//             bid.Trades.Count.Should().Be(1);
//             bid.Trades[0].Donor.Should().Be(ask.Id);
//             (DateTime.Now - bid.Trades[0].DtTrade).Should().BeLessThan(new TimeSpan(0, 0, 1));
//             bid.Trades[0].Price.Should().Be(ask.Price);
//             bid.Trades[0].Quantity.Should().Be(ask.Quantity);
//             bid.Trades[0].Taker.Should().Be(bid.Id);
//         }

//         private async Task<(OfferBook, Offer, Offer)> CreateMatchedHitSamePriceQuantity()
//         {
//             var book = new OfferBook();
//             var bid = await book.AddBid(10, 100);
//             var ask = await book.AddAsk(10, 100);

//             return (book, bid, ask);
//         }

//         [Fact]
//         public async void Should_Match_Hit_Same_Price_Quantity_Clean_Book()
//         {
//             var (book, _, _) = await CreateMatchedHitSamePriceQuantity();

//             book.Bids.Count.Should().Be(0);
//             book.Asks.Count.Should().Be(0);
//         }

//         [Fact]
//         public async void Should_Match_Hit_Same_Price_Quantity_With_Bid_Empty_Trades_Quantities()
//         {
//             var (_, bid, _) = await CreateMatchedHitSamePriceQuantity();

//             bid.Trades.Count.Should().Be(0);
//             bid.ExecQuantity.Should().Be(0);
//             bid.LeavesQuantity.Should().Be(100);
//             bid.Status.Should().Be(OfferStatus.New);
//         }

//         [Fact]
//         public async void Should_Match_Hit_Same_Price_Quantity_With_Ask_Trades_Quantities()
//         {
//             var (_, bid, ask) = await CreateMatchedHitSamePriceQuantity();

//             ask.ExecQuantity.Should().Be(100);
//             ask.LeavesQuantity.Should().Be(0);
//             ask.Status.Should().Be(OfferStatus.Filled);
//             ask.Trades.Count.Should().Be(1);
//             ask.Trades[0].Donor.Should().Be(bid.Id);
//             (DateTime.Now - ask.Trades[0].DtTrade).Should().BeLessThan(new TimeSpan(0, 0, 1));
//             ask.Trades[0].Price.Should().Be(bid.Price);
//             ask.Trades[0].Quantity.Should().Be(bid.Quantity);
//             ask.Trades[0].Taker.Should().Be(ask.Id);
//         }

//         [Fact]
//         public async void Should_Match_Take_Offer_With_Low_or_Same_Price_Leave_Quantity()
//         {
//             var book = new OfferBook();
//             await book.AddAsk(10, 100);
//             await book.AddAsk(11, 400);

//             var bid = await book.AddBid(11, 300);

//             bid.Trades.Count.Should().Be(2);
//             book.Bids.Count.Should().Be(0);
//             book.Asks.Count.Should().Be(1);
//             book.Asks[0].LeavesQuantity.Should().Be(200);
//         }

//         [Fact]
//         public async void Should_Match_Take_Offer_With_Low_or_Same_Price_Stay_On_Book()
//         {
//             var book = new OfferBook();
//             await book.AddAsk(10, 100);
//             await book.AddAsk(11, 400);


//             var bid = await book.AddBid(11, 600);

//             bid.Trades.Count.Should().Be(2);
//             book.Bids.Count.Should().Be(1);
//             book.Asks.Count.Should().Be(0);
//             book.Bids[0].LeavesQuantity.Should().Be(100);
//         }

//         [Fact]
//         public async void Should_Match_Hit_Offer_With_Low_or_Same_Price_Leave_Quantity()
//         {
//             var book = new OfferBook();
//             await book.AddBid(10, 400);
//             await book.AddBid(11, 100);

//             var ask = await book.AddAsk(10, 300);

//             ask.Trades.Count.Should().Be(2);
//             book.Bids.Count.Should().Be(1);
//             book.Asks.Count.Should().Be(0);
//             book.Bids[0].LeavesQuantity.Should().Be(200);
//         }

//         [Fact]
//         public async void Should_Match_Hit_Offer_With_Low_or_Same_Price_Stay_On_Book()
//         {
//             var book = new OfferBook();
//             await book.AddBid(10, 400);
//             await book.AddBid(11, 100);


//             var ask = await book.AddAsk(10, 600);

//             ask.Trades.Count.Should().Be(2);
//             book.Bids.Count.Should().Be(0);
//             book.Asks.Count.Should().Be(1);
//             book.Asks[0].LeavesQuantity.Should().Be(100);
//         }
//     }
// }
