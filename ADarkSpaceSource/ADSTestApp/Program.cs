using ADSTestApp.Data;
using ADSTestApp.Entities;
using ADSTestApp.Services;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Transactions;

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

app.MapGet("/ResetSector", async () =>
{
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    await storeService.DeleteAllShipsAsync();
    return Results.Ok("All ships deleted.");
});

app.MapGet("/GetGameState", async()=>
{
    const int movementTime = 2000;
    using var scope = app.Services.CreateScope();
    var storeService = scope.ServiceProvider.GetRequiredService<AdsStoreService>();
    
    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var paralellizableAwaits = new List<Task>();

    // Get the one sector, or create it.
    var sector = (await storeService.GetAllSectorsAsync())
        ?.FirstOrDefault();
    if (sector is null)
    {
        sector = new Sector
        {
            Name = "Test sector", 
            LastUpdateTime = now,
            NextUpdateTime = now + movementTime,
            UpdatedBy = workerName,
            Time = now
        };
        paralellizableAwaits.Add(storeService.CreateSectorAsync(sector));
    }
    // Either way, update the sector time before returning it to the client.
    sector.Time = now;

    // Get all existing ships
    var ships = new List<Ship>();
    try
    {
        ships = await storeService.GetAllShipsAsync();
    }
    catch (Exception ex)
    {
        await storeService.DeleteAllShipsAsync();
    }

    // Check if the sector is free and due for an update
    if ((sector.UpdatedBy == "None" || sector.UpdatedBy == workerName) && sector.NextUpdateTime < now)
    {
        // If no ships in the sector, create some.
        if (ships.Count == 0)
        {
            var shipCount = RandomNumberGenerator.GetInt32(3) + 2;
            ships = (from i in Enumerable.Range(0, shipCount)
                     let type = Ship.ShipTypes[RandomNumberGenerator.GetInt32(Ship.ShipTypes.Count)]
                     let startPosition = new Vector2(RandomNumberGenerator.GetInt32(1000) - 500, RandomNumberGenerator.GetInt32(1000) - 500)
                     let startDirection = RandomNumberGenerator.GetInt32(4) * (float)Math.PI / 2
                     let targetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * (float)Math.PI / 4
                     let endDirection = startDirection + targetTurn
                     let startForward = new Vector2(MathF.Cos(startDirection), -MathF.Sin(startDirection))
                     let endForward = new Vector2(MathF.Cos(endDirection), -MathF.Sin(endDirection))
                     let speed = 100
                     let endPosition = CalculateEndPosition(startPosition, startForward, endForward, speed, movementTime)
                     select new Ship
                     {
                         Name = $"{type} #{i}",
                         ShipType = type,
                         StartPositionX = startPosition.X,
                         StartPositionY = startPosition.Y,
                         StartForwardX = startForward.X,
                         StartForwardY = startForward.Y,
                         EndPositionX = endPosition.X,
                         EndPositionY = endPosition.Y,
                         EndForwardX = endForward.X,
                         EndForwardY = endForward.Y,
                         TargetTurn = targetTurn,
                         Speed = speed,
                         MovementStartTime = now,
                         MovementEndTime = now + movementTime
                     })
            .ToList();
            paralellizableAwaits.AddRange(ships
                .Select(ship => storeService.CreateShipAsync(ship))
            );
        }
        else
        {
            foreach(var ship in ships)
            {
                ship.MovementStartTime = now;
                ship.MovementEndTime = now + movementTime;
                ship.Speed = 200;
                ship.StartPosition = ship.EndPosition;
                if(ship.StartPosition.Length() > 2000)
                {
                    ship.StartPosition = Vector2.Zero;
                }
                ship.StartForward = ship.EndForward;                
                ship.TargetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * (float)Math.PI / 4;
                ship.EndForward = RotateVector2(ship.StartForward, ship.TargetTurn);                                
                ship.EndPosition = CalculateEndPosition(ship.StartPosition, ship.StartForward, ship.EndForward, ship.Speed, movementTime);

                paralellizableAwaits.Add(storeService.UpdateShipAsync(ship.Id, ship));
            }
            var s = ships[0];
            var degrees = MathF.Abs(s.TargetTurn * 180 / (float)Math.PI);
            var side = s.TargetTurn < 0 ? "left" : (s.TargetTurn > 0 ? "right" : "straight");
            Console.WriteLine($"{s.Name} will go {side} {degrees} degrees.");
        }
        // Wait until all ships are created/updated and the sector is created (if needed).
        await Task.WhenAll(paralellizableAwaits);
        // Update the sector by releasing it for another worker and setting the next update time.
        sector.UpdatedBy = "None";
        sector.NextUpdateTime = now + movementTime;
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

Vector2 CalculateEndPosition(Vector2 startPosition, Vector2 startForward, Vector2 endForward, float speed, int movementTime)
{
    // Normalize the forward and targetForward vectors
    var normalizedStartForward = Vector2.Normalize(startForward);
    var normalizedTargetForward = Vector2.Normalize(endForward);

    // Calculate the angle between the initial and final directions
    float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
    var directionChange = MathF.Acos(Vector2.Dot(normalizedStartForward, normalizedTargetForward)) * MathF.Sign(Cross(normalizedStartForward, normalizedTargetForward));

    // If there is no direction change, then the forward direction is constant, and the position is linear.
    if (MathF.Abs(directionChange) < 0.00001)
    {
        return startPosition + normalizedStartForward * speed * movementTime / 1000;
    }
    else
    {
        // If there is direction change, then the forward direction is interpolated, and the position is circular.
        // Calculate the interpolated position
        var radius = speed / directionChange;

        // Translate the vectorToMove a distance of radius, depending on whether we are turning right or left.        
        var translationVector = PerpendicularClockwise(normalizedStartForward);
        translationVector *= radius;        

        var forwardRotatedAroundRadius = TranslateRotateTranslate(normalizedStartForward, translationVector, -directionChange);
        var finalPosition = startPosition + forwardRotatedAroundRadius;

        return finalPosition;
    }
}

Vector2 PerpendicularClockwise(Vector2 v)
{
    return new Vector2(v.Y, -v.X);
}

Vector2 RotateVector2(Vector2 vector, float angleInRadians)
{
    var cos = MathF.Cos(angleInRadians);
    var sin = MathF.Sin(angleInRadians);
    var x = vector.X * cos - vector.Y * sin;
    var y = vector.X * sin + vector.Y * cos;
    return new Vector2(x, y);
}

Vector2 TranslateRotateTranslate(Vector2 vector, Vector2 translation, float angle) 
{
    // Translate the vector
    var translatedVector = vector + translation;
    // Rotate the translated vector
    var rotatedVector = RotateVector2(translatedVector, angle);
    // Translate the rotated vector back
    var finalVector = rotatedVector - translation;
    return finalVector;
}


app.Run();
