using CarSharingApp.Application.Interfaces;
using CarSharingApp.Application.Services;
using CarSharingApp.Domain.Entities;
using CarSharingApp.Infrastructure.MongoDB;
using CarSharingApp.Infrastructure.AzureKeyVault;
using CarSharingApp.Infrastructure.MSSQL;
using CarSharingApp.Infrastructure.AzureAD;
using CarSharingApp.Infrastructure.Authentication;
using CarSharingApp.Infrastructure.Authentication.Options;
using CarSharingApp.Infrastructure.MSSQL.Contexts;
using CarSharingApp.Infrastructure.MSSQL.Seeds;
using CarSharingApp.Infrastructure.Middlewares;
using CarSharingApp.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);
{
    #region Logger

    builder.Services.AddLogging();
    builder.Logging.ConfigureLogging();

    #endregion

    #region Middlewares

    builder.Services.AddTransient<GlobalExceptionHandlingMiddleware>();

    #endregion

    #region System Utilities

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(config =>
    {
        config.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Car Sharing Application Public API", Version = "v1" });
    });

    #endregion

    #region Azure Externals

    builder.Services.AddAzureKeyVaultAppsettingsValues(builder.Configuration);

    #endregion

    #region Mongo DB

    builder.Services.AddMongo(builder.Configuration, builder.Environment);
    builder.Services.AddMongoRepository<Vehicle>(builder.Configuration["MongoDbConfig:Collections:VehiclesCollectionName"] ?? "");
    builder.Services.AddSingleton<IVehicleService, VehicleService>();
    builder.Services.AddMongoRepository<Customer>(builder.Configuration["MongoDbConfig:Collections:CustomersCollectionName"] ?? "");
    builder.Services.AddSingleton<ICustomerService, CustomerService>();

    #endregion

    #region MSSQL Server

    builder.Services.AddMSSQLDBconnection(builder.Configuration);
    builder.Services.AddMSSQLRepository<ActionNote>();
    builder.Services.AddSingleton<IActionNotesService, ActionNotesService>();

    #endregion

    #region Authentication and Authorization

    builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();

    builder.Services.ConfigureOptions<JwtOptionsSetup>();
    builder.Services.AddTransient<IJwtProvider, JwtProvider>();
    builder.Services.AddJwtBearerAuthentication(builder.Configuration);
    //builder.Services.ConfigureAzureAD(builder.Configuration);

    #endregion
}

var app = builder.Build();
{
    app.Logger.LogInformation("App created...");

    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    app.Logger.LogInformation("Seeding Database...");

    using (var scope = app.Services.CreateScope())
    {
        var scopedProvider = scope.ServiceProvider;
        try
        {
            var catalogContext = scopedProvider.GetRequiredService<CarSharingAppContext>();
            await CarSharingAppContextSeed.SeedAsync(catalogContext, app.Logger);
            app.Logger.LogInformation("Migrations hisory was checked successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred seeding the DB and running down the migrations.");
        }
    }

    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Sharing Application Public API");
    });

    //app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Logger.LogInformation("LAUNCHING");

    app.Run();
}
