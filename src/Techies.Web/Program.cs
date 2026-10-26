using Techies.Web.Data;
using Techies.Web.Infrastructure;
using Techies.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAppDatabase(builder.Configuration);
builder.Services.AddRouting(o => o.LowercaseUrls = true);
// Emit Vietnamese text as-is instead of &#x...; entities.
builder.Services.AddWebEncoders(o => o.TextEncoderSettings =
    new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddAppAuthentication();
builder.Services.AddScoped<AccountStatusService>();
builder.Services.AddScoped<StaffAuthService>();
builder.Services.AddScoped<CustomerAccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<StockLedger>();
builder.Services.AddScoped<CouponService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddScoped<GiftService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderQueries>();
builder.Services.AddScoped<ShopQueries>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<CustomerQueries>();
builder.Services.AddScoped<ReviewQueries>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<RewardsService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<DashboardQueries>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".Techies.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromDays(7);
});
builder.Services.AddScoped(sp => new Techies.Web.Infrastructure.ImageStorage(
    sp.GetRequiredService<IWebHostEnvironment>().WebRootPath,
    sp.GetRequiredService<TimeProvider>().GetUtcNow()));

// Behind a reverse proxy (e.g. a tunnel) the request arrives as plain http from localhost; trust the
// proxy's X-Forwarded-* headers so redirects and absolute URLs use the public https scheme and host.
// Only loopback proxies are trusted (the default), so a remote client can't spoof these headers.
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost);

var app = builder.Build();

app.UseForwardedHeaders();

await app.MigrateAndSeedAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
public partial class Program;
