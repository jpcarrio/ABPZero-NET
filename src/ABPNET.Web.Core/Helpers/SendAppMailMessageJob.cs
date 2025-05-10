using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using ABPNET.Configuration;
using ABPNET.Models.Message;
using Microsoft.Extensions.Options;
namespace ABPNET.Helpers
{
    public class SendAppMailMessageJob : AsyncBackgroundJob<EmailModel>, ITransientDependency
    {
        private readonly SmtpClient _smtpClient;
        private readonly MailConfiguration _mailConfiguration;
        public SendAppMailMessageJob(IOptions<MailConfiguration> mailConfiguration)
        {
            _mailConfiguration = mailConfiguration.Value;
            _smtpClient = new SmtpClient(_mailConfiguration.Host, _mailConfiguration.Port)
            {
                EnableSsl = _mailConfiguration.SSL,
                Credentials = new NetworkCredential(_mailConfiguration.Username, _mailConfiguration.Password)
            };
        }
        public override async Task ExecuteAsync(EmailModel emailModel)
        {
            var mailMessage = new MailMessage
            {
                Subject = string.Format(!string.IsNullOrEmpty(emailModel.Subject) ? emailModel.Subject : emailModel.EmailTitle, _mailConfiguration.SenderName),
                IsBodyHtml = true
            };
            //
            // from
            //
            mailMessage.From = new MailAddress(_mailConfiguration.EmailFrom, _mailConfiguration.SenderName);
            //
            // to
            //
            mailMessage.To.Add(new MailAddress(emailModel.ToAddress, emailModel.ToDisplayName));
            //
            // embedded image
            //
            var img = await GetLinkedResource_ImagePngBase64(emailModel.AppLogo, "EmbeddedImageContent_1");
            //
            // alternate view (actual content)
            //
            var alternateView = AlternateView.CreateAlternateViewFromString(emailModel.Html, Encoding.UTF8, MediaTypeNames.Text.Html);
            alternateView.LinkedResources.Add(img);
            mailMessage.AlternateViews.Add(alternateView);
            //
            // Smtp client
            //
            await _smtpClient.SendMailAsync(mailMessage);
            mailMessage.Dispose();
        }
        private async Task<LinkedResource> GetLinkedResource_ImagePngBase64(string imageData, string contentId)
        {
            return await Task.Run(() =>
            {
                LinkedResource img;
                try
                {
                    byte[] bitmapData = Convert.FromBase64String(FixBase64ForImage(imageData));
                    var streamBitmap = new MemoryStream(bitmapData);
                    string mediaType = "image/png";
                    img = new LinkedResource(streamBitmap, mediaType)
                    {
                        ContentId = contentId
                    };
                    img.ContentType.MediaType = mediaType;
                    img.TransferEncoding = TransferEncoding.Base64;
                    img.ContentType.Name = img.ContentId;
                    img.ContentLink = new Uri("cid:" + img.ContentId);
                }
                catch (Exception ex)
                {
                    img = null;
                    Abp.Logging.LogHelper.LogException(ex);
                }
                return img;
            });
        }
        private static string FixBase64ForImage(string Image)
        {
            var sb = new StringBuilder(Image, Image.Length);
            sb.Replace("data:image/png;base64,", string.Empty);
            sb.Replace("\r", string.Empty);
            sb.Replace("\n", string.Empty);
            sb.Replace(" ", string.Empty);
            return sb.ToString();
        }
    }
}



