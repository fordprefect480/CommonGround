using CommonGround.Server.LeasedBeds;

namespace CommonGround.Server.Tests;

public class LeasedBedPriceTests
{
    [Theory]
    [InlineData(0)]      // $0 is allowed - a free term skips payment.
    [InlineData(100)]
    [InlineData(8000)]
    [InlineData(1_000_000)]
    public void Valid_amounts_pass(int cents)
    {
        Assert.True(LeasedBedPrice.TryValidate(cents, out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5000)]
    public void Negative_amounts_fail(int cents)
    {
        Assert.False(LeasedBedPrice.TryValidate(cents, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Above_maximum_fails()
    {
        Assert.False(LeasedBedPrice.TryValidate(LeasedBedPrice.MaxCents + 1, out var error));
        Assert.NotNull(error);
    }
}
