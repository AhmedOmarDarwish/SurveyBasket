namespace SurveyBasket.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole()
        {
            //Create Guid version 7
            Id = Guid.CreateVersion7().ToString();
        }
        public bool IsDefault { get; set; } 

        public bool IsDeleted { get; set; }

    }
}
