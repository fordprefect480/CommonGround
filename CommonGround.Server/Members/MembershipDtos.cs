namespace CommonGround.Server.Members;

public sealed record SignupRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Address,
    string Password,
    string[]? SecondaryMembers,
    bool SubscribeNewsletter,
    string? CaptchaToken);

public sealed record SignupResult(string CheckoutUrl);

public sealed record CompleteRequest(string SessionId);

public sealed record CompleteResult(bool Ok);
