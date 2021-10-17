using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Sbz.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Application Configuration
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
