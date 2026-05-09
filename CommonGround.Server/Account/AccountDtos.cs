namespace CommonGround.Server.Account;

public record MeDto(
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    bool IsAdmin,
    bool IsSubscribedToMailingList);

public record UpdateProfileDto(
    string? FirstName,
    string? LastName,
    bool IsSubscribedToMailingList);
