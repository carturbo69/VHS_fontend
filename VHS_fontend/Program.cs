using VHS_fontend.Models;
using VHS_frontend.Models;
using VHS_frontend.Services;
using VHS_frontend.Services.Provider;

var builder = WebApplication.CreateBuilder(args);

// 🧩 Đọc cấu hình API
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

// 🟢 Đăng ký HttpClient cho các service gọi BE
void AddHttpClientWithBaseUrl<TService>()
    where TService : class
{
    builder.Services.AddHttpClient<TService>(client =>
    {
        var baseUrl = builder.Configuration["ApiSettings:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            throw new InvalidOperationException("❌ BaseUrl missing in appsettings.json");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
}

// 🧱 Đăng ký tất cả service gọi API Backend
AddHttpClientWithBaseUrl<ProviderProfileService>();
AddHttpClientWithBaseUrl<ProviderStaffService>();
AddHttpClientWithBaseUrl<ProviderService>();
AddHttpClientWithBaseUrl<AuthService>();

// 🧠 Thiết lập Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".VHS.Session";

    // ✅ Cho phép gửi cookie cross-site từ FE (7161 HTTPS) sang BE (5154 HTTP)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// 🧩 Bổ sung HttpContextAccessor để đọc/ghi Session trong Controller
builder.Services.AddHttpContextAccessor();

// 🧩 MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ⚙️ Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ⚠️ Session phải nằm trước Authorization
app.UseSession();

// 🧩 Bật CORS cho phép FE truy cập BE
app.UseCors(policy =>
    policy
        .AllowAnyOrigin()      // hoặc .WithOrigins("http://localhost:5154") nếu bạn muốn chặt chẽ hơn
        .AllowAnyHeader()
        .AllowAnyMethod()
);

app.UseAuthorization();

// 🗺️ Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=HomePage}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
