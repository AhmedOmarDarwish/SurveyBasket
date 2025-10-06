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
            var passwordHasher = new PasswordHasher<ApplicationUser>();
            builder.HasData(new ApplicationUser
            {
                Id = DefaultUsers.AdminId,
                FirstName = DefaultUsers.AdminFirstName,
                LastName = DefaultUsers.AdminLastName,
                Email = DefaultUsers.AdminEmail,
                UserName = DefaultUsers.AdminUserName,
                NormalizedEmail = DefaultUsers.AdminEmail.ToUpper(),
                NormalizedUserName = DefaultUsers.AdminUserName.ToUpper(),
                SecurityStamp = DefaultUsers.AdminSecurityStamp,
                ConcurrencyStamp = DefaultUsers.AdminConcurrencyStamp,
                EmailConfirmed = true,
                //PasswordHash = passwordHasher.HashPassword(null,DefaultUsers.AdminPassword)
                PasswordHash = DefaultUsers.AdminPasswordHasher
            });

        }
    }
}
