using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDependencies(builder.Configuration);

//Add Serilog another Config in App Settings
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//Add Serilog
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

//Hangfire
app.UseHangfireDashboard("/jobs", new DashboardOptions
{
    Authorization =
    [
        new HangfireCustomBasicAuthenticationFilter
        {
            User = app.Configuration.GetValue<string>("HangfireSettings:Username"),
            Pass = app.Configuration.GetValue<string>("HangfireSettings:Password")
        }
    ],
    DashboardTitle = "Survey BasketDashboard",  
    //Disable Dashboard checkbox
   // IsReadOnlyFunc = (DashboardContext context) => true
});

//Add Scope To access to INotificationService
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using var scope = scopeFactory.CreateScope();
var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
RecurringJob.AddOrUpdate
    ("SendNewPollsNotification",
    () => notificationService.SendNewPollsNotification(null)
    ,Cron.Daily);



//app.UseCors("MyPolicy");
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

//Use RateLimiter
app.UseRateLimiter();

//Add Health checks
app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
    //If I need To sperate them with tags
app.MapHealthChecks("health-check-api", new HealthCheckOptions
{
    Predicate = x => x.Tags.Contains("api"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
