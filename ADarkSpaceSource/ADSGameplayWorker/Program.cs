using ADSCommon.Data;
using ADSGameplayWorker.Gameplay;
using ADSCommon.Services;
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
    var insertedPlayer = new ADSCommon.Entities.Player { Name = "Test Player"};
    await adsStoreService.CreatePlayerAsync(insertedPlayer);
    var fetchedPlayer = await adsStoreService.GetPlayerByIdAsync(insertedPlayer.Id);
    var fetchAll = await adsStoreService.GetAllPlayersAsync();
    return Results.Ok(fetchAll);
});

app.MapGet("/ResetSector", async () =>
{
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    await storeService.DeleteAllShipsAsync();
    return Results.Ok("All ships deleted.");
});

app.MapGet("/GetGameState", async()=>
{
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    var sectorUpdater = new SectorUpdater();    
    var result = await sectorUpdater.UpdateSector(storeService, workerName);
    return Results.Ok(result);
});

app.Run();
