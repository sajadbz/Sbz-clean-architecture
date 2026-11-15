using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sbz.Application.Common.Interfaces;
using Sbz.Application.Common.Mappings;
using Sbz.Application.Tasks;
using Sbz.Infrastructure.Persistence;
using Sbz.Infrastructure.Services;

namespace Sbz.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            var connectionString = configuration.GetConnectionString("Tasks") ?? "Data Source=sbz-tasks.db";
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
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

        public static void InitializeTaskDatabase(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
        }
    }
}
