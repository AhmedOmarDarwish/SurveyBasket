namespace SurveyBasket.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
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
        public async Task<IActionResult> RefreshAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.GetRefreshTokenAsync(token: request.Token, refreshToken: request.RefreshToken, cancellationToken);

            return authResult.IsSuccess 
                ? Ok(authResult.Value)
                : authResult.ToProblem();
        }

        [HttpPut("revoke-refresh-token")]
        public async Task<IActionResult> RevokeRefreshTokenAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
        {
            var authResult = await _authService.RevokeRefreshTokenAsync(token: request.Token, refreshToken: request.RefreshToken, cancellationToken);

            return authResult.IsSuccess 
                ? Ok()
                : authResult.ToProblem();
        }
    }

}
