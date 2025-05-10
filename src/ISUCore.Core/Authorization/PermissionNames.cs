namespace ISUCore.Authorization
{
    public static class PermissionNames
    {
        public const string Api_ClientUser = "Api.ClientUser";
        public const string Func_ChangeApplicationSettings = "Func.ChangeApplicationSettings";
        public const string Func_ChangeShowTentant = "Func.ChangeShowTentant";
        public const string Func_ChangeLogoImage = "Func.ChangeLogoImage";
        public const string Func_Jobs = "Func.Jobs";
        public const string Pages_Tenants = "Pages.Tenants";
        public const string Pages_Users = "Pages.Users";
        public const string Pages_Users_Activation = "Pages.Users.Activation";
        public const string Pages_Roles = "Pages.Roles";
        public const string Pages_Tasks = "Pages.Tasks";
        public const string Pages_Settings = "Pages.Settings";
        public const string Pages_GlobalSettings = "Pages.GlobalSettings";
        public const string Pages_EmployeesDetail = "Pages.EmployeesDetail";
        public const string Pages_EmployeesList = "Pages.Employees.List";
        public const string Pages_Projects = "Pages.Projects";
        public const string Pages_Imports = "Pages.Imports";
        public const string Pages_Payments = "Pages.Payments";
        public const string Pages_Actions = "Pages.Actions";
        public const string Pages_SkillsManager = "Pages.SkillsManager";
        public const string Pages_SkillsWizard = "Pages.SkillsWizard";
        public const string Pages_SkillsAnalysis = "Pages.SkillsAnalysis";
        public const string Pages_Reports_Projects = "Pages.Reports.Projects";
        public const string Pages_Reports_TimeOffs = "Pages.Reports.TimeOffs";
        public const string Pages_Reports_HumanResources = "Pages.Reports.HumanResources";
        public const string Pages_Projects_Notifications = "Pages.Projects.Notifications";


        public static bool IsPermissionChangeApplicationSettings(string permissionName)
        {
            return permissionName == Func_ChangeApplicationSettings;
        }
        public static bool IsPermissionChangeShowTentant(string permissionName)
        {
            return permissionName == Func_ChangeShowTentant;
        }
        public static bool IsPermissionChangeLogoImage(string permissionName)
        {
            return permissionName == Func_ChangeLogoImage;
        }
        public static bool IsPermissionJobs(string permissionName)
        {
            return permissionName == Func_Jobs;
        }
        public static bool IsPermissionApiClientUser(string permissionName)
        {
            return permissionName == Api_ClientUser;
        }
    }
}

