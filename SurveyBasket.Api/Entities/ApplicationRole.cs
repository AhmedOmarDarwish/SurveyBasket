namespace SurveyBasket.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public bool IsDefault { get; set; } = default;

        public bool IsDeleted { get; set; } = default;

    }
}
