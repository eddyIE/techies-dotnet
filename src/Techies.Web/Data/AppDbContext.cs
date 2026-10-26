using Microsoft.EntityFrameworkCore;
using Techies.Web.Domain;

namespace Techies.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<MembershipTier> MembershipTiers => Set<MembershipTier>();
    public DbSet<CustomerLoyalty> CustomerLoyalty => Set<CustomerLoyalty>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSpec> ProductSpecs => Set<ProductSpec>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<Gift> Gifts => Set<Gift>();
    public DbSet<GiftRedemption> GiftRedemptions => Set<GiftRedemption>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(Configurations.OrderConfiguration.RefSequence).StartsAt(100001);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (type.IsEnum)
                property.SetValueConverter((Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter)
                    Activator.CreateInstance(typeof(UpperSnakeEnumConverter<>).MakeGenericType(type))!);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(19, 2);
    }
}
