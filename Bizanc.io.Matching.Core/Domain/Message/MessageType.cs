using System;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public enum MessageType
    {
        None,
        HandShake,
        PeerListRequest,
        PeerListResponse,
        Block,
        Offer,
        OfferCancel,
        Transaction,
        TransactionPoolRequest,
        TransactionPoolResponse,
        BlockResponse,
        HeartBeat,
        Deposit,
        Withdrawal
    }
}