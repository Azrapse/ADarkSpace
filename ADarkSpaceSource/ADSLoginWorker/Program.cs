using ADSCommon.Data;
using ADSCommon.Entities;
using ADSCommon.Services;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container
builder.Services.Configure<AdsStoreDatabaseSettings>(builder.Configuration.GetSection("AdsStoreDatabase"));
builder.Services.AddSingleton<AdsStoreService>();

var app = builder.Build();

app.MapPost("/Login", async (LoginData loginData, AdsStoreService storeService) =>
{
    var player = await storeService.GetPlayerByLoginAsync(loginData.UserName, loginData.Password);
    if (player is null)
    {
        return Results.BadRequest("Invalid username or password");
    }
    return Results.Ok(player);
});

app.MapPost("/Register", async (RegisterData registerData, AdsStoreService storeService) =>
{
    var player = await storeService.GetPlayerByUserNameAsync(registerData.UserName);
    if (player is not null)
    {
        return Results.BadRequest("Username already exists");
    }
    if (registerData.Password != registerData.ConfirmPassword)
    {
        return Results.BadRequest("Passwords do not match");
    }
    
    // ToDo: Assign the player a ship in the game.

    var newPlayer = new Player
    {
        UserName = registerData.UserName,
        Password = registerData.Password,
        EMail = registerData.Email,
        Name = registerData.DisplayName
    };
    await storeService.CreatePlayerAsync(newPlayer);        

    return Results.Ok(newPlayer);
});

app.Run();
