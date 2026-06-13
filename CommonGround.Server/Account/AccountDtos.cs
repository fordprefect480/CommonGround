namespace CommonGround.Server.Account;

public record MeDto(
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? PhoneNumber,
    string? Address,
    string[] SecondaryMembers,
    DateTime? MembershipPaidThroughUtc,
    bool IsAdmin,
    bool IsSubscribedToMailingList);

public record UpdateProfileDto(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Address,
    string[]? SecondaryMembers,
    bool IsSubscribedToMailingList);
