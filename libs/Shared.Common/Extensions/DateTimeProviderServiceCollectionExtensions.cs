using Microsoft.Extensions.DependencyInjection;
using Shared.Common.Abstractions;

namespace Shared.Common.Extensions;

public static class DateTimeProviderServiceCollectionExtensions
{
    public static IServiceCollection AddSharedDateTimeProvider(this IServiceCollection services) =>
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
}
