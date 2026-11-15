using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sbz.Application.Accounts;
using Sbz.Application.Common.Interfaces;
using Sbz.Application.Common.Mappings;
using Sbz.Application.Tasks;
using Sbz.Infrastructure.Accounts;
using Sbz.Infrastructure.Persistence;
using Sbz.Infrastructure.Services;

namespace Sbz.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            var connection = new SqliteConnectionStringBuilder(
                configuration.GetConnectionString("Tasks") ?? "Data Source=sbz-tasks.db")
            {
                ForeignKeys = true
            };
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection.ToString()));
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Lockout.MaxFailedAccessAttempts = 5;
            }).AddEntityFrameworkStores<ApplicationDbContext>();
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/denied";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            // Revoked or changed accounts must stop authorizing existing sessions on the next request.
            services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

            services.AddScoped<AdministratorBootstrap>();
            services.AddScoped<IAccountAdministration, IdentityAccountAdministration>();
            services.AddScoped<ITaskAdministration, SqliteTaskAdministration>();
            services.AddScoped<ITaskStore, SqliteTaskStore>();

            services.AddSingleton(typeof(ILoggerManager<>), typeof(LoggerManager<>));

            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddTransient<IDateTime, DateTimeService>();
            services.AddTransient<IFileService, FileService>();

            services.AddAutoMapper(options => options.LicenseKey = configuration["AutoMapper:LicenseKey"], typeof(MappingProfile).Assembly);
            return services;
        }

        public static void InitializeDatabase(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
            scope.ServiceProvider.GetRequiredService<AdministratorBootstrap>().InitializeAsync().GetAwaiter().GetResult();
        }
    }
}
