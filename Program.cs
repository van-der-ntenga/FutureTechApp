using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Azure.Cosmos;
using FutureTechApp.Services;
using FututreTechApp.Services;
using Microsoft.AspNetCore.Authentication.OAuth;
using Azure.Storage;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();
builder.Logging.AddFilter("Microsoft.Azure.Cosmos", LogLevel.Debug);


builder.Services.AddControllersWithViews();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    options.SlidingExpiration = true; // Reset the expiration time on each request
    options.LoginPath = "/Account/Login"; // Redirect to login page if not authenticated
    options.LogoutPath = "/Account/Logout"; // Redirect to logout page
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
    options.Events = new OAuthEvents
    {
        OnRemoteFailure = context =>
        {
            // Handle the failure case
            context.Response.Redirect("/Account/Error?message=" + context.Failure.Message);
            context.HandleResponse(); 
            return Task.CompletedTask;
        }
    };

});


builder.Services.AddAuthorization(options =>
{

    options.AddPolicy("RequireAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddSingleton(s =>
{
    var config = s.GetRequiredService<IConfiguration>();

    var cosmosClientOptions = new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        ApplicationName = "FutureTechApp",
        EnableContentResponseOnWrite = false
    };

    var endpoint = config["CosmosDb:Endpoint"];
    var key = config["CosmosDb:Key"];

    return new CosmosClient(endpoint, key, cosmosClientOptions);
});

//builder.Services.AddTransient<CosmosTestService>();
builder.Services.AddTransient<StudentService>();



builder.Services.AddSingleton<BlobServiceClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var accountName = configuration["BlobStorage:AccountName"];
    var accountKey = configuration["BlobStorage:AccountKey"];
    var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");

    var credential = new StorageSharedKeyCredential(accountName, accountKey);

    return new BlobServiceClient(blobUri, credential);
});


var app = builder.Build();


//using (var scope = app.Services.CreateScope())
//{
//    var testService = scope.ServiceProvider.GetRequiredService<CosmosTestService>();
//    await testService.RunTestAsync();
//}


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();


app.MapGet("/", async context =>
{
    if (context.User.Identity.IsAuthenticated)
    {
        context.Response.Redirect("/Home/Dashboard");
    }
    else
    {
        context.Response.Redirect("/Home/Index");
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
