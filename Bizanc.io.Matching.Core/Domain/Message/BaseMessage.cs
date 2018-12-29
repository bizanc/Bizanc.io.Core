using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Crypto;
using Newtonsoft.Json;
using SimpleBase;

namespace Bizanc.io.Matching.Core.Domain.Messages
{
    public class BaseMessage
    {
        public string Version { get; set; }

        public virtual byte[] Hash { get; set; }

        public virtual string HashStr
        {
            get
            {
                if (Hash == null)
                    return "";
                return Base58.Bitcoin.Encode(new Span<Byte>(Hash));
            }
        }

        public virtual MessageType MessageType { get; set; }

        public virtual long TimeStampTicks { get; set; }

        public bool Mined { get; private set; }

        public virtual DateTime Timestamp
        {
            get { return new DateTime(TimeStampTicks, DateTimeKind.Utc); }
            set { TimeStampTicks = value.ToUniversalTime().Ticks; }
        }

        public void BuildHash()
        {
            Hash = CryptoHelper.Hash(ToString());
        }

        public virtual T Clone<T>() where T : BaseMessage
        {
            return (T)MemberwiseClone();
        }

        public virtual void Reset()
        {
            Mined = false;
        }

        public virtual void Finish()
        {
            Mined = true;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}