// Copyright (c) Arctium.

namespace RealmServer.Managers
{
    public class Manager
    {
        public static DataManager DataMgr;
        public static TableManager TableMgr;

        public static void Initialize()
        {
            DataMgr = new DataManager();
            TableMgr = new TableManager();
        }
    }
}
