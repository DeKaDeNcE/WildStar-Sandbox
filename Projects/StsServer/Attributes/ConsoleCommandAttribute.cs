// Copyright (c) Arctium.

using System;

namespace StsServer.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ConsoleCommandAttribute : Attribute
    {
        public string Command { get; }
        public string Description { get; }

        public ConsoleCommandAttribute(string command, string description)
        {
            Command = command.ToLower();
            Description = description;
        }
    }
}
