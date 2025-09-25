
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

//app.UseCors("MyPolicy");
app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler();

app.Run();
