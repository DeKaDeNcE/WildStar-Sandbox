// Copyright (c) Arctium.

namespace AuthServer.Constants.Net
{
    enum ServerMessage : ushort
    {
        State1         = 0x000, // 15193
        State2         = 0x001, // 15193
        SHello         = 0x003, // 15193
        AuthToClientWrapper = 0x076, // 15193
        ConnectToRealm = 0x3DB, // 15193
        AuthComplete   = 0x591, // 15193
        RealmMessage   = 0x763, // 15193
        RealmList = 0x6F2,
    }
}
