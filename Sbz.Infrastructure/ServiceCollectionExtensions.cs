using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sbz.Application.Common.Interfaces;
using Sbz.Application.Common.Mappings;
using Sbz.Infrastructure.Services;

namespace Sbz.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddSingleton(typeof(ILoggerManager<>), typeof(LoggerManager<>));

            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddTransient<IDateTime, DateTimeService>();
            services.AddTransient<IFileService, FileService>();

            services.AddAutoMapper(typeof(MappingProfile).Assembly);
            return services;
        }
    }
}
