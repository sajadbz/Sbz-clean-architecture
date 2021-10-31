using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Sbz.Application.Common.Interfaces;

namespace Sbz.Infrastructure.Services
{
    public class EmailService : IEmailService
    {


        public bool SendEmail(string to, string subject, string body)
        {
            try
            {
                var settingEmail = new Setting()
                {
                    Smtp = "domain.com",
                    DisplayName = "Sajad Bagherzadeh",
                    EmailAddress= "no-reply@domain.com",
                    EmailPassword = "password",
                    SmtpPort = 25,// 587 - 25;
                    EnableSsl = false                     
                };
                SmtpClient smtp = new SmtpClient(settingEmail.Smtp);
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(settingEmail.EmailAddress, settingEmail.DisplayName);
                mailMessage.To.Add(to);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                smtp.Port = settingEmail.SmtpPort;
                smtp.Credentials = new NetworkCredential(settingEmail.EmailAddress, settingEmail.EmailPassword);
                smtp.EnableSsl = settingEmail.EnableSsl;
                smtp.Send(mailMessage);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }
        private class Setting
        {            
            public string Smtp { get; set; }
            public string EmailAddress { get; set; }
            public string DisplayName { get; set; }
            public int SmtpPort { get; set; }
            public string EmailPassword { get; set; }         
            public bool EnableSsl { get; set; }
        }
    }
    
}
