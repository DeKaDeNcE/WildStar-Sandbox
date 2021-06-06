// Copyright (c) Arctium.

using System;
using StsServer.Constants.Net;

namespace StsServer.Attributes
{
    class StsMessageAttribute : Attribute
    {
        public StsMessage Message { get; set; }
        
        public StsMessageAttribute(StsMessage message)
        {
            Message = message;
        }
    }
}
