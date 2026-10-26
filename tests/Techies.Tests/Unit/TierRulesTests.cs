using Techies.Web.Domain;
using Techies.Web.Services;

namespace Techies.Tests.Unit;

public class TierRulesTests
{
    private static readonly MembershipTier[] Tiers =
    [
        new() { Tier = 0, ThresholdPoints = 0 }, new() { Tier = 1, ThresholdPoints = 10_000 },
        new() { Tier = 2, ThresholdPoints = 30_000 }, new() { Tier = 3, ThresholdPoints = 60_000 },
    ];

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9_999, 0)]
    [InlineData(10_000, 1)]
    [InlineData(59_999, 2)]
    [InlineData(1_000_000, 3)]
    public void TierFor_picks_highest_threshold_reached(long points, short expected) =>
        Assert.Equal(expected, LoyaltyService.TierFor(points, Tiers));
}
