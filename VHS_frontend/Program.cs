using VHS_frontend.Models;
using VHS_frontend.Services;
using VHS_frontend.Services.Admin;
using VHS_frontend.Services.Customer;
using VHS_frontend.Services.Customer.Implementations;
using VHS_frontend.Services.Customer.Interfaces;
using VHS_frontend.Services.Provider;

var builder = WebApplication.CreateBuilder(args);

// DI HttpClient dùng đúng BaseAddress
var backendBase = builder.Configuration["Apis:Backend"];
if (string.IsNullOrWhiteSpace(backendBase))
    throw new InvalidOperationException("Missing configuration: Apis:Backend");

builder.Services.AddHttpClient<ReviewServiceCustomer>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

builder.Services.AddHttpClient<ProviderFeedbackService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

builder.Services.AddHttpClient<IServiceCustomerService, ServiceCustomerService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

builder.Services.AddHttpClient<CartServiceCustomer>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

builder.Services.AddHttpClient<BookingServiceCustomer>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

builder.Services.AddHttpClient<CategoryAdminService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<TagAdminService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<CustomerAdminService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<ProviderAdminService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<RegisterProviderService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<AdminRegisterProviderService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<AdminVoucherService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<AdminFeedbackService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<NotificationService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<AdminNotificationService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<VHS_frontend.Services.Provider.ProviderNotificationService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});
builder.Services.AddHttpClient<AdminBookingService>(client =>
{
    client.BaseAddress = new Uri(backendBase.TrimEnd('/')); // => https://localhost:7154
});

// ServiceShopService
builder.Services.AddScoped<ServiceShopService>();

// Connect VNPay API
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<MoMoService>();

// HttpClient cho AuthService
builder.Services.AddHttpClient<AuthService>();

builder.Services.AddControllersWithViews();


builder.Services.AddHttpContextAccessor();


// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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
app.UseAuthorization();

// Route cho Areas trước
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
