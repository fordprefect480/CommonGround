namespace CommonGround.Server.Members;

public record MemberDto(
    string Id,
    string? Email,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber,
    string? Address,
    DateTime JoinedAt,
    DateTime? MembershipPaidThroughUtc,
    bool EmailConfirmed,
    bool IsSubscribedToMailingList,
    string[] SecondaryMembers,
    string[] Roles);

public record CreateMemberDto(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string Password,
    bool IsAdmin,
    bool IsSubscribedToMailingList);

internal static class MemberHelpers
{
    public static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
