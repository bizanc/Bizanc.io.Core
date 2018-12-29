using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Bizanc.io.Matching.Core.Domain.Messages;
using Bizanc.io.Matching.Core.Util;
using Newtonsoft.Json;

namespace Bizanc.io.Matching.Infra
{
    public class Peer : IPeer
    {
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private TcpClient client;
        private Stream stream;

        private StreamWriter streamWriter;

        private StreamReader streamReader;

        private string ip;

        private DateTime lastHeartBeat;

        public Guid Id { get; private set; } = Guid.NewGuid();

        public TaskCompletionSource<object> InitSource { get; } = new TaskCompletionSource<object>();

        private List<BaseMessage> sendPool = new List<BaseMessage>();

        private TaskCompletionSource<bool> sendCompSource = new TaskCompletionSource<bool>();

        private Channel<BaseMessage> sendStream;

        public string Address
        {
            get
            {
                return ip + ":" + ListenPort;
            }
        }

        public int ListenPort { get; set; }

        public Peer(TcpClient client)
        {
            this.client = client;

            var endpoint = (IPEndPoint)client.Client.RemoteEndPoint;
            ip = endpoint.Address.ToString();

            stream = client.GetStream();
            streamWriter = new StreamWriter(stream);
            streamReader = new StreamReader(stream);

            sendStream = Channel.CreateUnbounded<BaseMessage>();
            Task.Factory.StartNew(() => SendTask());
            lastHeartBeat = DateTime.Now;
        }

        private async Task HeartBeatTask(Task task)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                SendMessage(new HeartBeat());

                await Task.Delay(5000).ContinueWith(t => HeartBeatTask(t));
            }
        }

        private async Task HeartBeatTimeoutTask(Task task)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if ((DateTime.Now - lastHeartBeat).Seconds >= 30000)
                {
                    Console.WriteLine("Peer Timeout: " + Address);
                    await Disconnect();
                    return;
                }

                await Task.Delay(10000).ContinueWith(t => HeartBeatTimeoutTask(t));
            }
        }



        private async Task SendTask()
        {
            while(await sendStream.Reader.WaitToReadAsync())
            {
                var data = await sendStream.Reader.ReadAsync();
                var msg = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                if(data.MessageType ==  MessageType.Block)
                    Console.WriteLine("Block size: "+Encoding.UTF8.GetByteCount(msg));

                await streamWriter.WriteLineAsync(msg);
                await streamWriter.FlushAsync();
            }
        }


        public async void SendMessage<T>(T message) where T : BaseMessage
        {
            await sendStream.Writer.WriteAsync(message);
        }

        public async Task<string> Receive()
        {
            try
            {
                return await streamReader.ReadLineAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        public async Task Disconnect()
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Run(() =>
                {
                    cancellationToken.Cancel();
                    client.Close();
                    client.Dispose();
                });
            }
        }

        public void StartHeartBeat()
        {
            Task.Delay(1000).ContinueWith(t => HeartBeatTask(t));
        }
    }
}