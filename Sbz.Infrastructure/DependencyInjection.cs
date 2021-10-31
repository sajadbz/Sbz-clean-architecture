using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sbz.Application.Common.Interfaces;
using Sbz.Infrastructure.Services;

namespace Sbz.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {           

            services.AddSingleton(typeof(ILoggerManager<>), typeof(LoggerManager<>));
                                    
            services.AddScoped<IViewRenderService, ViewRenderService>();            
            services.AddScoped<ICookieService, CookieService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddTransient<IDateTime, DateTimeService>();
            return services;
        }
    }
}
