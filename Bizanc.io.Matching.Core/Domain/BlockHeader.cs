using System;
using System.Text;
using SimpleBase;

namespace Bizanc.io.Matching.Core.Domain
{
    public class BlockHeader
    {
        public byte[] Hash { get; set; }

        public virtual long Nonce { get; set; }

        public virtual byte[] PreviousBlockHash { get; set; }

        public virtual int Difficult { get; set; }

        public virtual byte[] MerkleRoot { get; set; }

        public virtual long TimeStampTicks { get; set; }

        public virtual DateTime TimeStamp
        {
            get { return new DateTime(TimeStampTicks, DateTimeKind.Utc); }
            set { TimeStampTicks = value.ToUniversalTime().Ticks; }
        }

        public long Depth { get; set; }

        public virtual BlockStatus Status { get; set; } = BlockStatus.Open;

        public override string ToString()
        {
            return ToString(Nonce);
        }

        public string ToString(long nonce)
        {
            return (PreviousBlockHash != null ? Base58.Bitcoin.Encode(new Span<Byte>(PreviousBlockHash)) : "")
                + Difficult.ToString()
                + (MerkleRoot != null ? Base58.Bitcoin.Encode(new Span<Byte>(MerkleRoot)) : "")
                + TimeStampTicks
                + Depth
                + nonce;
        }

        public BlockHeader Clone()
        {
            return (BlockHeader)MemberwiseClone();
        }
    }

    public enum BlockStatus
    {
        Open,
        Mined
    }
}