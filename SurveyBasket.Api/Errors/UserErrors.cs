﻿namespace SurveyBasket.Errors
{
    public static class UserErrors
    {
        public static readonly Error InvalidCredentials = 
            new("User.InvalidCredentials", "Invalid email/password", StatusCodes.Status401Unauthorized);
        
        public static readonly Error UserNotFound =
         new("User.NotFound", "No user was found with the given ID", StatusCodes.Status404NotFound);

        public static readonly Error InvalidJwtToken =
            new("User.InvalidJwtToken", "Invalid Jwt token", StatusCodes.Status401Unauthorized);

        public static readonly Error InvalidRefreshToken =
            new("User.InvalidRefreshToken", "Invalid refresh token", StatusCodes.Status401Unauthorized);
        
        public static readonly Error DuplicatedEmail =
            new("User.DuplicatedEmail", "Another user with the same name is already exists", StatusCodes.Status409Conflict); 

        public static readonly Error EmailNotConfirmed =
            new("User.EmailNotConfirmed", "Email is not confirmed", StatusCodes.Status401Unauthorized);
        
        public static readonly Error InvalidCode =
            new("User.InvalidCode", "Invalid Code", StatusCodes.Status401Unauthorized);
        
        public static readonly Error DuplicatedConfirmation =
            new("User.DuplicatedConfirmation", "Email already confirmed", StatusCodes.Status400BadRequest);
    }
}
