namespace ISUCore.Helpers
{
    internal static class UtilsFunctions
    {
        internal static string FixEmailFormat(string email)
        {
            string fixedEmail = string.Empty;
            string splitChar = string.Empty;
            if (email.Contains(","))
            {
                splitChar = ",";
            }
            else if (email.Contains(";"))
            {
                splitChar = ";";
            }
            else if (email.Contains(" "))
            {
                splitChar = " ";
            }
            fixedEmail = email.Split(splitChar)[0];
            return fixedEmail;
        }
    }
}

