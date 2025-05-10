using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.BackgroundJobs;
using Abp.Configuration;
using Abp.Domain.Uow;
using Abp.IdentityFramework;
using Abp.MultiTenancy;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.UI;
using ABPNET.Authentication.External;
using ABPNET.Authentication.JwtBearer;
using ABPNET.Authorization;
using ABPNET.Authorization.Users;
using ABPNET.Configuration;
using ABPNET.Exceptions;
using ABPNET.Helpers;
using ABPNET.Models.Message;
using ABPNET.Models.TokenAuth;
using ABPNET.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
namespace ABPNET.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TokenAuthController : ABPNETControllerBase
    {
        private readonly LogInManager _logInManager;
        private readonly ITenantCache _tenantCache;
        private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
        private readonly TokenAuthConfiguration _configuration;
        private readonly IExternalAuthConfiguration _externalAuthConfiguration;
        private readonly IExternalAuthManager _externalAuthManager;
        private readonly UserRegistrationManager _userRegistrationManager;
        private readonly UserManager _userManager;
        private readonly ClientConfiguration _clientConfiguration;
        private readonly MailConfiguration _mailConfiguration;
        private readonly SettingManager _settingManager;
        private readonly IBackgroundJobManager _backgroundJobManager;
        public TokenAuthController(
            AbpLoginResultTypeHelper abpLoginResultTypeHelper,
            IExternalAuthConfiguration externalAuthConfiguration,
            IExternalAuthManager externalAuthManager,
            IOptions<ClientConfiguration> clientConfiguration,
            IOptions<MailConfiguration> mailConfiguration,
            ITenantCache tenantCache,
            LogInManager logInManager,
            SettingManager settingManager,
            TokenAuthConfiguration configuration,
            UserManager userManager,
            UserRegistrationManager userRegistrationManager,
            IBackgroundJobManager backgroundJobManager
            )
        {
            _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
            _clientConfiguration = clientConfiguration.Value;
            _configuration = configuration;
            _externalAuthConfiguration = externalAuthConfiguration;
            _externalAuthManager = externalAuthManager;
            _logInManager = logInManager;
            _mailConfiguration = mailConfiguration.Value;
            _settingManager = settingManager;
            _tenantCache = tenantCache;
            _userManager = userManager;
            _userRegistrationManager = userRegistrationManager;
            _backgroundJobManager = backgroundJobManager;
        }
        [HttpPost]
        public async Task<AuthenticateResultModel> Authenticate([FromBody] AuthenticateModel model)
        {
            var loginResult = await GetLoginResultAsync(
                model.UserNameOrEmailAddress,
                model.Password,
                GetTenancyNameOrNull()
            );
            if (loginResult.User.IsTwoFactorEnabled)
            {
                loginResult.User.IsEmailConfirmed = false;
                UnitOfWorkManager.Current.SaveChanges();
            }
            var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));
            return new AuthenticateResultModel
            {
                AccessToken = accessToken,
                EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                UserId = loginResult.User.Id
            };
        }
        [HttpGet]
        [AbpAuthorize]
        [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BadRequestObjectResult), 400)]
        public async Task<FileContentResult> GetLogs()
        {
            var coreAssemblyDirectoryPath = Path.GetDirectoryName(typeof(ABPNETCoreModule).GetAssembly().Location);
            if (coreAssemblyDirectoryPath == null)
            {
                throw new Exception("Could not find location of ABPNET.Core assembly!");
            }
            var directoryInfo = new DirectoryInfo(coreAssemblyDirectoryPath);
            while (!DirectoryContains(directoryInfo.FullName, "App_Data"))
            {
                if (directoryInfo.Parent == null)
                {
                    throw new Exception("Could not find content root folder!");
                }
                directoryInfo = directoryInfo.Parent;
            }
            var logFile = Path.Combine(directoryInfo.FullName, "App_Data", "Logs", "Logs.txt");
            if (System.IO.File.Exists(logFile))
            {
                byte[] data = default(byte[]);
                MemoryStream ms = new MemoryStream();
                using (var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    await sr.BaseStream.CopyToAsync(ms);
                    data = ms.ToArray();
                }
                return File(data, "application/octet-stream", "logs.txt");
            }
            throw new FileNotFoundException();
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task<bool> VerifyTwoFactorCode([FromBody] TwoFactorCodeModel model)
        {
            var isTwoFactorEnabled = await IsTwoFactorEnabled();
            if (!isTwoFactorEnabled)
            {
                throw new Exception(this.L("NotUsingTwoFactorAuthException"));
            }
            var currentUser = this.ResolveAuthenticatedUser();
            if (string.IsNullOrEmpty(currentUser.EmailConfirmationCode))
            {
                throw new Exception(this.L("NoPendingCodeException"));
            }
            if (!currentUser.EmailConfirmationCode.Equals(model.Code))
            {
                throw new AbpAuthorizationException(this.L("InvalidCode"));
            }
            this.CleanUserVerificationCode(currentUser);
            return true;
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task<bool> IsTwoFactorEnabled()
        {
            var currentUser = this.ResolveAuthenticatedUser();
            return await _userManager.GetTwoFactorEnabledAsync(currentUser) || this.SettingManager.GetSettingValue(AppSettingNames.AppEnableTwoFactor).ToLower() == "true";
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task SendEmailVerificationCode()
        {
            try
            {
                // prepare message content
                var user = this.ResolveAuthenticatedUser();
                var random = new Random();
                var code = random.Next(1000, 9999);
                user.EmailConfirmationCode = code.ToString();
                await CurrentUnitOfWork.SaveChangesAsync();
                var subject = this.L("YourVerificationCode");
                var emailTitle = "VerificationCode";
                var emailIntroduction = string.Format(this.L("YourUserNameIs"), user.UserName);
                List<string> bodyLines = new List<string>
                {
                    this.L("PleaseFollowingVerifyCode")
                };
                var emailButtonText = string.Empty;
                var emailModel = new EmailModel
                {
                    RedirectUrl = code.ToString(),
                    UserName = user.UserName,
                    ServerDomain = _clientConfiguration.ClientRootAddress,
                    Subject = subject,
                    EmailTitle = emailTitle,
                    EmailIntroText = emailIntroduction,
                    EmailBodyLines = bodyLines.ToArray(),
                    EmailButtonText = emailButtonText
                };
                // schedule send email job
                await this.GetAppMailMessage("TwoFactorMailView", emailModel);
                emailModel.ToAddress = user.EmailAddress;
                emailModel.ToDisplayName = user.Name;
                _backgroundJobManager.Enqueue<SendAppMailMessageJob, EmailModel>(emailModel);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task SendEmailConfirmation([FromBody] EmailVerificationModel model)
        {
            try
            {
                // prepare message content
                var user = await FindByUserIdAsync(model.UserId);
                user.EmailConfirmationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                user.IsEmailConfirmed = false;
                var paramTenantId = user.TenantId.HasValue ? $"&pTenantId={user.TenantId.Value}" : string.Empty;
                var paramToken = $"&token={HttpUtility.UrlEncode(user.EmailConfirmationCode)}";
                var confirmationLink = $"{_clientConfiguration.ClientRootAddress}account/verify?userId={user.Id}{paramToken}{paramTenantId}";
                var emailTitle = this.L("AccountVerification");
                var emailIntroduction = string.Format(this.L("YourUserNameIs"), user.UserName);
                var subject = this.L(LoginMessagesConsts.WelcomeMessage);
                List<string> bodyLines = new List<string>
                {
                    this.L("PleaseFollowingVerifyLink")
                };
                var emailButtonText = this.L("ClickHere");
                // model
                var emailModel = new EmailModel
                {
                    RedirectUrl = confirmationLink,
                    UserName = user.UserName,
                    ServerDomain = _clientConfiguration.ClientRootAddress,
                    Subject = subject,
                    EmailTitle = emailTitle,
                    EmailIntroText = emailIntroduction,
                    EmailBodyLines = bodyLines.ToArray(),
                    EmailButtonText = emailButtonText
                };
                // schedule send email job
                await this.GetAppMailMessage("EmailConfirmation", emailModel);
                emailModel.ToAddress = user.EmailAddress;
                emailModel.ToDisplayName = user.Name;
                _backgroundJobManager.Enqueue<SendAppMailMessageJob, EmailModel>(emailModel);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task SendEmailConfirmationWithSetPassword([FromBody] EmailVerificationModel model)
        {
            try
            {
                // prepare message content
                var user = await FindByUserIdAsync(model.UserId);
                user.EmailConfirmationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                user.IsEmailConfirmed = false;
                var paramTenantId = user.TenantId.HasValue ? $"&pTenantId={user.TenantId.Value}" : string.Empty;
                var paramToken = $"&token={HttpUtility.UrlEncode(user.EmailConfirmationCode)}";
                var confirmationLink = $"{_clientConfiguration.ClientRootAddress}account/verifySetPassword?userId={user.Id}{paramToken}{paramTenantId}";
                var emailTitle = this.L("AccountVerification");
                var emailIntroduction = string.Format(this.L("YourUserNameIs"), user.UserName);
                var subject = this.L(LoginMessagesConsts.WelcomeMessage);
                List<string> bodyLines = new List<string>
                {
                    this.L("PleaseFollowingVerifyLink")
                };
                var emailButtonText = this.L("ClickHere");
                // model
                var emailModel = new EmailModel
                {
                    RedirectUrl = confirmationLink,
                    UserName = user.UserName,
                    ServerDomain = _clientConfiguration.ClientRootAddress,
                    Subject = subject,
                    EmailTitle = emailTitle,
                    EmailIntroText = emailIntroduction,
                    EmailBodyLines = bodyLines.ToArray(),
                    EmailButtonText = emailButtonText
                };
                // schedule send email job
                await this.GetAppMailMessage("EmailConfirmation", emailModel);
                emailModel.ToAddress = user.EmailAddress;
                emailModel.ToDisplayName = user.Name;
                _backgroundJobManager.Enqueue<SendAppMailMessageJob, EmailModel>(emailModel);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<BasicResultModel> SendEmailRecoveryPassword([FromBody] RecoverModel model)
        {
            try
            {
                // prepare message content
                var user = await FindByUserNameOrEmailAsync(model.EmailAddress);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var paramTenantId = user.TenantId.HasValue ? $"&pTenantId={user.TenantId.Value}" : string.Empty;
                var paramToken = $"&token={HttpUtility.UrlEncode(token)}";
                string resetUrl = $"{_clientConfiguration.ClientRootAddress}account/reset?userId={user.Id}{paramToken}{paramTenantId}";
                var subject = L("PasswordRecovery");
                var emailTitle = this.L("PasswordRecovery");
                var emailIntroduction = string.Format(this.L("YourUserNameIs"), user.UserName);
                var emailButtonText = this.L("ClickHere");
                List<string> bodyLines = new List<string>
                {
                    this.L("PleaseFollowingRecoveryLink")
                };
                var emailModel = new EmailModel
                {
                    RedirectUrl = resetUrl,
                    UserName = user.UserName,
                    ServerDomain = _clientConfiguration.ClientRootAddress,
                    Subject = subject,
                    EmailTitle = emailTitle,
                    EmailIntroText = emailIntroduction,
                    EmailBodyLines = bodyLines.ToArray(),
                    EmailButtonText = emailButtonText
                };
                // schedule send email job
                await this.GetAppMailMessage("RecoveryPasswordEmail", emailModel);
                emailModel.ToAddress = user.EmailAddress;
                emailModel.ToDisplayName = user.Name;
                _backgroundJobManager.Enqueue<SendAppMailMessageJob, EmailModel>(emailModel);
                return new BasicResultModel { Success = true, Message = "EmailSentSuccessfully" };
            }
            catch (TimeoutException t)
            {
                Logger.Error(t.Message);
                return new BasicResultModel { Success = false, Message = "TimeoutException" };
            }
            catch (EmailDoesntExistException t)
            {
                Logger.Error(t.Message);
                return new BasicResultModel { Success = false, Message = "EmailDoesntExist" };
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                return new BasicResultModel { Success = false, Message = "EmailSentFailed" };
            }
        }
        [HttpPost]
        [AbpAuthorize]
        public async Task SendEmailInviteToUsers(List<long> userIds)
        {
            try
            {
                if (!IsGranted(PermissionNames.Func_ChangeApplicationSettings))
                    throw new UserFriendlyException("DoNotHavePermissions");
                using (UnitOfWorkManager.Current.DisableFilter(AbpDataFilters.MayHaveTenant))
                {
                    foreach (var userId in userIds)
                    {
                        var user = await _userManager.GetUserByIdAsync(userId);
                        if (user == null)
                            throw new Exception("Invalid user id");
                        user.IsEmailConfirmed = false;
                        user.EmailConfirmationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        user.PasswordResetCode = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var paramTenantId = user.TenantId.HasValue ? $"&pTenantId={user.TenantId.Value}" : string.Empty;
                        var paramToken = $"&token={HttpUtility.UrlEncode(user.EmailConfirmationCode)}";
                        var confirmationLink = $"{_clientConfiguration.ClientRootAddress}account/verifySetPassword?userId={user.Id}{paramToken}{paramTenantId}";
                        var emailTitle = this.L("InvitationToUser");
                        var emailIntroduction = string.Format(this.L("YourUserNameIs"), user.UserName);
                        var subject = this.L(LoginMessagesConsts.WelcomeMessage);
                        List<string> bodyLines = new List<string>
                        {
                            this.L("PleaseFollowingVerifyLink")
                        };
                        var emailButtonText = this.L("ClickHere");
                        // model
                        var emailModel = new EmailModel
                        {
                            RedirectUrl = confirmationLink,
                            UserName = user.UserName,
                            ServerDomain = _clientConfiguration.ClientRootAddress,
                            Subject = subject,
                            EmailTitle = emailTitle,
                            EmailIntroText = emailIntroduction,
                            EmailBodyLines = bodyLines.ToArray(),
                            EmailButtonText = emailButtonText
                        };
                        // schedule send email job
                        await this.GetAppMailMessage("EmailConfirmation", emailModel);
                        emailModel.ToAddress = user.EmailAddress;
                        emailModel.ToDisplayName = user.Name;
                        _backgroundJobManager.Enqueue<SendAppMailMessageJob, EmailModel>(emailModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        [HttpGet]
        [AbpAllowAnonymous]
        public async Task<BasicResultModel> VerifyAccount(int userId, string token)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var user = await _userManager.GetUserByIdAsync(userId);
                if (user == null)
                    return new BasicResultModel() { Success = false, Message = "LinkBroken" };
                if (await _userManager.IsEmailConfirmedAsync(user))
                    return new BasicResultModel() { Success = true, Message = "AlreadyConfirmedEmail" };
                if (await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.EmailConfirmationTokenProvider, UserManager.ConfirmEmailTokenPurpose, token))
                {
                    var result = await _userManager.ConfirmEmailAsync(user, token);
                    CleanUserVerificationCode(user);
                    await CurrentUnitOfWork.SaveChangesAsync();
                    return new BasicResultModel()
                    {
                        Success = result.Succeeded,
                        Message = result.Errors != null && result.Errors.Count() > 0 ? "ConfirmEmailResultErrors" : "ConfirmEmailResultSucceeded"
                    };
                }
            }
            return new BasicResultModel() { Success = false, Message = "ConfirmEmailResultVerificationFailed" };
        }

        [HttpPost]
        public async Task<BasicResultModel> ResetPassword([FromBody] ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, UserManager.ResetPasswordTokenPurpose, model.Token))
            {
                try
                {
                    (await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword)).CheckErrors();

                    return new BasicResultModel { Success = true, Message = "ResetPasswordSuccess" };

                }
                catch (UserFriendlyException e)
                {
                    Logger.Error(e.Message);
                    if (e.Message.Contains("at least one non alphanumeric"))
                    {
                        return new BasicResultModel { Success = false, Message = "PasswordRequiresNonAlphanumeric" };
                    }

                    return new BasicResultModel { Success = false, Message = "ResetPasswordError" };
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    return new BasicResultModel { Success = false, Message = "ResetPasswordError" };
                }
            }

            Logger.Error(this.L("InvalidTokenforReset"));
            return new BasicResultModel { Success = false, Message = "InvalidTokenforReset" };
        }

        [HttpPost]
        public async Task<BasicResultModel> ResetPasswordWithVerify([FromBody] ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            var verify = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (verify == null)
            {
                return new BasicResultModel() { Success = false, Message = "ConfirmEmailResultVerificationFailed" };
            }
            model.Token = await _userManager.GeneratePasswordResetTokenAsync(user);
            if (await _userManager.VerifyUserTokenAsync(user, _userManager.Options.Tokens.PasswordResetTokenProvider, UserManager.ResetPasswordTokenPurpose, model.Token))
            {
                try
                {
                    IdentityResult result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                    CleanUserVerificationCode(user);
                    await CurrentUnitOfWork.SaveChangesAsync();
                    return new BasicResultModel { Success = true, Message = "ResetPasswordSuccess" };
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                    return new BasicResultModel { Success = false, Message = "ResetPasswordError" };
                }
            }
            else
            {
                Logger.Error(this.L("InvalidTokenforReset"));
                return new BasicResultModel { Success = false, Message = "InvalidTokenforReset" };
            }
        }
        [HttpGet]
        public List<ExternalLoginProviderInfoModel> GetExternalAuthenticationProviders()
        {
            return ObjectMapper.Map<List<ExternalLoginProviderInfoModel>>(_externalAuthConfiguration.Providers);
        }
        [HttpPost]
        public async Task<ExternalAuthenticateResultModel> ExternalAuthenticate([FromBody] ExternalAuthenticateModel model)
        {
            var externalUser = await GetExternalUserInfo(model);
            var loginResult = await _logInManager.LoginAsync(new UserLoginInfo(model.AuthProvider, model.ProviderKey, model.AuthProvider), GetTenancyNameOrNull());
            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    {
                        var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));
                        return new ExternalAuthenticateResultModel
                        {
                            AccessToken = accessToken,
                            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                            ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds
                        };
                    }
                case AbpLoginResultType.UnknownExternalLogin:
                    {
                        var newUser = await RegisterExternalUserAsync(externalUser);
                        if (!newUser.IsActive)
                        {
                            return new ExternalAuthenticateResultModel
                            {
                                WaitingForActivation = true
                            };
                        }
                        // Try to login again with newly registered user!
                        loginResult = await _logInManager.LoginAsync(new UserLoginInfo(model.AuthProvider, model.ProviderKey, model.AuthProvider), GetTenancyNameOrNull());
                        if (loginResult.Result != AbpLoginResultType.Success)
                        {
                            throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                                loginResult.Result,
                                model.ProviderKey,
                                GetTenancyNameOrNull()
                            );
                        }
                        return new ExternalAuthenticateResultModel
                        {
                            AccessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity)),
                            ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds
                        };
                    }
                default:
                    {
                        throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                            loginResult.Result,
                            model.ProviderKey,
                            GetTenancyNameOrNull()
                        );
                    }
            }
        }
        [HttpPost]
        public async Task<ExternalAuthenticateResultModel> ExternalAuthenticateSocial([FromBody] ExternalAuthenticateSocialModel externalAuthModel)
        {
            var externalUser = new ExternalAuthUserInfo()
            {
                Name = externalAuthModel.Name,
                Surname = externalAuthModel.SurName,
                EmailAddress = externalAuthModel.EmailAddress,
                Provider = externalAuthModel.AuthProvider,
                ProviderKey = externalAuthModel.ProviderKey
            };
            var loginResult = await _logInManager.LoginAsync(new UserLoginInfo(externalAuthModel.AuthProvider, externalAuthModel.ProviderKey, externalAuthModel.DisplayName), GetTenancyNameOrNull());
            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    {
                        var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));
                        return new ExternalAuthenticateResultModel
                        {
                            AccessToken = accessToken, //externalAuthModel.AccessToken,
                            EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                            ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds
                        };
                    }
                case AbpLoginResultType.UnknownExternalLogin:
                    {
                        var newUser = await RegisterExternalUserAsync(externalUser);
                        if (!newUser.IsActive)
                        {
                            return new ExternalAuthenticateResultModel
                            {
                                WaitingForActivation = true
                            };
                        }
                        // Try to login again with newly registered user!
                        loginResult = await _logInManager.LoginAsync(new UserLoginInfo(externalAuthModel.AuthProvider, externalAuthModel.ProviderKey, externalAuthModel.DisplayName), GetTenancyNameOrNull());
                        if (loginResult.Result != AbpLoginResultType.Success)
                        {
                            throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                                loginResult.Result,
                                externalAuthModel.ProviderKey,
                                GetTenancyNameOrNull()
                            );
                        }
                        return new ExternalAuthenticateResultModel
                        {
                            //AccessToken = externalAuthModel.AccessToken,
                            AccessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity)),
                            ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds
                        };
                    }
                default:
                    {
                        throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(
                            loginResult.Result,
                            externalAuthModel.ProviderKey,
                            GetTenancyNameOrNull()
                        );
                    }
            }
        }
        #region Login
        private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress, string password, string tenancyName)
        {
            var loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName);
            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    return loginResult;
                default:
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, usernameOrEmailAddress, tenancyName);
            }
        }
        #endregion
        #region Authenticate
        private string CreateAccessToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
        {
            var now = DateTime.UtcNow;
            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration.Issuer,
                audience: _configuration.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(expiration ?? _configuration.Expiration),
                signingCredentials: _configuration.SigningCredentials
            );
            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }
        private static List<Claim> CreateJwtClaims(ClaimsIdentity identity)
        {
            var claims = identity.Claims.ToList();
            var nameIdClaim = claims.First(c => c.Type == ClaimTypes.NameIdentifier);
            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            claims.AddRange(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, nameIdClaim.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            });
            return claims;
        }
        private string GetEncryptedAccessToken(string accessToken)
        {
            return SimpleStringCipher.Instance.Encrypt(accessToken, AppConsts.DefaultPassPhrase);
        }
        private void CleanUserVerificationCode(User user)
        {
            user.EmailConfirmationCode = null;
            user.IsEmailConfirmed = true;
        }
        #endregion
        #region External & Social
        private async Task<User> RegisterExternalUserAsync(ExternalAuthUserInfo externalUser)
        {
            var user = await _userRegistrationManager.RegisterAsync(
                externalUser.Name,
                externalUser.Surname,
                externalUser.EmailAddress,
                externalUser.EmailAddress,
                Authorization.Users.User.CreateRandomPassword(),
                true
            );
            user.Logins = new List<UserLogin>
            {
                new UserLogin
                {
                    LoginProvider = externalUser.Provider,
                    ProviderKey = externalUser.ProviderKey,
                    TenantId = user.TenantId
                }
            };
            await CurrentUnitOfWork.SaveChangesAsync();
            return user;
        }
        private async Task<ExternalAuthUserInfo> GetExternalUserInfo(ExternalAuthenticateModel model)
        {
            var userInfo = await _externalAuthManager.GetUserInfo(model.AuthProvider, model.ProviderAccessCode);
            if (userInfo.ProviderKey != model.ProviderKey)
            {
                throw new UserFriendlyException(L("CouldNotValidateExternalUser"));
            }
            return userInfo;
        }
        #endregion
        #region Tenant & User
        private string GetTenancyNameOrNull()
        {
            if (!AbpSession.TenantId.HasValue)
            {
                return null;
            }
            return _tenantCache.GetOrNull(AbpSession.TenantId.Value)?.TenancyName;
        }
        private User ResolveAuthenticatedUser()
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var userId = this.AbpSession.UserId;
                var currentUser = _userManager.GetUserById((long)userId);
                return currentUser;
            }
        }
        private async Task<User> FindByUserNameOrEmailAsync(string email)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var user = await _userManager.FindByNameOrEmailAsync(email);
                if (user == null)
                {
                    throw new EmailDoesntExistException(this.L("EmailDoesntExist"));
                }
                return user;
            }
        }
        private async Task<User> FindByUserIdAsync(long userId)
        {
            using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.MayHaveTenant))
            {
                var user = await _userManager.GetUserByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception("Invalid user id");
                }
                return user;
            }
        }
        #endregion
        #region Util
        private static bool DirectoryContains(string directory, string directoryChild)
        {
            return Directory.GetDirectories(directory).Any(dir => string.Equals(Path.GetFileName(dir), directoryChild));
        }
        #endregion
    }
}



