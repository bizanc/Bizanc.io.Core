using System;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain.Messages;

namespace Bizanc.io.Matching.Core.Domain
{
    public interface IPeer
    {
        Guid Id { get; }
        string Address { get; }

        TaskCompletionSource<object> InitSource { get; }

        int ListenPort { get; set; }

        void SendMessage<T>(T message) where T : BaseMessage;

        Task<string> Receive();

        Task Disconnect();

        void StartHeartBeat();
    }
}