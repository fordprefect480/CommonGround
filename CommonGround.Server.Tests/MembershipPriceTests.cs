using CommonGround.Server.Members;

namespace CommonGround.Server.Tests;

public class MembershipPriceTests
{
    [Theory]
    [InlineData(100)]      // $1.00 - minimum
    [InlineData(2500)]     // $25.00 - current
    [InlineData(1_000_000)] // $10,000 - maximum
    public void Valid_amounts_pass(int cents)
    {
        Assert.True(MembershipPrice.TryValidate(cents, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]        // below $1.00
    [InlineData(1_000_001)] // above $10,000
    public void Invalid_amounts_fail_with_message(int cents)
    {
        Assert.False(MembershipPrice.TryValidate(cents, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
