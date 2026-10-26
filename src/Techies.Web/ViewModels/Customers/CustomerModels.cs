using Techies.Web.Domain;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.ViewModels.Customers;

public sealed record CustomerRow(Guid Id, string FullName, string Email, string Phone, short Tier, long Balance, AccountStatus Status);

public sealed record CustomerProfile(User User, CustomerLoyalty Loyalty, string TierName, List<Address> Addresses,
    List<PointTransaction> PointHistory, List<OrderRow> Orders);

public sealed class ReviewFilter
{
    public short? Rating { get; set; }
    public bool LowOnly { get; set; }
    public Guid? ProductId { get; set; }
    public int Page { get; set; } = 1;
}

public sealed record ReviewRow(Guid Id, Guid ProductId, string ProductName, Guid CustomerId, string CustomerName,
    short Rating, string? Comment, DateTimeOffset CreatedAt);
