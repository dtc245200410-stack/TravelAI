using Microsoft.AspNetCore.Authentication.Cookies;
using TravelAI.Services;
using Microsoft.EntityFrameworkCore;
using TravelAI.Data;

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
    });

// ================= MVC =================

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<OpenAIService>();

builder.Services.AddScoped<OllamaService>();

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