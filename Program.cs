using Awai.Classes;
using Awai.Hubs;
using Awai.Models;
using Awai.Models.Entities;
using Awai.Models.Interfaces;
using Awai.Services;
using Awai.Services.Ims;
using Awai.Services.Payments;
using StackExchange.Redis;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(typeof(IUnitOfWork<>), typeof(Awai.Models.UnitOfWork.UnitOfWork<>));
builder.Services.AddScoped<IApplicationSubmissionService, LocalApplicationSubmissionService>();
builder.Services.AddScoped<IStaffNotificationService, StaffNotificationService>();
builder.Services.Configure<ImsOptions>(builder.Configuration.GetSection("Ims"));
builder.Services.AddSingleton<ImsTokenCache>();
builder.Services.AddHttpClient<ImsApiClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(40);
});

builder.Services.Configure<PaymentOptions>(builder.Configuration.GetSection("Payments"));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redis = builder.Configuration.GetSection("Payments")["Redis"];
    var config = ConfigurationOptions.Parse(string.IsNullOrWhiteSpace(redis) ? "localhost:6379" : redis);
    config.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddSingleton<RedisPaymentLock>();
builder.Services.AddSingleton<RabbitPaymentBus>();
builder.Services.AddSingleton<IPaymentGateway, UnconfiguredPaymentGateway>();
builder.Services.AddScoped<IPaymentFlow, PaymentFlow>();
builder.Services.AddScoped<PaymentProcessor>();
builder.Services.AddHostedService<PaymentWorkerHost>();
builder.Services.AddHostedService<StuckPaymentRecoveryHost>();

builder.Services.AddDbContext<AppDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("DbCon")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAutoMapper(typeof(MappingProfile));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/hubs/notifications");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

await IdentitySeeder.SeedAsync(app.Services, app.Configuration);
await ProductCatalogSeeder.EnsureAsync(app.Services);

app.Run();
