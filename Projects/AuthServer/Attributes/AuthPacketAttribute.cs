// Copyright (c) Arctium.

using System;
using AuthServer.Constants.Net;

namespace AuthServer.Attributes
{
    class AuthPacketAttribute : Attribute
    {
        public ClientMessage Message { get; set; }

        public AuthPacketAttribute(ClientMessage message)
        {
            Message = message;
        }
    }
}
