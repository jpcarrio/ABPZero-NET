using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Abp.BackgroundJobs;
using Abp.Dependency;
using ABPNET.Configuration;
using ABPNET.Models.TokenAuth;
using Microsoft.Extensions.Options;
namespace ABPNET.Helpers
{
    public class SendMailMessageJob : AsyncBackgroundJob<EmailSendingArgs>, ITransientDependency
    {
        private readonly MailConfiguration _mailConfiguration;
        public SendMailMessageJob(IOptions<MailConfiguration> mailConfiguration)
        {
            _mailConfiguration = mailConfiguration.Value;
        }
        public override async Task ExecuteAsync(EmailSendingArgs args)
        {
            var mailMessage = new MailMessage
            {
                Subject = args.Subject,
                IsBodyHtml = true,
                Body = args.Body,
                From = new MailAddress(args.From, args.Sender)
            };
            mailMessage.To.Add(new MailAddress(args.To, args.Sender));
            var smtpClient = new SmtpClient(_mailConfiguration.Host, _mailConfiguration.Port)
            {
                EnableSsl = _mailConfiguration.SSL,
                Credentials = new NetworkCredential(_mailConfiguration.Username, _mailConfiguration.Password)
            };
            await smtpClient.SendMailAsync(mailMessage);
            mailMessage.Dispose();
        }
    }
}



