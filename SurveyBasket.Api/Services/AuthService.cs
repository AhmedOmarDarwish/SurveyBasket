using System.Security.Cryptography;

namespace SurveyBasket.Services
{
    public class AuthService(UserManager<ApplicationUser> userManager, IJwtProvider jwtProvider) : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager  = userManager;
        private readonly IJwtProvider _jwtProvider  = jwtProvider;
        private readonly int _refreshTokenExpiryDays = 14;

        public async Task<Result<AuthResponse>> GetTokenAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            //Check User?
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null) return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            var isValidPassword= await _userManager.CheckPasswordAsync(user, password);
            if (!isValidPassword) return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);

            //Generate JWT Token
            var (token, expireIn) = _jwtProvider.GenerateToken(user);

            //Generate Refresh Token
            var refreshToken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.Now.AddDays(_refreshTokenExpiryDays);
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = refreshToken,
                ExpiresOn = refreshTokenExpiration,
            });
            await _userManager.UpdateAsync(user);

            var response = new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, token, expireIn, refreshToken, refreshTokenExpiration);

            return Result.Success(response); 
        }
        
        //public async Task<OneOf<AuthResponse, Error>> GetTokenAsync(string email, string password, CancellationToken cancellationToken = default)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);

        //    if (user is null)
        //        return UserErrors.InvalidCredentials;

        //    var isValidPassword = await _userManager.CheckPasswordAsync(user, password);

        //    if (!isValidPassword)
        //        return UserErrors.InvalidCredentials;

        //    var (token, expiresIn) = _jwtProvider.GenerateToken(user);
        //    var refreshToken = GenerateRefreshToken();
        //    var refreshTokenExpiration = DateTime.Now.AddDays(_refreshTokenExpiryDays);

        //    user.RefreshTokens.Add(new RefreshToken
        //    {
        //        Token = refreshToken,
        //        ExpiresOn = refreshTokenExpiration
        //    });

        //    await _userManager.UpdateAsync(user);

        //    return new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, token, expiresIn, refreshToken, refreshTokenExpiration);
        //}

        public async Task<Result<AuthResponse>> GetRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(token);
            if (userId is null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidJwtToken);

            var userRefreshToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken && x.IsActive);
            if (userRefreshToken is null)
                return Result.Failure<AuthResponse>(UserErrors.InvalidRefreshToken);

            userRefreshToken.RevokedOn = DateTime.Now;

            //Generate JWT Token
            var (newToken, expireIn) = _jwtProvider.GenerateToken(user);

            //Generate Refresh Token
            var newRefreshToken = GenerateRefreshToken();
            var refreshTokenExpiration = DateTime.Now.AddDays(_refreshTokenExpiryDays);
            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                ExpiresOn = refreshTokenExpiration,
            });
            await _userManager.UpdateAsync(user);

            var response = new AuthResponse(user.Id, user.Email, user.FirstName, user.LastName, newToken, expireIn, newRefreshToken, refreshTokenExpiration);

            return Result.Success(response);
        }

        public async Task<Result> RevokeRefreshTokenAsync(string token, string refreshToken, CancellationToken cancellationToken = default)
        {
            var userId = _jwtProvider.ValidateToken(token);
            if (userId is null)
                return Result.Failure(UserErrors.InvalidJwtToken);

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Result.Failure(UserErrors.InvalidJwtToken);

            var userRefreshToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken && x.IsActive);
            if (userRefreshToken is null)
                return Result.Failure(UserErrors.InvalidRefreshToken);

            userRefreshToken.RevokedOn = DateTime.Now;

            await _userManager.UpdateAsync(user);

            return Result.Success();
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

    }
}
