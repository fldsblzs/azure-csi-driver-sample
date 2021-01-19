namespace AzureCsiDriver.Application.Configuration
{
    public class NotificationOptions
    {
        public const string Notification = "Notification";
        
        public bool ShouldSendErrorNotifications { get; set; }
        public string MessagingQueue { get; set; }
        public string MessagingUserName { get; set; }
        public string MessagingPassword { get; set; }
    }
}