namespace Awai.Helpers
{
    /// <summary>
    /// Maps published product titles to Policy controller actions (IMS-style forms).
    /// Other products keep the classic Apply form.
    /// </summary>
    public static class PolicyProductRoutes
    {
        public const string Compulsory = "Compulsory";
        public const string OrangeCard = "OrangeCard";
        public const string Travelers = "Travelers";

        public static string? ActionFor(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            if (title.Contains("اجباري", StringComparison.OrdinalIgnoreCase)
                || title.Contains("إجباري", StringComparison.OrdinalIgnoreCase)
                || title.Contains("سيارة", StringComparison.OrdinalIgnoreCase) && title.Contains("إجبار", StringComparison.OrdinalIgnoreCase))
                return Compulsory;

            if (title.Contains("برتقال", StringComparison.OrdinalIgnoreCase)
                || title.Contains("عربية موحدة", StringComparison.OrdinalIgnoreCase))
                return OrangeCard;

            if (title.Contains("مسافر", StringComparison.OrdinalIgnoreCase))
                return Travelers;

            return null;
        }
    }
}
