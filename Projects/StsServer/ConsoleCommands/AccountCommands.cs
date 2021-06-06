// Copyright (c) Arctium.


namespace StsServer.ConsoleCommands
{
    class AccountCommands
    {
        /*[ConsoleCommand("CreateAccount", "")]
        public static void CreateAccount(string[] args)
        {
            var email = Helper.Read<string>(args, 0);
            var loginName = Helper.Read<string>(args, 1);
            var password = Helper.Read<string>(args, 2);

            if (email != null && password != null && loginName != null)
            {
                var salt = Helper.GenerateRandomKey(8).ToHexString();
                var result = DB.Auth.Any<Account>(a => a.Email == email || a.LoginName == loginName);

                if (!result)
                {
                    var srp = new SRP6a(salt);

                    srp.CalculateX(email, password, false);

                    var account = new Account
                    {
                        Email = email,
                        LoginName = loginName,
                        PasswordVerifier = srp.V.ToHexString(),
                        Salt = salt
                    };

                    var accountId = DB.Auth.GetAutoIncrementValue<Account, uint>();
                    var gameAccountId = DB.Auth.GetAutoIncrementValue<GameAccount, uint>();

                    var gameAccount = new GameAccount
                    {
                        AccountId = accountId,
                        Alias     = $"{accountId}#{gameAccountId}",
                    };

                    if (DB.Auth.Add(account) && DB.Auth.Add(gameAccount))
                        Console.WriteLine($"Account 'Email: {email}, LoginName: {loginName}' successfully created.");
                }
                else
                    Console.WriteLine($"Account 'Email: {email}' or 'LoginName: {loginName}' already in database.");
            }
        }*/
    }
}
