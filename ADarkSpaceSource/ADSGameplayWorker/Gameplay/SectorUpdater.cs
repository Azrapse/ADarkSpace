using ADSCommon.Entities;
using ADSCommon.Services;
using System.Numerics;
using System.Security.Cryptography;

namespace ADSGameplayWorker.Gameplay
{
    public class SectorUpdater
    {
        const int movementTime = 2000;

        public async Task<SectorGameState> UpdateSector(AdsStoreService storeService, string workerName)
        {
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
                Console.WriteLine(ex.Message);
                await storeService.DeleteAllShipsAsync();
            }

            // Check if the sector is free and due for an update
            if ((sector.UpdatedBy == "None" || sector.UpdatedBy == workerName) && sector.NextUpdateTime < now)
            {
                // If no ships in the sector, create some.
                if (ships.Count == 0)
                {
                    var shipCount = RandomNumberGenerator.GetInt32(5) + 2;
                    ships = (from i in Enumerable.Range(0, shipCount)
                             let type = Ship.ShipTypes[RandomNumberGenerator.GetInt32(Ship.ShipTypes.Count)]
                             let startPosition = new Vector2(RandomNumberGenerator.GetInt32(1000) - 500, RandomNumberGenerator.GetInt32(1000) - 500)
                             let startDirection = RandomNumberGenerator.GetInt32(4) * (float)Math.PI / 2
                             let targetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * (float)Math.PI / 4
                             let endDirection = startDirection + targetTurn
                             let startForward = new Vector2(MathF.Cos(startDirection), MathF.Sin(startDirection))
                             let endForward = new Vector2(MathF.Cos(endDirection), MathF.Sin(endDirection))
                             let speed = 100
                             let endPosition = CalculateEndPosition(startPosition, startForward, endForward, targetTurn, speed, movementTime)
                             select new Ship
                             {
                                 Name = $"{type} #{i}",
                                 SectorId = sector.Id,
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
                    foreach (var ship in ships)
                    {
                        ship.MovementStartTime = now;
                        ship.MovementEndTime = now + movementTime;
                        ship.Speed = 200;
                        ship.StartPosition = ship.EndPosition;
                        if (ship.StartPosition.Length() > 2000)
                        {
                            ship.StartPosition = Vector2.Zero;
                        }
                        ship.StartForward = ship.EndForward;
                        ship.TargetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * (float)Math.PI / 4;
                        ship.EndForward = RotateVector2(ship.StartForward, ship.TargetTurn);
                        ship.EndPosition = CalculateEndPosition(ship.StartPosition, ship.StartForward, ship.EndForward, ship.TargetTurn, ship.Speed, movementTime);

                        paralellizableAwaits.Add(storeService.UpdateShipAsync(ship.Id, ship));
                    }
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
            var result = new SectorGameState
            {
                Sector = sector,
                Ships = ships
            };
            return result;
        }

        Vector2 CalculateEndPosition(Vector2 startPosition, Vector2 startForward, Vector2 endForward, float turn, float speed, int movementTime)
        {
            // Normalize the forward and targetForward vectors
            var normalizedStartForward = Vector2.Normalize(startForward);

            // If there is no direction change, then the forward direction is constant, and the position is linear.
            if (MathF.Abs(turn) < 0.00001)
            {
                return startPosition + normalizedStartForward * speed * movementTime / 1000;
            }
            else
            {
                // If there is direction change, then the forward direction is interpolated, and the position is circular.
                // Calculate the interpolated position
                var radius = speed * movementTime / 1000 / turn;

                // Translate the vectorToMove a distance of radius, depending on whether we are turning right or left.        
                var translationVector = PerpendicularClockwise(normalizedStartForward) * radius;

                var originRotatedAroundRadius = TranslateRotateTranslate(Vector2.Zero, translationVector, turn);
                var finalPosition = startPosition + originRotatedAroundRadius;

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
    }

    public class SectorGameState
    {
        public Sector Sector { get; set; } = null!;
        public List<Ship> Ships { get; set; } = null!;
    }
}
