using Finance_Tracker.Data;
using Finance_Tracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

//  Identity configuration
builder.Services.AddIdentity<Applicationuser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbcontext>()
.AddDefaultTokenProviders();

//  Database
builder.Services.AddDbContext<ApplicationDbcontext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Data")));

// Session & HTTP context
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure IIS server options
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 104857600; // Increase max request body size (in bytes)
});

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"];);
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

//  Middleware for Conditional Routing
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();
    var isAuthenticated = context.User.Identity?.IsAuthenticated == true;

    // Adjust routing based on authentication status
    if (isAuthenticated && (path == "/" || path == "/home/index"))
    {
        context.Response.Redirect("/Transaction/Dashboard");
        return;
    }
    else if (!isAuthenticated && path.StartsWith("/transaction"))
    {
        context.Response.Redirect("/Home/Index");
        return;
    }

    await next();
});

// Default Route Configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
