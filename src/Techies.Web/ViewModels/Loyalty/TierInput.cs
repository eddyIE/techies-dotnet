namespace Techies.Web.ViewModels.Loyalty;

public sealed class TierInput
{
    public short Tier { get; set; }
    public string Name { get; set; } = "";
    public long ThresholdPoints { get; set; }
    public decimal DiscountPercent { get; set; }
}
