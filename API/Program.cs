var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

string dbConnectionString = (builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
            + "Database=MunicipalityDB;";
builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));
builder.Services.AddSingleton(new DAL.ReportDB(dbConnectionString));

var app = builder.Build();

string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

string? adminEmail = builder.Configuration["Admin:Email"];
string? adminPassword = builder.Configuration["Admin:Password"];
if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
{
    await app.Services.GetRequiredService<DAL.UserDB>().EnsureAdminAccountAsync(adminEmail, adminPassword);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
string apiWebRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(apiWebRootPath);
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
