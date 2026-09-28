namespace Awai.Helpers
{
    public static class MapLink
    {
        public static string ForAddress(string? address, string? preferredUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredUrl))
                return preferredUrl.Trim();

            if (string.IsNullOrWhiteSpace(address))
                return "https://www.google.com/maps";

            return "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(address.Trim());
        }
    }

    public static class ApplicationStatusText
    {
        public static string Arabic(Awai.Models.Entities.ApplicationStatus status) => status switch
        {
            Awai.Models.Entities.ApplicationStatus.Submitted => "جديد / مُرسل",
            Awai.Models.Entities.ApplicationStatus.Reviewed => "تمت المراجعة",
            Awai.Models.Entities.ApplicationStatus.Synced => "معتمد / مكتمل",
            Awai.Models.Entities.ApplicationStatus.Failed => "فشل / مرفوض",
            _ => status.ToString()
        };

        public static string Css(Awai.Models.Entities.ApplicationStatus status) => status switch
        {
            Awai.Models.Entities.ApplicationStatus.Submitted => "text-bg-primary",
            Awai.Models.Entities.ApplicationStatus.Reviewed => "text-bg-success",
            Awai.Models.Entities.ApplicationStatus.Synced => "text-bg-success",
            Awai.Models.Entities.ApplicationStatus.Failed => "text-bg-danger",
            _ => "text-bg-secondary"
        };
    }
}
