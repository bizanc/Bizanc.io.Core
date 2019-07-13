using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Bizanc.io.Matching.Core.Domain;
using Serilog;

namespace Bizanc.io.Matching.Infra
{
    public class PeerListener : IPeerListener
    {
        private TcpListener listener;
        private int listenPort;

        public PeerListener(int listenPort)
        {
            this.listenPort = listenPort;
        }
        
        public async Task Start()
        {
            listener = new TcpListener(IPAddress.IPv6Any, listenPort);
            listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            try
            {
                //Only supported on Windows, other platforms thows exception.
                listener.AllowNatTraversal(true);
            }
            catch { }

            listener.Start();

            Log.Debug("Listener started...");

            await Task.CompletedTask;
        }

        public async Task<IPeer> Connect(string address)
        {
            try
            {
                return await Task<Peer>.Run(() =>
                {
                    Log.Debug("Connectig to: " + address);

                    var values = address.Split(':');
                    var port = values[values.Length - 1];
                    var ip = address.Substring(0, address.Length - port.Length - 1);

                    return new Peer(new TcpClient(ip, int.Parse(port)));
                });
            }
            catch (Exception e)
            {
                Log.Error("Failed to connect peer: " + address);
                Log.Debug(e.ToString());
            }

            return null;
        }

        public async Task<IPeer> Accept()
        {
            try
            {
                Log.Debug("Accept ");
                var tcpClient = await listener.AcceptTcpClientAsync();
                var peer = new Peer(tcpClient);
                Log.Information("Connection Received: " + peer.Address);

                return peer;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            return null;
        }
    }

}