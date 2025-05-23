using AutoMapper;
using Ecommerce.Application.Mappings;

namespace Ecommerce.Api.Extensions.ServiceCollection
{
    public static class AutoMapperExtensions
    {
        public static IServiceCollection AddCustomAutoMapper(this IServiceCollection services)
        {
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });

            IMapper mapper = mapperConfig.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }
    }
}
