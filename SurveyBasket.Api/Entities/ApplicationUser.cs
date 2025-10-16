namespace SurveyBasket.Entities
{
    public sealed class ApplicationUser: IdentityUser
    {
        public ApplicationUser()
        {
            //Create Guid version 7
            Id = Guid.CreateVersion7().ToString();
            SecurityStamp = Guid.CreateVersion7().ToString();
        }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsDisabled { get; set; } = false;

        public List<RefreshToken> RefreshTokens { get; set; } = [];
    }  
}
