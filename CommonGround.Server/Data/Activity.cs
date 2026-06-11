namespace CommonGround.Server.Data;

public class Activity
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string ActivityType { get; set; } = "";
    public string? ActorUserId { get; set; }
    public ApplicationUser? Actor { get; set; }
    public string? ActorEmailSnapshot { get; set; }
    public string Summary { get; set; } = "";
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? DetailsJson { get; set; }
}
