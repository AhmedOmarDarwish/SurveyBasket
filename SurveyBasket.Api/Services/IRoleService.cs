namespace SurveyBasket.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleResponse>> GetAllAsync(bool? includeDisabled = false, CancellationToken cancellationToken = default);
        Task<Result<RoleDetailResponse>> GetAsync(string id, CancellationToken cancellationToken = default);
        Task<Result<RoleDetailResponse>> AddAsync(RoleRequest roleRequest, CancellationToken cancellationToken = default);
        Task<Result> UpdateAsync(string id, RoleRequest request, CancellationToken cancellationToken = default);
        Task<Result> ToggleStatusAsync(string id, CancellationToken cancellationToken = default);
    }
}
