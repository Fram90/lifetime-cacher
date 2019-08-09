using System;
using Microsoft.Extensions.DependencyInjection;

namespace ObjectLifetimeCacher.Extensions
{
    public static class Extensions
    {
        public static void AddScopeCached<TInterface, TImplementation>(this IServiceCollection collection)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            collection.AddScoped<TImplementation>();
            collection.AddScoped<TInterface>(provider =>
            {
                var service = provider.GetService<TImplementation>();
                return LifetimeCacheDecorator<TInterface>.Create(service);
            });
        }

        public static void AddScopeCached<TInterface, TImplementation>(this IServiceCollection collection,
            Func<IServiceProvider, TImplementation> implFactory)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (implFactory == null)
            {
                throw new ArgumentNullException(nameof(implFactory));
            }

            collection.AddScoped<TInterface>(provider =>
            {
                var svc = implFactory(provider);
                return LifetimeCacheDecorator<TInterface>.Create(svc);
            });
        }
    }
}
