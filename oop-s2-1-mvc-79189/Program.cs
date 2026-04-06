using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using oop_s2_1_mvc_79189.Data;
using Serilog;
using Serilog.Events;

using oop_s2_1_mvc_79189.Middleware;
using System;


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "VgcCollege")
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Application} {Message:lj}{NewLine}{Exception}"
    )
    .CreateBootstrapLogger();

try
{
    Log.Information("VgcCollege starting up");

    var builder = WebApplication.CreateBuilder(args);

    // ?? Serilog ????????????????????????????????????????????
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "VgcCollege")
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Application} {Message:lj}{NewLine}{Exception}"
        ));

    // ?? Database ???????????????????????????????????????????
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));



    builder.Services.AddDefaultIdentity<IdentityUser>()
     .AddRoles<IdentityRole>()
     .AddEntityFrameworkStores<AppDbContext>();

    // ?? MVC ????????????????????????????????????????????????
    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    // ?? Middleware ?????????????????????????????????????????
    app.UseMiddleware<oop_s2_1_mvc_79189.Middleware.ExceptionHandlingMiddleware>();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    app.MapRazorPages();

    // ?? Seed ???????????????????????????????????????????????
    using (var scope = app.Services.CreateScope())
    {
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}