using ADSTestApp.Data;
using ADSTestApp.Entities;
using ADSTestApp.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.Configure<AdsStoreDatabaseSettings>(builder.Configuration.GetSection("AdsStoreDatabase"));
builder.Services.AddSingleton<AdsStoreService>();

var app = builder.Build();

app.MapGet("/", async() =>
{
    using var scope = app.Services.CreateScope();
    var adsStoreService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    var insertedPlayer = new ADSTestApp.Entities.Player { Name = "Test Player"};
    await adsStoreService.CreatePlayerAsync(insertedPlayer);
    var fetchedPlayer = await adsStoreService.GetPlayerByIdAsync(insertedPlayer.Id);
    var fetchAll = await adsStoreService.GetAllPlayersAsync();
    return Results.Ok(fetchAll);
});

app.MapGet("/GetShips", async()=>
{
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    // Get all existing ships
    var ships = await storeService.GetAllShipsAsync();
    
    // If none, create some.
    if (ships.Count == 0)
    {
        ships = Enumerable.Range(0, 10)
        .Select(i => new Ship
        {
            Name = $"Ship {i}",
            ShipType = Ship.ShipTypes[RandomNumberGenerator.GetInt32(Ship.ShipTypes.Count)],
            PositionX = RandomNumberGenerator.GetInt32(1000) - 500,
            PositionY = RandomNumberGenerator.GetInt32(1000) - 500,
            Rotation = RandomNumberGenerator.GetInt32(8) * 2 * (float)Math.PI / 8,
            Speed = 100
        })
        .ToList();
        var creations = ships
            .Select(ship => storeService.CreateShipAsync(ship));
        await Task.WhenAll(creations);        
    }
    return Results.Ok(ships);
});

app.Run();
