using ADSCommon.Data;
using ADSCommon.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ADSCommon.Services
{
    public class AdsStoreService
    {
        private readonly IMongoCollection<Player> _playersCollection;
        private readonly IMongoCollection<Ship> _shipsCollection;
        private readonly IMongoCollection<Sector> _sectorsCollection;
        private readonly IMongoCollection<Attack> _attacksCollection;

        public AdsStoreService(IOptions<AdsStoreDatabaseSettings> adsStoreDatabaseSettings)
        {            
            var databaseHost = Environment.GetEnvironmentVariable("DATASTORE_HOST") ?? "localhost";
            var connectionString = adsStoreDatabaseSettings.Value.ConnectionString.Replace("localhost", databaseHost);
            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(adsStoreDatabaseSettings.Value.DatabaseName);
            _playersCollection = mongoDatabase.GetCollection<Player>(adsStoreDatabaseSettings.Value.PlayersCollectionName);
            _shipsCollection = mongoDatabase.GetCollection<Ship>(adsStoreDatabaseSettings.Value.ShipsCollectionName);
            _sectorsCollection = mongoDatabase.GetCollection<Sector>(adsStoreDatabaseSettings.Value.SectorsCollectionName);
            _attacksCollection = mongoDatabase.GetCollection<Attack>(adsStoreDatabaseSettings.Value.AttacksCollectionName);
        }

        #region Players
        public async Task<List<Player>> GetAllPlayersAsync() =>
            await _playersCollection.Find(_ => true).ToListAsync();

        public async Task<Player?> GetPlayerByIdAsync(string id) =>
            await _playersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreatePlayerAsync(Player newPlayer) =>
            await _playersCollection.InsertOneAsync(newPlayer);

        public async Task UpdatePlayerAsync(string id, Player updatedPlayer) =>
            await _playersCollection.ReplaceOneAsync(x => x.Id == id, updatedPlayer);

        public async Task DeletePlayerAsync(string id) =>
            await _playersCollection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteAllPlayersAsync() =>
            await _playersCollection.DeleteManyAsync(x => true);

        #endregion

        #region Sectors
        public async Task<List<Sector>> GetAllSectorsAsync() =>
            await _sectorsCollection.Find(_ => true).ToListAsync();
        public async Task CreateSectorAsync(Sector newSector) =>
            await _sectorsCollection.InsertOneAsync(newSector);
        public async Task UpdateSectorAsync(string id, Sector updatedSector) =>
            await _sectorsCollection.ReplaceOneAsync(x => x.Id == id, updatedSector);
        public async Task<Sector?> GetSectorByIdAsync(string id) =>
            await _sectorsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        // These two methods help locking a sector so that only one gameplay worker modifies a sector at a time.
        public async Task<Sector?> LockSectorForModificationAsync(string sectorId, string workerName, long forceTakeoverIfUnmodifiedSince) =>
            await _sectorsCollection.FindOneAndUpdateAsync(x => x.Id == sectorId && (x.UpdatedBy == "None" || x.UpdatedBy == workerName || x.LastUpdateTime < forceTakeoverIfUnmodifiedSince),
                Builders<Sector>.Update.Set(x => x.UpdatedBy, workerName).Set(x => x.LastUpdateTime, forceTakeoverIfUnmodifiedSince + 1));
        public async Task<Sector?> UnlockSectorForModificationAsync(string sectorId, string workerName) =>
            await _sectorsCollection.FindOneAndUpdateAsync(x => x.Id == sectorId && x.UpdatedBy == workerName,
                Builders<Sector>.Update.Set(x => x.UpdatedBy, "None"));

        #endregion

        #region Ships

        public async Task<List<Ship>> GetAllShipsInSectorAsync(string sectorId) =>
            await _shipsCollection.Find(ship => ship.SectorId == sectorId).ToListAsync();

        public async Task CreateShipAsync(Ship newShip) =>
            await _shipsCollection.InsertOneAsync(newShip);

        public async Task UpdateShipAsync(string id, Ship updatedShip) =>
            await _shipsCollection.ReplaceOneAsync(x => x.Id == id, updatedShip);

        public async Task CreateShipsAsync(IEnumerable<Ship> newShips) =>
            await _shipsCollection.InsertManyAsync(newShips);

        public async Task DeleteAllShipsAsync() =>
            await _shipsCollection.DeleteManyAsync(x => true);

        #endregion

        #region Attacks

        public async Task CreateAttacksAsync(IEnumerable<Attack> newAttacks) =>
            await _attacksCollection.InsertManyAsync(newAttacks);
        public async Task<List<Attack>> GetAllAttacksInSectorAsync(string sectorId) =>
            await _attacksCollection.Find(attack => attack.SectorId == sectorId).ToListAsync();

        public async Task<List<Attack>> GetAllAttacksInSectorBetweenAsync(string sectorId, long startTime, long endTime) =>
            await _attacksCollection.Find(attack => attack.SectorId == sectorId && attack.StartTime >= startTime && attack.EndTime <= endTime).ToListAsync();

        public async Task DeleteAllAttacksInSectorBeforeAsync(string sectorId, long time) =>
            await _attacksCollection.DeleteManyAsync(x => x.SectorId == sectorId && x.EndTime < time);

        #endregion


        public IQueryable<Player> Players =>_playersCollection.AsQueryable();

        public IQueryable<Ship> Ships  => _shipsCollection.AsQueryable();

        public IQueryable<Sector> Sectors => _sectorsCollection.AsQueryable();

        public IQueryable<Attack> Attacks => _attacksCollection.AsQueryable();
    }
}
