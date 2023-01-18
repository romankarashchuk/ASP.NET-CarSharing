﻿using CarSharingApp.Login;
using CarSharingApp.Login.Authentication;
using CarSharingApp.OptionsSetup;
using CarSharingApp.Payment;
using CarSharingApp.Payment.StripeService;
using CarSharingApp.Services;
using CarSharingApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Stripe;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using CarSharingApp.Middlewares;
using CarSharingApp.Repository.MongoDbRepository;
using CarSharingApp.Web.Clients;
using CarSharingApp.Web.Clients.Interfaces;
using CarSharingApp.Web.Clients.Extensions;
using IdentityModel.Client;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging((context, logging) =>
{
    //logging.ClearProviders();
    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    logging.AddDebug();
    //logging.AddConsole();
});

builder.Services.AddControllersWithViews();


builder.Services.AddSingleton<IVehicleServicePublicApiClient, VehicleServicePublicApiClient>();
builder.Services.RegisterNewHttpClients("VehiclesAPI", builder.Configuration);
builder.Services.AddSingleton<ICustomerServicePublicApiClient, CustomerServicePublicApiClient>();
builder.Services.RegisterNewHttpClients("CustomersAPI", builder.Configuration);
builder.Services.AddSingleton<IAuthorizationServicePublicApiClient, DuendeIdentityServerPublicApiClient>();
builder.Services.RegisterNewHttpClients("DuendeIdentityServerAPI", builder.Configuration);

//HttpClient client = new HttpClient();
//DiscoveryDocumentResponse document = await client.GetDiscoveryDocumentAsync("https://localhost:5001");
//if (document.IsError)
//{
//    throw new Exception(document.Error);
//}

//var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
//{
//    Address = document.TokenEndpoint,

//    ClientId = "gleb15a@gmail.com",
//    ClientSecret = "F2BC41266204CCAC2D5B52EF7F7AD1A656822479B6246A9AE1CEDD5BCC364DD4",
//    Scope = "accessShareNewVehiclePage"
//});

//if (tokenResponse.IsError)
//{
//    throw new Exception(tokenResponse.Error);
//}

//string token = tokenResponse.AccessToken;

//// call api
//var apiClient = new HttpClient();
//apiClient.SetBearerToken(tokenResponse.AccessToken);

//var response = await apiClient.GetAsync("https://localhost:44363/Authorization");
//if (!response.IsSuccessStatusCode)
//{
//    Console.WriteLine(response.StatusCode);
//}
//else
//{
//    var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
//    Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
//}









// Sessions setting
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Dependency injection is here
builder.Services.AddSingleton<IPaymentSessionProvider, StripeSessionProvider>();

builder.Services.AddScoped<IFileUploadService, LocalFileUploadService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddTransient<IJwtProvider, JwtProvider>();

// JWT options configuration setup is here
builder.Services.ConfigureOptions<JwtOptionsSetup>();

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    opt.RequireHttpsMetadata = false;
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidIssuers = new string[] { builder.Configuration["Jwt:Issuer"] },
        ValidAudiences = new string[] { builder.Configuration["Jwt:Audience"] },
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// DB configuration section
builder.Services.Configure<CarSharingDatabaseSettings>(
    builder.Configuration.GetSection("CarSharingLocalDB"));
builder.Services.AddSingleton<MongoDbService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseSession();

app.Use(async (context, next) =>
{
    var JWToken = context.Session.GetString("JWToken");
    if (!string.IsNullOrEmpty(JWToken))
    {
        context.Request.Headers.Add("Authorization", "Bearer " + JWToken);
    }
    await next();
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseRouting();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseAuthorization();

StripeConfiguration.ApiKey = "sk_test_51M6B0AGBXizEWSwDh5mkyk4o3DvKzmywGwJh7Fg2cpd9mxmhLiIPkARsFcvN3Yov0Qyshlqu8gITm3NGPPReXtbW00dvIu6aGa";

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
