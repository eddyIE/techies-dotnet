using Techies.Web.Domain;
using Techies.Web.ViewModels.Orders;

namespace Techies.Web.ViewModels.Customers;

public sealed record CustomerRow(Guid Id, string FullName, string Email, string Phone, short Tier, long Balance, AccountStatus Status);

public sealed record CustomerProfile(User User, CustomerLoyalty Loyalty, string TierName, List<Address> Addresses,
    List<PointTransaction> PointHistory, List<OrderRow> Orders);
