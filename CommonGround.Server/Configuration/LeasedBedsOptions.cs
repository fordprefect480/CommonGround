namespace CommonGround.Server.Configuration;

public sealed class LeasedBedsOptions
{
    public const string SectionName = "LeasedBeds";

    /// <summary>Where leased-bed admin notifications (applications, waiting-list joins) are sent.</summary>
    public string AdminNotificationEmail { get; set; } = "seafordcg@gmail.com";
}
