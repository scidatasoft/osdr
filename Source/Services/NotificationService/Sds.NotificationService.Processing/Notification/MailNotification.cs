using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using Sds.NotificationService.Processing.Monitoring;
using Serilog;
using System.Threading.Tasks;

namespace Sds.NotificationService.Processing.Notification
{
    public class MailNotification
    {
        public List<Service> CheckingServices { get; }
        public string Password { get; }
        public UserMail From { get; }
        public List<UserMail> To { get; }
        private IConfiguration _configurations;
        
        public MailNotification(IConfiguration config)
        {
            _configurations = config;

            Log.Information("Mail settings: ");

            To = new List<UserMail>();
            From = CreateUserMail(config["mailServer:from:mail"]);
            Password = config["mailServer:from:password"];

            Log.Information($"from: {From.Mail}");
            Log.Information("to: ");

            for (var i = 0; config[$"mailServer:to:{i}"] != null; i++)
            {
                var mail = config[$"mailServer:to:{i}"];

                var userMail = CreateUserMail(mail);
                To.Add(userMail);

                Log.Information($"{i+1}. {userMail.Mail}");
            }

            CheckingServices = new List<Service>();
        }
        
        public void Notify(List<Service> services)
        {
            var message = new StringBuilder("services down");
            message.AppendLine("");
            var shouldBeSend = false;

            services.ForEach(service =>
            {
                var isNewContainsService = IsCheckingService(service);

                if (isNewContainsService)
                {
                    message.AppendLine(service.ToString());
                    message.AppendLine("");

                    CheckingServices.Add(service);
                    shouldBeSend = true;
                }
            });

            if (shouldBeSend)
            {
                Log.Information(message.ToString());
                Log.Information(new string('-', 30));
                To.ForEach(to =>
                {
                    SendMail(to.Host, From.Mail, Password, to.Mail, to.Port, "monitoring service", message.ToString());
                });
            }
            else CheckingServices.Clear();
        }
        private bool IsCheckingService(Service newService)
        {
            bool isNew = true;
            CheckingServices.ForEach(service =>
            {
                if (service.Type == newService.Type &&
                    service.Monitor == newService.Monitor &&
                    service.Host == newService.Host &&
                    service.Error == newService.Error &&
                    service.Up == newService.Up &&
                    service.TimeStamp.ToString() == newService.TimeStamp.ToString())
                    isNew = false;
            });

            return isNew;
        }
        
        public static async void SendMail(string smtpServer, string from, string password,
            string mailto, int port, string caption, string message, string attachFile = null)
        {
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(from),
                    To = { new MailAddress(mailto) },
                    Subject = caption,
                    Body = message
                };

                if (!string.IsNullOrEmpty(attachFile))
                    mail.Attachments.Add(new Attachment(attachFile));

                var client = new SmtpClient
                {
//                    Host = smtpServer,
//                    Port = port,
//                    EnableSsl = true,
                    UseDefaultCredentials = true,
//                    Credentials = new NetworkCredential(from.Split('@')[0], password),
//                    DeliveryMethod = SmtpDeliveryMethod.Network
                    DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis
                };
                
//                client.Credentials = (ICredentialsByHost) CredentialCache.DefaultCredentials;
                
//                client.UseDefaultCredentials = true;

                await client.SendMailAsync(mail);
                
                mail.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"Mail send: {e.Message}");
            }
        }
        
        private UserMail CreateUserMail(string mail)
        {
            var hostName = mail.Substring(mail.IndexOf("@")+1, mail.Length - mail.LastIndexOf(".")+1);
            var isSSL = false;
            var port = -1;
            var host = "";

            for (var i = 0; _configurations[$"mailServer:config:{i}:name"] != null; i++)
            {
                var configName = _configurations[$"mailServer:config:{i}:name"]; 

                if (hostName == configName)
                {
                    host = _configurations[$"mailServer:config:{i}:host"];
                    isSSL = bool.Parse(_configurations[$"mailServer:config:{i}:isSSL"]);
                    port = int.Parse(_configurations[$"mailServer:config:{i}:port"]);
                    
                    break;
                }
            }
            var user = new UserMail(mail, host, port, isSSL);

            return user;
        }
    }
}