using Techies.Web.Domain;

namespace Techies.Web.ViewModels.Orders;

public sealed class OrderFilter
{
    public OrderStatus? Status { get; set; }
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
}

public sealed record OrderRow(Guid Id, string OrderRef, string Recipient, string Phone, DateTimeOffset CreatedAt,
    decimal Total, PaymentMethod PaymentMethod, PaymentStatus PaymentStatus, OrderStatus Status);
