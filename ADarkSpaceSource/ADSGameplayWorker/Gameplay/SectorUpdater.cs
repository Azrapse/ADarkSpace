using ADSCommon.Entities;
using ADSCommon.Services;
using System.Numerics;
using System.Security.Cryptography;

namespace ADSGameplayWorker.Gameplay
{
    public class SectorUpdater
    {
        const int movementTimeMilliseconds = 2000;
        const int attackTimeMilliseconds = 1000;
        const int attackRangeSectionLenght = 300;

        Sector? sector;
        List<Ship>? ships;
        List<Attack>? attacks;

        public async Task<SectorGameState> UpdateSector(AdsStoreService storeService, string workerName)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var paralellizableAwaits = new List<Task>();

            // Get the one sector, or create it.
            sector = (await storeService.GetAllSectorsAsync())
                ?.FirstOrDefault();
            if (sector is null)
            {
                sector = new Sector
                {
                    Name = "Test sector",
                    LastUpdateTime = now,
                    NextUpdateTime = now + movementTimeMilliseconds,
                    UpdatedBy = "None",
                    Time = now
                };
                paralellizableAwaits.Add(storeService.CreateSectorAsync(sector));
            }

            // Either way, update the sector time before returning it to the client.
            sector.Time = now;
            // Get all existing ships
            ships  = await storeService.GetAllShipsInSectorAsync(sector.Id);

            // Try to get exclusive access to the sector.
            // If the sector wasn't updated in the last 4 seconds, force a takeover.
            var forceTakeOverIfLastModifiedBefore = now - 4000;
            var lockedSector = await storeService.LockSectorForModificationAsync(sector.Id, workerName, forceTakeOverIfLastModifiedBefore);
            // Check we managed to lock the sector for modification, and it's due for an update
            if (lockedSector is not null && lockedSector.NextUpdateTime < now)
            {
                try
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
                                 let endPosition = CalculateEndPosition(startPosition, startForward, endForward, targetTurn, speed, movementTimeMilliseconds)
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
                                     MovementEndTime = now + movementTimeMilliseconds
                                 })
                        .ToList();
                        paralellizableAwaits.Add(storeService.CreateShipsAsync(ships));
                    }
                    else
                    {
                        foreach (var ship in ships)
                        {
                            ship.MovementStartTime = now;
                            ship.MovementEndTime = now + movementTimeMilliseconds;
                            ship.Speed = 200;
                            ship.StartPosition = ship.EndPosition;
                            if (ship.StartPosition.Length() > 2000)
                            {
                                ship.StartPosition = Vector2.Zero;
                            }
                            ship.StartForward = ship.EndForward;
                            ship.TargetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * (float)Math.PI / 4;
                            ship.EndForward = RotateVector2(ship.StartForward, ship.TargetTurn);
                            ship.EndPosition = CalculateEndPosition(ship.StartPosition, ship.StartForward, ship.EndForward, ship.TargetTurn, ship.Speed, movementTimeMilliseconds);

                            paralellizableAwaits.Add(storeService.UpdateShipAsync(ship.Id, ship));
                        }
                    }

                    // Remove all old attacks
                    await storeService.DeleteAllAttacksInSectorBeforeAsync(sector.Id, sector.LastUpdateTime);
                    // Determine if any ships are attacking
                    attacks = (
                        from ship in ships
                        let target = ClosestTargetInArc(ship)
                        where target is not null
                        select new Attack
                        {
                            SectorId = sector.Id,
                            AttackerId = ship.Id,
                            DefenderId = target.Id,
                            StartTime = now,
                            EndTime = now + movementTimeMilliseconds,
                            Damage = 1,
                            Amount = RandomNumberGenerator.GetInt32(3) + 1,
                            Result = "Hit",
                            Weapon = "Red Laser"
                        }
                        ).ToList();
                    if (attacks.Count > 0)
                    {
                        paralellizableAwaits.Add(storeService.CreateAttacksAsync(attacks));
                    }

                    // Wait until all ships are created/updated and the sector is created (if needed).
                    await Task.WhenAll(paralellizableAwaits);
                    // Update the sector by releasing it for another worker and setting the next update time.
                    sector.UpdatedBy = "None";
                    sector.NextUpdateTime = now + movementTimeMilliseconds;
                    sector.LastUpdateTime = now;
                    await storeService.UpdateSectorAsync(sector.Id, sector);
                }
                finally
                {
                    // Release the sector for modification.
                    await storeService.UnlockSectorForModificationAsync(sector.Id, workerName);
                }
            }
            else
            {
                attacks = await storeService.GetAllAttacksInSectorBetweenAsync(sector.Id, sector.LastUpdateTime, sector.NextUpdateTime);
            }

            // Return the sector and ships.
            var result = new SectorGameState
            {
                Sector = sector,
                Ships = ships,
                Attacks = attacks
            };
            return result;
        }

        Ship? ClosestTargetInArc(Ship attacker)
        {
            const int maxAttackRange = attackRangeSectionLenght * 3;
            const int maxAttackRangeSquared = maxAttackRange * maxAttackRange;

            if (ships is null)
            {
                return null;
            }
            var closestTargetDistance = float.MaxValue;
            Ship? closestTarget = null;
            for (var i = 0; i < ships.Count; i++)
            {
                var target = ships[i];
                if (target == attacker)
                {
                    continue;
                }
                // Check if it is in arc.
                var vectorToTarget = target.EndPosition - attacker.EndPosition;
                // It is in arc if the angle between the vector to target and the forward of this ship is less than 45 degrees (or PI/4)
                // Or if the Dot product of the vectors is larger than the cosinus of 45 degrees.
                var isInArc = Vector2.Dot(attacker.EndForward, Vector2.Normalize(vectorToTarget)) > MathF.Cos(MathF.PI / 4);
                if (!isInArc) 
                { 
                    continue;
                }
                // Check if it is closer than the previous closest, and within range of the attacker's weapons.
                var distanceToTarget = vectorToTarget.LengthSquared();
                if (distanceToTarget < closestTargetDistance && distanceToTarget < maxAttackRangeSquared)
                {
                    closestTargetDistance = distanceToTarget;
                    closestTarget = target;
                }
            }
            return closestTarget;
        }

        static Vector2 CalculateEndPosition(Vector2 startPosition, Vector2 startForward, Vector2 endForward, float turn, float speed, int movementTime)
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

        static Vector2 PerpendicularClockwise(Vector2 v)
        {
            return new Vector2(v.Y, -v.X);
        }

        static Vector2 RotateVector2(Vector2 vector, float angleInRadians)
        {
            var cos = MathF.Cos(angleInRadians);
            var sin = MathF.Sin(angleInRadians);
            var x = vector.X * cos - vector.Y * sin;
            var y = vector.X * sin + vector.Y * cos;
            return new Vector2(x, y);
        }

        static Vector2 TranslateRotateTranslate(Vector2 vector, Vector2 translation, float angle)
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
        /// <summary>
        /// Describes the sector, including the current time
        /// </summary>
        public Sector Sector { get; set; } = null!;
        /// <summary>
        /// Lists all active ships in the current sector.
        /// </summary>
        public List<Ship> Ships { get; set; } = null!;
        /// <summary>
        /// Lists all current attacks in the current sector.
        /// </summary>
        public List<Attack> Attacks { get; set; } = null!;
    }
}
