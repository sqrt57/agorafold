using AgoraFold.Core.Services;
using AgoraFold.Core.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AgoraFold.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AgoraFold.Core's business services. Does not register <see cref="AppDbContext"/>
    /// (each variant does that itself) and does not set <see cref="ListingImageStorageOptions.RootPath"/> —
    /// the caller must configure that separately, since Core has no notion of e.g. wwwroot.
    /// </summary>
    public static IServiceCollection AddAgoraFoldCore(this IServiceCollection services)
    {
        services.AddOptions<ListingImageStorageOptions>();
        services.AddScoped<IListingImageStorage, LocalDiskListingImageStorage>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IListingImageService, ListingImageService>();
        services.AddScoped<IConversationService, ConversationService>();

        return services;
    }
}
