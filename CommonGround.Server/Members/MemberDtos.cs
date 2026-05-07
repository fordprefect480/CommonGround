namespace CommonGround.Server.Members;

public record AdminMeDto(
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    bool IsAdmin);

public record MemberDto(
    string Id,
    string? Email,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber,
    DateTime JoinedAt,
    bool EmailConfirmed,
    string[] Roles);

public record UpdateProfileDto(string? FirstName, string? LastName);

public record CreateMemberDto(
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string Password,
    bool IsAdmin);

internal static class MemberHelpers
{
    public static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
