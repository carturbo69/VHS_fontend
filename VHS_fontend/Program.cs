using VHS_fontend.Models;
using VHS_frontend.Services;
using VHS_frontend.Services.Provider;

var builder = WebApplication.CreateBuilder(args);

// Bind ApiSettings
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

// Đăng ký HttpClient cho ProviderProfileService
builder.Services.AddHttpClient<ProviderProfileService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    Console.WriteLine($">>> ProviderProfileService BaseUrl = {baseUrl}");
    client.BaseAddress = new Uri(baseUrl ?? throw new InvalidOperationException("BaseUrl missing in config"));
});

// Đăng ký HttpClient cho AuthService
builder.Services.AddHttpClient<AuthService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    Console.WriteLine($">>> AuthService BaseUrl = {baseUrl}");
    client.BaseAddress = new Uri(baseUrl ?? throw new InvalidOperationException("BaseUrl missing in config"));
});
builder.Services.AddHttpClient<ProviderStaffService>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
    client.BaseAddress = new Uri(baseUrl!);
});


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
