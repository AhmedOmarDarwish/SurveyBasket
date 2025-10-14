using Microsoft.AspNetCore.RateLimiting;

namespace SurveyBasket.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [EnableRateLimiting(RateLimiters.IpLimiter)]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.GetTokenAsync(email: request.Email, password: request.Password, cancellationToken);

            return authResult.IsSuccess
                ? Ok(authResult.Value)
                : authResult.ToProblem();

            //return authResult.Match(
            //    Ok,
            //    error => Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Code, detail: error.Description)
            //);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.GetRefreshTokenAsync(token: request.Token, refreshToken: request.RefreshToken, cancellationToken);

            return authResult.IsSuccess
                ? Ok(authResult.Value)
                : authResult.ToProblem();
        }

        [HttpPut("revoke-refresh-token")]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RevokeRefreshTokenAsync(token: request.Token, refreshToken: request.RefreshToken, cancellationToken);

            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        [HttpPost("register")]
        [DisableRateLimiting]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.ConfirmEmailAsync(request, cancellationToken);
            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        [HttpPost("resend-confirm-email")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResendConfirmationEmailAsync(request, cancellationToken);
            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        [HttpPost("forget-password")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.SendResetPasswordCodeAsync(request.Email, cancellationToken);
            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.ResetPasswordAsync(request, cancellationToken);
            return result.IsSuccess
                ? Ok()
                : result.ToProblem();
        }

        //[HttpGet("test")]
        //[EnableRateLimiting("token")]
        //public IActionResult Test()
        //{
        //    Thread.Sleep(6000);
        //    return Ok();
        //}
    }

}
