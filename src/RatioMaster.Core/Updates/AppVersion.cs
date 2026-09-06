namespace RatioMaster.Core.Updates;

/// <summary>Version identity of this build, as the website's update check expects it.</summary>
public static class AppVersion
{
    /// <summary>Human-readable version shown in the UI.</summary>
    public const string Public = "1.0.0";

    /// <summary>The four-digit id sent to and compared against /vc.php. Ordinal comparison, so it stays above "0430".</summary>
    public const string CheckId = "1000";

    public const string ReleaseDate = "2026-09-06";

    public const string WebsiteUrl = "https://ratiomaster.net";

    public const string GitHubUrl = "https://github.com/NikolayIT/RatioMaster.NET";

    public const string ForumUrl = "https://github.com/NikolayIT/RatioMaster.NET/discussions";

    public const string BugReportUrl = "https://github.com/NikolayIT/RatioMaster.NET/issues/new";

    public const string SupportEmail = "ratiomaster@nikolay.it";

    public const string DonateUrl =
        "https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=PG6NUT5YWYF82&lc=BG&item_name=RatioMaster%2eNET&item_number=RatioMaster%2eNET&currency_code=USD&bn=PP%2dDonationsBF%3abtn_donateCC_LG%2egif%3aNonHosted";
}
