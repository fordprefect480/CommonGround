using Microsoft.AspNetCore.DataProtection;

namespace CommonGround.Server.Email;

public sealed class UnsubscribeTokenService
{
    private const string ProtectorPurpose = "CommonGround.Email.Unsubscribe.v1";

    private readonly IDataProtector _protector;

    public UnsubscribeTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    public string CreateToken(string userId) => _protector.Protect(userId);

    public bool TryDecodeUserId(string? token, out string userId)
    {
        userId = "";
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            userId = _protector.Unprotect(token);
            return userId.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
