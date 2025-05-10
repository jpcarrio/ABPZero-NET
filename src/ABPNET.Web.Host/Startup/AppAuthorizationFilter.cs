using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Hangfire.Dashboard;
namespace ABPNET.Web.Host.Startup
{
    public class AppDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var result = false;
            var cookies = context.GetHttpContext().Request.Cookies;
            if (cookies["Abp.AuthToken"] != null)
            {
                string jwtToken = cookies["Abp.AuthToken"];
                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                JwtSecurityToken securityToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
                var roles = securityToken.Claims.Where(claim => claim.Type.EndsWith("role")).ToList();
                result = roles.First(claim => claim.Value.ToLower() == "admin") != null;
            }
            return result;
        }
    }
}



