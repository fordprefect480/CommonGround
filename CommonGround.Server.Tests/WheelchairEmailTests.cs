using CommonGround.Server.LeasedBeds;

namespace CommonGround.Server.Tests;

public class WheelchairEmailTests
{
    [Fact]
    public void Applied_text_includes_the_wheelchair_line_only_when_required()
    {
        var withFlag = LeasedBedEmails.BuildAppliedText("Sam", 5, "https://x/", requiresWheelchairAccessible: true);
        var without = LeasedBedEmails.BuildAppliedText("Sam", 5, "https://x/", requiresWheelchairAccessible: false);

        Assert.Contains("needs a wheelchair-accessible bed", withFlag);
        Assert.DoesNotContain("wheelchair", without);
    }
}
