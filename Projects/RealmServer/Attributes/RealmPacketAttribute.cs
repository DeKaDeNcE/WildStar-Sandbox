// Copyright (c) Arctium.

using System;
using RealmServer.Constants.Net;

namespace RealmServer.Attributes
{
    class RealmPacketAttribute : Attribute
    {
        public ClientMessage Message { get; set; }

        public RealmPacketAttribute(ClientMessage message)
        {
            Message = message;
        }
    }
}
