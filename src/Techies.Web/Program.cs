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
builder.Services.AddScoped(sp => new Techies.Web.Infrastructure.ImageStorage(
    sp.GetRequiredService<IWebHostEnvironment>().WebRootPath,
    sp.GetRequiredService<TimeProvider>().GetUtcNow()));

var app = builder.Build();

await app.MigrateAndSeedAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRouting();

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
