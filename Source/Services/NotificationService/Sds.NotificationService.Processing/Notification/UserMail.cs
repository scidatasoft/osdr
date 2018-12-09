namespace Sds.NotificationService.Processing.Notification
{
    public class UserMail
    {
        public int Port { get; }
        public string Mail { get; }
        public string Host { get; }
        public bool IsSSL { get; }
        public UserMail(string mail, string host, int port, bool isSSL)
        {
            Mail = mail;
            Host = host;
            Port = port;
            IsSSL = isSSL;
        }
    }
}