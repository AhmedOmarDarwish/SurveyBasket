namespace SurveyBasket.Errors
{
    public record RoleErrors
    {
        public static readonly Error RoleNotFound = 
            new("Role.RoleNotFound", "Role is not found", StatusCodes.Status404NotFound);

        public static readonly Error DuplicatedRole =
            new("Role.DuplicatedName", "Another Role in the same name is already exists", StatusCodes.Status409Conflict);

        public static readonly Error InvalidPermission =
             new("Role.InvalidPermission", "Invalid Permission", StatusCodes.Status400BadRequest);
    }
}
