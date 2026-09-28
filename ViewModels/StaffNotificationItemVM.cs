namespace Awai.ViewModels
{
    public class StaffNotificationItemVM
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CreatedLocal { get; set; } = string.Empty;
    }
}
