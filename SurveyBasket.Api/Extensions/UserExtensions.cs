namespace SurveyBasket.Extensions
{
    public static class UserExtensions
    {
        public static string? GetUserId (this ClaimsPrincipal user)
        {
            return user.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public static string? GetUserName(this ApplicationUser user)
        {
            if (user == null) return null;

            return $"{user.FirstName} {user.LastName}";
        }

    }
}
