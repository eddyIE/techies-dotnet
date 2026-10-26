namespace Techies.Web.Domain;

// Written by a customer after a completed order. Read-only everywhere in this app (BRD §6).
public sealed class Review
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid OrderId { get; set; }
    public short Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
