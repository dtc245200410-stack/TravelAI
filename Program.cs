using Microsoft.AspNetCore.Authentication.Cookies;
using TravelAI.Services;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;
using TravelAI.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ================= DATABASE =================

builder.Services.AddDbContext<TravelDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ================= AUTHENTICATION =================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") context.Response.StatusCode = 401;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest") context.Response.StatusCode = 403;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// ================= MVC =================

builder.Services.AddControllersWithViews();

builder.Services.AddOptions<AIOptions>().BindConfiguration("AI")
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out var uri) &&
        uri.IsLoopback && uri.Scheme == "http", "AI:BaseUrl phải là địa chỉ HTTP loopback của Ollama local.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.Model) && !o.Model.Contains("coder", StringComparison.OrdinalIgnoreCase),
        "Chọn model tổng quát, không dùng coder.")
    .Validate(o => o.ResponseTimeoutSeconds is >= 5 and <= 60 && o.GenerationTimeoutSeconds >= 1 &&
        o.GenerationTimeoutSeconds < o.ResponseTimeoutSeconds && o.ContextLength is >= 2048 and <= 16384 &&
        o.HistoryMessages is >= 0 and <= 20 && o.MaxResponseTokens is >= 64 and <= 1024, "Cấu hình AI không hợp lệ.")
    .ValidateOnStart();
builder.Services.AddSingleton<AIRequestGate>();
builder.Services.AddHttpClient<OllamaService>((provider, http) =>
{
    http.BaseAddress = new Uri(provider.GetRequiredService<IOptions<AIOptions>>().Value.BaseUrl);
    http.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddHostedService<OllamaWarmupService>();
builder.Services.AddScoped<TravelAIService>();

builder.Services.AddScoped<AdminAITools>();

builder.Services.AddScoped<AdminAIAgentService>();

var app = builder.Build();

// ================= SEED DATA =================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<TravelDbContext>();

    await DbSeeder.SeedAsync(context);
}

// ================= PIPELINE =================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication phải đứng trước Authorization
app.UseAuthentication();
app.UseAuthorization();

// ================= ROUTING =================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
public partial class Program { }
