using ADSCommon.Entities;
using ADSCommon.Services;
using ADSCommon.Util;
using System.Diagnostics.CodeAnalysis;
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
        long previousUpdateTimeSlot;

        public async Task<SectorGameState> UpdateSector(AdsStoreService storeService, string workerName)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Calculate when was the last time the sector was updated (even if it was before it existed).
            // It is updated on a regular 'movementTimeMilliseconds' interval.
            previousUpdateTimeSlot = (now / movementTimeMilliseconds) * movementTimeMilliseconds;
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
                    NextUpdateTime = previousUpdateTimeSlot + movementTimeMilliseconds,
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
            // Check whether we managed to lock the sector for modification, and it's due for an update
            if (lockedSector is not null && lockedSector.NextUpdateTime < now)
            {
                try
                {
                    // If no ships in the sector, create some.
                    if (ships.Count == 0)
                    {
                        var shipCount = RandomNumberGenerator.GetInt32(5) + 2;
                        GenerateShips(shipCount);
                        paralellizableAwaits.Add(storeService.CreateShipsAsync(ships));
                    }
                    else
                    {
                        // If there exists some ships, update their positions.
                        foreach (var ship in ships)
                        {
                            UpdateShip(ship);

                            paralellizableAwaits.Add(storeService.UpdateShipAsync(ship.Id, ship));
                        }
                    }

                    // Remove all old attacks
                    await storeService.DeleteAllAttacksInSectorBeforeAsync(sector.Id, sector.LastUpdateTime);
                    // Determine if any ships are attacking
                    attacks = GenerateAttacks();
                    if (attacks.Count > 0)
                    {
                        paralellizableAwaits.Add(storeService.CreateAttacksAsync(attacks));
                    }

                    // Wait until all ships are created/updated and the sector is created (if needed).
                    await Task.WhenAll(paralellizableAwaits);
                    // Update the sector by releasing it for another worker and setting the next update time.
                    sector.UpdatedBy = "None";
                    sector.NextUpdateTime = previousUpdateTimeSlot + movementTimeMilliseconds;
                    sector.LastUpdateTime = previousUpdateTimeSlot;
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
                
        private List<Attack> GenerateAttacks()
        {
            if (sector is null)
            {
                throw new InvalidOperationException($"{nameof(sector)} is null");
            }
            return (
                from ship in ships
                let target = ClosestTargetInArc(ship)
                where target is not null
                select new Attack
                {
                    SectorId = sector.Id,
                    AttackerId = ship.Id,
                    DefenderId = target.Id,
                    StartTime = previousUpdateTimeSlot,
                    EndTime = previousUpdateTimeSlot + movementTimeMilliseconds,
                    Damage = 1,
                    Amount = RandomNumberGenerator.GetInt32(3) + 1,
                    Result = "Hit",
                    Weapon = "Red Laser"
                }
                ).ToList();
        }

        private void GenerateShips(int shipCount)
        {
            if (sector is null)
            {
                throw new InvalidOperationException($"{nameof(sector)} is null");
            }
            ships = (from i in Enumerable.Range(0, shipCount)
                     let type = Ship.ShipTypes[RandomNumberGenerator.GetInt32(Ship.ShipTypes.Count)]
                     let startPosition = new Vector2(RandomNumberGenerator.GetInt32(1000) - 500, RandomNumberGenerator.GetInt32(1000) - 500)
                     let startDirection = RandomNumberGenerator.GetInt32(4) * MathF.PI / 2
                     let targetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * MathF.PI / 4
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
                         MovementStartTime = previousUpdateTimeSlot,
                         MovementEndTime = previousUpdateTimeSlot + movementTimeMilliseconds
                     })
            .ToList();
        }

        private void UpdateShip(Ship ship)
        {
            ship.MovementStartTime = previousUpdateTimeSlot;
            ship.MovementEndTime = previousUpdateTimeSlot + movementTimeMilliseconds;
            ship.Speed = 200;
            ship.StartPosition = ship.EndPosition;
            ship.StartForward = ship.EndForward;
            var centerwards = (Vector2.Zero - ship.StartPosition).Normalized();
            // If the ship is too far away, turn around.
            if (ship.StartPosition.Length() > 1900)
            {
                // If the ship is not facing towards the center, turn to face the center.
                var signedAngle = ship.StartForward.AngleTo(centerwards);
                var isFacingCenterwards = MathF.Abs(signedAngle) <= MathF.PI / 4;
                if (!isFacingCenterwards)
                {
                    // The ship will turn 90 degrees to face the center of the sector.                     
                    // Left or right depending on what is the shortest turn towards (0, 0)                    
                    ship.TargetTurn = MathF.PI / 2 * MathF.Sign(signedAngle);
                }
                else
                {
                    ship.TargetTurn = 0;
                }
            }
            else
            {
                // Otherwise, choose a random direction
                ship.TargetTurn = (RandomNumberGenerator.GetInt32(5) - 2) * MathF.PI / 4;
            }
            ship.EndForward = ship.StartForward.Rotate(ship.TargetTurn);
            ship.EndPosition = CalculateEndPosition(ship.StartPosition, ship.StartForward, ship.EndForward, ship.TargetTurn, ship.Speed, movementTimeMilliseconds);
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
            var normalizedStartForward = startForward.Normalized();

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
                var translationVector = normalizedStartForward.PerpendicularClockwise() * radius;

                var originRotatedAroundRadius = Vector2.Zero.TranslateRotateTranslate(translationVector, turn);
                var finalPosition = startPosition + originRotatedAroundRadius;

                return finalPosition;
            }
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
