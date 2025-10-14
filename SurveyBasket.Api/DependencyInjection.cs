using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SurveyBasket.Health;
using System.Text;
using System.Threading.RateLimiting;

namespace SurveyBasket
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllers();

            //Add Distributed Memory Cache
            services.AddDistributedMemoryCache();

            //Add Hybrid Cache
            services.AddHybridCache();

            //Add Cors
            var allowOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                    builder
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .WithOrigins(allowOrigins!)
                );

                //options.AddPolicy("MyPolicy", builder =>
                //   builder
                //          .WithMethods()
                //          .AllowAnyHeader()
                //          .WithOrigins(allowOrigins!)
                //);
            }
            );

            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
             throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services
                .AddSwaggerServices()
                .AddSwaggerServices()
                .AddMapsterConfing()
                .AddFluentValidationConfing()
                .AddAuthConfing(configuration)
                .AddBackgroundJobsConfig(configuration)
                .AddRateLimitingConfig();
            


            services.AddScoped<IPollService, PollService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailSender, EmailService>();
            services.AddScoped<IQuestionService, QuestionService>();
            services.AddScoped<IVoteService, VoteService>();
            services.AddScoped<IResultService, ResultService>();
            services.AddScoped<ICacheService, CacheService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();


            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            //For Read Header and get Origin in AuthService
            services.AddHttpContextAccessor();

            //Map from AppSetting(Mail Section) to MailSetting Class 
            services.Configure<MailSettings>(configuration.GetSection(nameof(MailSettings)));

            
            //Add Health Checks
            services.AddHealthChecks()
                .AddSqlServer(name: "database", connectionString: connectionString)
                .AddHangfire(Options => { Options.MinimumAvailableServers = 1; })
                .AddUrlGroup(name: "external API", uri: new Uri("https://www.google.com")/*, tags: ["api"], httpMethod: HttpMethod.Get*/)
                .AddCheck<MailProviderHealthCheck>(name: "mail service");
            return services;
        }

        private static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            return services;
        }

        private static IServiceCollection AddMapsterConfing(this IServiceCollection services)
        {
            var mappingConfig = TypeAdapterConfig.GlobalSettings;
            mappingConfig.Scan(Assembly.GetExecutingAssembly());
            services.AddSingleton<IMapper>(new Mapper(mappingConfig));

            return services;
        }

        private static IServiceCollection AddFluentValidationConfing(this IServiceCollection services)
        {
            services
                 .AddFluentValidationAutoValidation()
                 .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        private static IServiceCollection AddAuthConfing(this IServiceCollection services, IConfiguration configuration)
        {
            //Add Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            //For Permissions
            services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

            services.AddSingleton<IJwtProvider, JwtProvider>();

            //For Jwt Configurations from App Setting
            //services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
            services.AddOptions<JwtOptions>()
                .BindConfiguration(JwtOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var jwtSettings = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

            services.AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                }
                ).AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.Key!)),
                        ValidIssuer = jwtSettings?.Issuer,
                        ValidAudience = jwtSettings?.Audience,
                    };
                });

            services.Configure<IdentityOptions>(options =>
             {
                 // Default Password settings.
                 options.Password.RequiredLength = 8;

                 // Default SignIn settings.
                 options.SignIn.RequireConfirmedEmail = true;

                 // Default User settings.
                 options.User.RequireUniqueEmail = true;
             });
            return services;
        }

        private static IServiceCollection AddBackgroundJobsConfig(this IServiceCollection services, IConfiguration configuration)
        {

            // Add Hangfire services.
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("HangfireConnection") ??
             throw new InvalidOperationException("Connection string 'HangfireConnection' not found.")));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

            return services;
        }

        private static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
        {
            //User Rate Limiter
            services.AddRateLimiter(rateLimiterOptions =>
            {
                rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                rateLimiterOptions.AddPolicy(RateLimiters.IpLimiter, HttpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            Window = TimeSpan.FromSeconds(20)
                        }
                    )
                );

                rateLimiterOptions.AddPolicy(RateLimiters.UserLimiter, HttpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: HttpContext.User.GetUserId(),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            Window = TimeSpan.FromSeconds(20)
                        }
                    )
                );

                rateLimiterOptions.AddConcurrencyLimiter(RateLimiters.Concurrency, options =>
                {
                    options.PermitLimit = 1000;
                    options.QueueLimit = 100;
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                //rateLimiterOptions.AddTokenBucketLimiter("token", options =>
                //{
                //    options.TokenLimit = 2;
                //    options.QueueLimit = 1;
                //    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                //    options.ReplenishmentPeriod = TimeSpan.FromSeconds(30);
                //    options.TokensPerPeriod = 1;
                //    options.AutoReplenishment = true;
                //});

                //rateLimiterOptions.AddFixedWindowLimiter("fixed", options =>
                //{
                //    options.PermitLimit = 2;
                //    options.Window = TimeSpan.FromSeconds(20);
                //    options.QueueLimit = 1;
                //    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;

                //});

                //rateLimiterOptions.AddSlidingWindowLimiter("sliding", options =>
                //{
                //    options.PermitLimit = 2;
                //    options.Window = TimeSpan.FromSeconds(20);
                //    options.SegmentsPerWindow = 2;
                //    options.QueueLimit = 1;
                //    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                //});

            });

            return services;
        }
    }
}
