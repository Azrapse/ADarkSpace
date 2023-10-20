using ADSTestApp.Data;
using ADSTestApp.Entities;
using ADSTestApp.Services;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.Configure<AdsStoreDatabaseSettings>(builder.Configuration.GetSection("AdsStoreDatabase"));
builder.Services.AddSingleton<AdsStoreService>();

var app = builder.Build();
var workerName = $"Worker {RandomNumberGenerator.GetInt32(1000)}";

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

app.MapGet("/GetGameState", async()=>
{
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    var paralellizableAwaits = new List<Task>();

    // Get the one sector, or create it.
    var sector = (await storeService.GetAllSectorsAsync())
        ?.FirstOrDefault();
    if (sector is null)
    {
        sector = new Sector
        {
            Name = "Test sector", 
            LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            NextUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 2,
            UpdatedBy = workerName
        };
        paralellizableAwaits.Add(storeService.CreateSectorAsync(sector));
    }

    // Get all existing ships
    var ships = await storeService.GetAllShipsAsync();

    // Check if the sector is free and due for an update
    if ((sector.UpdatedBy == "None" || sector.UpdatedBy == workerName) && sector.NextUpdateTime < now)
    {
        // If no ships in the sector, create some.
        if (ships.Count == 0)
        {
            var shipCount = RandomNumberGenerator.GetInt32(3) + 2;
            ships = Enumerable.Range(0, shipCount)
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
            paralellizableAwaits.AddRange(ships
                .Select(ship => storeService.CreateShipAsync(ship))
            );
        }
        // Wait until all ships are created and the sector is created.
        await Task.WhenAll(paralellizableAwaits);
        // Update the sector by releasing it for another worker and setting the next update time.
        sector.UpdatedBy = "None";
        sector.NextUpdateTime = now + 2;
        sector.LastUpdateTime = now;
        await storeService.UpdateSectorAsync(sector.Id, sector);
    }
    // Return the sector and ships.
    var result = new
    {
        sector,
        ships
    };
    return Results.Ok(result);
});

app.Run();
