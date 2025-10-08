
namespace SurveyBasket.Services
{
    public class UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IRoleService roleService ) : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager = userManager;
        private readonly ApplicationDbContext _context = context;
        private readonly IRoleService _roleService = roleService;

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default) =>
            await (from u in _context.Users
                   join ur in _context.UserRoles
                   on u.Id equals ur.UserId into userRoles // group join
                   from ur in userRoles.DefaultIfEmpty() // LEFT JOIN for get all user without role
                   join r in _context.Roles
                   on ur.RoleId equals r.Id into roles
                   where !roles.Any(x=> x.Name == DefaultRoles.Member)
                   select new
                       {
                           u.Id,
                           u.FirstName,
                           u.LastName,
                           u.Email,
                           u.IsDisabled,
                           Roles = roles.Select(x => x.Name!).ToList()
                       }
                   )
                    .GroupBy(u => new {u.Id, u.FirstName, u.LastName, u.Email, u.IsDisabled})
                    .Select(u => new UserResponse(
                        u.Key.Id,
                        u.Key.FirstName,
                        u.Key.LastName,
                        u.Key.Email,
                        u.Key.IsDisabled,
                        u.SelectMany(x => x.Roles)
                        ))
                    .ToListAsync(cancellationToken);

        public async Task<Result<UserResponse>> GetAsync(string id,  CancellationToken cancellationToken = default)
        {
            if (await _userManager.FindByIdAsync(id) is not { } user)
                return Result.Failure<UserResponse>(UserErrors.UserNotFound);

            var userRoles = await _userManager.GetRolesAsync(user);
            var response = (user, userRoles).Adapt<UserResponse>();

            return Result.Success(response);
        }

        public async Task<Result<UserResponse>> AddAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var emailIsExists = await _userManager.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
            if(emailIsExists) return Result.Failure<UserResponse>(UserErrors.DuplicatedEmail);

            var allowedRoles = await _roleService.GetAllAsync(cancellationToken: cancellationToken);
            if(request.Roles.Except(allowedRoles.Select(x=> x.Name)).Any())
                return Result.Failure<UserResponse>(UserErrors.InvalidRole);

            var user = request.Adapt<ApplicationUser>();

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRolesAsync(user, request.Roles);
                var response = (user, request.Roles).Adapt<UserResponse>();
                return Result.Success(response);
            }

            var error = result.Errors.First();
            return Result.Failure<UserResponse>(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
            
        }

        public async Task<Result> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var emailIsExists = await _userManager.Users.AnyAsync(u => u.Email == request.Email && u.Id != id, cancellationToken);
            if (emailIsExists) return Result.Failure(UserErrors.DuplicatedEmail);

            var allowedRoles = await _roleService.GetAllAsync(cancellationToken: cancellationToken);
            if (request.Roles.Except(allowedRoles.Select(x => x.Name)).Any())
                return Result.Failure(UserErrors.InvalidRole);

            if(await _userManager.FindByIdAsync(id) is not { } user)
                return Result.Failure(UserErrors.UserNotFound);

            user = request.Adapt(user);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _context.UserRoles
                    .Where(x=> x.UserId == id)
                    .ExecuteDeleteAsync(cancellationToken);

                await _userManager.AddToRolesAsync(user, request.Roles);
                var response = (user, request.Roles).Adapt<UserResponse>();
                return Result.Success(response);
            }

            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));

        }

        public async Task<Result> ToggleStatus(string id)
        {
            if (await _userManager.FindByIdAsync(id) is not { } user)
                return Result.Failure(UserErrors.UserNotFound);
            user.IsDisabled = !user.IsDisabled;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Result.Success();

            var error = result.Errors.First();
                return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        public async Task<Result> Unlock(string id)
        {
            if (await _userManager.FindByIdAsync(id) is not { } user)
                return Result.Failure(UserErrors.UserNotFound);

            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
                return Result.Success();

            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default)
        {
            //var user = await _userManager.FindByIdAsync(userId);
         
            //Use .Users to custom data response not get all data
            var user = await _userManager.Users
                .Where(u => u.Id == userId)
                .ProjectToType<UserProfileResponse>()
                .FirstAsync(cancellationToken); 

            return Result.Success(user);
        }

        public async Task<Result> UpdateProfileAsync(string userId, UpdateProfileRequest request , CancellationToken cancellationToken = default )
        {
            //var user = await _userManager.FindByIdAsync(userId);
            //user = request.Adapt(user);
            //await _userManager.UpdateAsync(user!);

            //New Way and Best Way
            await _userManager.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setters =>
                    setters
                        .SetProperty(x => x.FirstName , request.FirstName)
                        .SetProperty(x => x.LastName , request.LastName)
                );
            return Result.Success();
        }

        public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var result = await _userManager.ChangePasswordAsync(user!, request.CurrentPassword, request.NewPassword);
            if(result.Succeeded) return Result.Success(user);
          
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }
    }
}
