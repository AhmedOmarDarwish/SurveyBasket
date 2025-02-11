using System.Text;

namespace SurveyBasket.Authentication
{
    public class JwtProvider : IJwtProvider
    {
        public (string token, int expiresIn) GenerateToken(ApplicationUser user)
        {
            Claim[] claims = [
                new(JwtRegisteredClaimNames.Sub, user.Id),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(JwtRegisteredClaimNames.GivenName, user.FirstName),
                new(JwtRegisteredClaimNames.FamilyName, user.LastName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())             
                ];

            //Key For Encode and Decode
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wKJFixsIo7kU6OBbw8u9qheJFUlNYL1V"));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);
            var expiresIn = 30;
            var ecpirationDate = DateTime.UtcNow.AddMinutes(expiresIn);
            var token = new JwtSecurityToken(
                issuer: "SurveyBasketApp",
                audience: "SurveyBasketApp users",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresIn),
                signingCredentials: signingCredentials
                );

            return (token: new JwtSecurityTokenHandler().WriteToken(token), expiresIn: expiresIn * 60);
        }

    }
}
