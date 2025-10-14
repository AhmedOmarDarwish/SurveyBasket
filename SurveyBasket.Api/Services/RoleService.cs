namespace SurveyBasket.Services
{
    public class RoleService(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context) : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager = roleManager;
        private readonly ApplicationDbContext _context = context;

        public async Task<IEnumerable<RoleResponse>> GetAllAsync(bool? includeDisabled = false, CancellationToken cancellationToken = default) =>
              await _roleManager.Roles
            .Where(x => !x.IsDefault && (!x.IsDeleted || (includeDisabled.HasValue && includeDisabled.Value)))
            .ProjectToType<RoleResponse>()
            .ToListAsync(cancellationToken);

        public async Task<Result<RoleDetailResponse>> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (await _roleManager.FindByIdAsync(id) is not { } role)
                return Result.Failure<RoleDetailResponse>(RoleErrors.RoleNotFound);

            var permissions = await _roleManager.GetClaimsAsync(role);
            var response = new RoleDetailResponse(
                role.Id,
                role.Name!,
                role.IsDeleted,
                permissions.Select(x => x.Value));

            return Result.Success(response);
        }

        public async Task<Result<RoleDetailResponse>> AddAsync(RoleRequest request, CancellationToken cancellationToken = default)
        {
            var roleIsExists = await _roleManager.RoleExistsAsync(request.Name);

            if (roleIsExists) 
                return Result.Failure<RoleDetailResponse>(RoleErrors.DuplicatedRole);

            var allowedPermission = Permissions.GetAllPermissions();

            //Check if any permission not from permissions 
            if (request.Permissions.Except(allowedPermission).Any())
                return Result.Failure<RoleDetailResponse>(RoleErrors.InvalidPermission);

            var role = new ApplicationRole{
                Name = request.Name,
                ConcurrencyStamp = Guid.NewGuid().ToString(),
            };

            var result = await _roleManager.CreateAsync(role);
            if (result.Succeeded)
            {
                var permission = request.Permissions.Select(x => new IdentityRoleClaim<string>
                {
                    ClaimType = Permissions.Type,
                    ClaimValue = x,
                    RoleId = role.Id
                });

                await _context.AddRangeAsync(permission, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var response = new RoleDetailResponse(
                    role.Id,
                    role.Name!,
                    role.IsDeleted,
                    request.Permissions); 

                return Result.Success(response);
            }

            var error = result.Errors.First();
            return Result.Failure<RoleDetailResponse>(
                new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        public async Task<Result> UpdateAsync(string id, RoleRequest request, CancellationToken cancellationToken = default)
        {
            var roleIsExists = await _roleManager.Roles.AnyAsync(x => x.Name == request.Name && x.Id != id);

            if (roleIsExists)
                return Result.Failure(RoleErrors.DuplicatedRole);

            if(await _roleManager.FindByIdAsync(id) is not { } role)
                return Result.Failure(RoleErrors.RoleNotFound);


            var allowedPermission = Permissions.GetAllPermissions();
            //Check if any permission not from permissions 
            if (request.Permissions.Except(allowedPermission).Any())
                return Result.Failure<RoleDetailResponse>(RoleErrors.InvalidPermission);

            role.Name = request.Name;
            var result = await _roleManager.UpdateAsync(role);
            if (result.Succeeded)
            {
                //var currentPermission = await _roleManager.GetClaimsAsync(role);
                var currentPermission = await _context.RoleClaims
                    .Where(x => x.RoleId == id && x.ClaimType == Permissions.Type)
                    .Select(x => x.ClaimValue)
                    .ToListAsync(cancellationToken);

                var newPermissions = request.Permissions.Except(currentPermission)
                    .Select( x=> new IdentityRoleClaim<string>
                    {
                        ClaimType = Permissions.Type,
                        ClaimValue = x,
                        RoleId = id
                    });

                var removedPermission = currentPermission.Except(request.Permissions);

                await _context.RoleClaims
                    .Where(x => x.RoleId == id && removedPermission.Contains(x.ClaimValue))
                    .ExecuteDeleteAsync(cancellationToken);

                await _context.AddRangeAsync(newPermissions, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Success();
            }

            var error = result.Errors.First();
            return Result.Failure(
                new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        public async Task<Result> ToggleStatusAsync(string id, CancellationToken cancellationToken = default)
        {
            if (await _roleManager.FindByIdAsync(id) is not { } role)
                return Result.Failure(RoleErrors.RoleNotFound);

            role.IsDeleted = !role.IsDeleted;
            await _roleManager.UpdateAsync(role);
            return Result.Success();
        }

    }
}
