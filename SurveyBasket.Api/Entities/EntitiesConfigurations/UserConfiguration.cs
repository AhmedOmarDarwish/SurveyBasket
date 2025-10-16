namespace SurveyBasket.Entities.EntitiesConfigurations
{
    public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder
                .OwnsMany(x => x.RefreshTokens)
                .ToTable("RefreshTokens")
                .WithOwner()
                .HasForeignKey("UserId");

            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);


            //Default Data
           // var passwordHasher = new PasswordHasher<ApplicationUser>();
            builder.HasData(new ApplicationUser
            {
                Id = DefaultUsers.Admin.Id,
                FirstName = DefaultUsers.Admin.FirstName,
                LastName = DefaultUsers.Admin.LastName,
                Email = DefaultUsers.Admin.Email,
                UserName = DefaultUsers.Admin.UserName,
                NormalizedEmail = DefaultUsers.Admin.Email.ToUpper(),
                NormalizedUserName = DefaultUsers.Admin.UserName.ToUpper(),
                SecurityStamp = DefaultUsers.Admin.SecurityStamp,
                ConcurrencyStamp = DefaultUsers.Admin.ConcurrencyStamp,
                EmailConfirmed = true,
                //PasswordHash = passwordHasher.HashPassword(null,DefaultUsers.AdminPassword)
                PasswordHash = DefaultUsers.Admin.PasswordHasher
            });

        }
    }
}
