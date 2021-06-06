// Copyright (c) Arctium.

using System.Net.Sockets;
using System.Threading.Tasks;
using Framework.Network;

namespace StsServer.Network
{
    class Server : ServerBase
    {
        public bool accept;
        public Server(string ip, int port) : base(ip, port) { }

        public override async Task DoWork(Socket client)
        {
            if (accept)
            await Task.Factory.StartNew(new StsSession(client).Accept);
        }
    }
}
