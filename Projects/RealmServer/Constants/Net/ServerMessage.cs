// Copyright (c) Arctium.

namespace RealmServer.Constants.Net
{
    enum ServerMessage : ushort
    {
        State1                = 0x000, // 16029
        State2                = 0x001, // 16029
        SHello                = 0x003, // 16029
        CharacterCreateResult = 0x0DC, // 16029
        CharacterDeleteResult = 0x0E6, // 16029
        GatewayToClientWrapper = 0x3DC, // 16029
        AccountEntitlementUpdates = 0x968,
        AccountEntitlementUpdate = 0x973,
        CharacterListResponse = 0x117, // 16029
        CreateObject          = 0x262, // 16029
        CharacterCreated = 606,
        RealmList = 0x0761,
        UpdateUnitProperties = 0x93A,
        UpdateEquipment = 0x933,
        Logout = 146,
        LogoutComplete = 1428,
        Time = 0x845,

        TestStuff = 2418

    }
}
