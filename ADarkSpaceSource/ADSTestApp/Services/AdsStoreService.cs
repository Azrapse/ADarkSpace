﻿using ADSTestApp.Data;
using ADSTestApp.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ADSTestApp.Services
{
    public class AdsStoreService
    {
        private readonly IMongoCollection<Player> _playersCollection;
        private readonly IMongoCollection<Ship> _shipsCollection;

        public AdsStoreService(IOptions<AdsStoreDatabaseSettings> adsStoreDatabaseSettings)
        {            
            var databaseHost = Environment.GetEnvironmentVariable("DATASTORE_HOST") ?? "localhost";
            var connectionString = adsStoreDatabaseSettings.Value.ConnectionString.Replace("localhost", databaseHost);
            var mongoClient = new MongoClient(connectionString);
            var mongoDatabase = mongoClient.GetDatabase(adsStoreDatabaseSettings.Value.DatabaseName);
            _playersCollection = mongoDatabase.GetCollection<Player>(adsStoreDatabaseSettings.Value.PlayersCollectionName);
            _shipsCollection = mongoDatabase.GetCollection<Ship>(adsStoreDatabaseSettings.Value.ShipsCollectionName);
        }

        public async Task<List<Player>> GetAllPlayersAsync() =>
            await _playersCollection.Find(_ => true).ToListAsync();

        public async Task<List<Ship>> GetAllShipsAsync() =>
            await _shipsCollection.Find(_ => true).ToListAsync();

        public async Task<Player?> GetPlayerByIdAsync(string id) =>
            await _playersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreatePlayerAsync(Player newPlayer) =>
            await _playersCollection.InsertOneAsync(newPlayer);

        public async Task CreateShipAsync(Ship newShip) =>
            await _shipsCollection.InsertOneAsync(newShip);

        public async Task UpdatePlayerAsync(string id, Player updatedPlayer) =>
            await _playersCollection.ReplaceOneAsync(x => x.Id == id, updatedPlayer);

        public async Task DeletePlayerAsync(string id) =>
            await _playersCollection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteAllPlayersAsync() =>
            await _playersCollection.DeleteManyAsync(x => true);

        public IQueryable<Player> Players =>_playersCollection.AsQueryable();

        public IQueryable<Ship> Ships  => _shipsCollection.AsQueryable();
    }
}