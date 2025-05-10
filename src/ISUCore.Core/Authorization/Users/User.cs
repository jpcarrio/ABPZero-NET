using System;
using System.Collections.Generic;
using Abp.Authorization.Users;
using Abp.Extensions;

namespace ISUCore.Authorization.Users
{
    public class User : AbpUser<User>
    {
        public const string DefaultPassword = "123qwe";

        public static string CreateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Truncate(16);
        }

        public static User CreateTenantAdminUser(int tenantId, string emailAddress)
        {
            var name = tenantId > 1 ? AdminUserName + tenantId : AdminUserName;
            var user = new User
            {
                TenantId = tenantId,
                UserName = name,
                Name = name,
                Surname = name,
                EmailAddress = emailAddress,
                Roles = new List<UserRole>()
            };
            user.SetNormalizedNames();
            return user;
        }
        public static User CreateTenantUser(int tenantId, string emailAddress, string userName)
        {
            var name = tenantId > 1 ? userName + tenantId : userName;
            var user = new User
            {
                TenantId = tenantId,
                UserName = name,
                Name = name,
                Surname = name,
                EmailAddress = emailAddress,
                Roles = new List<UserRole>()
            };
            user.SetNormalizedNames();
            return user;
        }

        public string UserImage { get; set; }
        public string UserImageFileName { get; set; }
        public long? EmployeeId { get; set; }
    }
}

