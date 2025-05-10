namespace ISUCore.Authorization.Roles
{
    public static class StaticRoleNames
    {
        public static class Host
        {
            public const string Admin = "Admin"; //cambiar a HostAdmin
            public const string ApiClient = "ApiClient"; // also exists ApiClient user.
            public const string Jobs = "Jobs";
        }

        public static class Tenants
        {
            public const string SuperAdmin = "Super Admin";
            public const string Admin = "Admin";
            public const string ApiClient = "ApiClient"; // also exists ApiClient user.
            public const string Jobs = "Jobs";
        }
    }
}

