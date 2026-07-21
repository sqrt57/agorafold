using AgoraFold.BlazorWasm.Client.Api.Dto.Categories;

namespace AgoraFold.BlazorWasm.Client.Api;

public sealed class CategoryApiClient(ApiClient api)
{
    public Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<CategoryResponse>>("api/categories", cancellationToken);
}
