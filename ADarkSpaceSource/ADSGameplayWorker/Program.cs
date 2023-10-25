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

app.MapGet("/ResetSector", async (AdsStoreService storeService) =>
{
    await storeService.DeleteAllShipsAsync();
    return Results.Ok("All ships deleted.");
});

app.MapGet("/GetGameState", async (AdsStoreService storeService) =>
{
    var sectorUpdater = new SectorUpdater();
    var result = await sectorUpdater.UpdateSector(storeService, workerName);
    return Results.Ok(result);
});

app.Run();
