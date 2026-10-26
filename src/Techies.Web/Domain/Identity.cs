namespace Techies.Web.Domain;

public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public List<Address> Addresses { get; set; } = [];
    public CustomerLoyalty? Loyalty { get; set; }
}

public sealed class Address
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string RecipientName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string Ward { get; set; } = "";
    public string District { get; set; } = "";
    public string Province { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Staff
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = "";
    public StaffRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
