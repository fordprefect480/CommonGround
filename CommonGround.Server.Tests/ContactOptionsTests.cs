using CommonGround.Server.Configuration;

namespace CommonGround.Server.Tests;

public class ContactOptionsTests
{
    [Theory]
    [InlineData(" 0x4AAAAAADj7WNGJxZvgMJA1", "0x4AAAAAADj7WNGJxZvgMJA1")] // leading space (the prod outage)
    [InlineData("0x4AAAAAADj7WNGJxZvgMJA1 ", "0x4AAAAAADj7WNGJxZvgMJA1")] // trailing space
    [InlineData("\t key \n", "key")]
    public void Turnstile_keys_are_trimmed(string raw, string expected)
    {
        var options = new ContactOptions { TurnstileSiteKey = raw, TurnstileSecretKey = raw };

        Assert.Equal(expected, options.TurnstileSiteKey);
        Assert.Equal(expected, options.TurnstileSecretKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_turnstile_keys_become_null(string? raw)
    {
        var options = new ContactOptions { TurnstileSiteKey = raw, TurnstileSecretKey = raw };

        Assert.Null(options.TurnstileSiteKey);
        Assert.Null(options.TurnstileSecretKey);
        Assert.False(options.CaptchaEnabled);
    }

    [Fact]
    public void Captcha_enabled_when_both_keys_set_even_with_whitespace()
    {
        var options = new ContactOptions
        {
            TurnstileSiteKey = " site ",
            TurnstileSecretKey = " secret ",
        };

        Assert.True(options.CaptchaEnabled);
    }
}
