// Copyright (c) Arctium.

namespace RealmServer.Constants.Net
{
    enum ClientMessage : ushort
    {
        State1                = 0x000, // 16029
        State2                = 0x001, // 16029
        SHello                = 0x003, // 16029
        Composite             = 0x244, // 16029
        CreateCharacter       = 0x25B, // 16029
        DeleteCharacter       = 0x352, // 16029
        WorldComposite        = 0x25C, // 16029
        WorldWrap             = 0x38C, // 16029
        GatewayRequest        = 0x58F, // 16029
        RetrieveCharacterList = 0x7E0, // 16029
        PlayerLogin           = 0x7DD, // 16029
        UpdateCommand         = 0x637, // 16029

        ChatMessage = 0x1C3, // 16029
        
        RealmList = 0x7A4,
        RealmList2 = 0x82D,
        LogoutRequest = 0xBF,
        LogoutComplete = 0xC0,
    }
}
