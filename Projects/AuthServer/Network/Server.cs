// Copyright (c) Arctium.

using System.Net.Sockets;
using System.Threading.Tasks;
using Framework.Network;

namespace AuthServer.Network
{
    class Server : ServerBase
    {
        public Server(string ip, int port) : base(ip, port) { }

        public override async Task DoWork(Socket client)
        {
            await Task.Factory.StartNew(new AuthSession(client).Accept);
        }
    }
}
