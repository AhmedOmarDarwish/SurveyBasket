

namespace SurveyBasket
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();
            
            //Read Connection String From AppSetting
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
          
            services.AddDbContext<ApplicationDbContext>(option => option.UseSqlServer(connectionString));
           
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            services.AddOpenApi();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IPollService, PollService>();

  
             services
                .AddMapsterConfig()
                .AddFluentValidationConfig()
                .AddAuthConfig();
           

             return services;
        }

        private static IServiceCollection AddMapsterConfig(this IServiceCollection services)
        {
            var mappingConfig = TypeAdapterConfig.GlobalSettings;
            mappingConfig.Scan(Assembly.GetExecutingAssembly());

            services.AddSingleton<IMapper>(new Mapper(mappingConfig));

            return services;
        }

        private static IServiceCollection AddFluentValidationConfig(this IServiceCollection services)
        {
            services
                .AddFluentValidationAutoValidation()
                .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        private static IServiceCollection AddAuthConfig(this IServiceCollection services)
        {
            services.AddSingleton<IJwtProvider, JwtProvider>();
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.SaveToken = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wKJFixsIo7kU6OBbw8u9qheJFUlNYL1V")),
                    ValidIssuer = "SurveyBasketApp",
                    ValidAudience = "SurveyBasketApp users"
                };
            });

            return services;
        }
    }
}
 