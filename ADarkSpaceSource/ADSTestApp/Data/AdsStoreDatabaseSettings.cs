namespace ADSTestApp.Data
{
    public class AdsStoreDatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string PlayersCollectionName { get; set; } = null!;

        public string ShipsCollectionName { get; set; } = null!;

        public string SectorsCollectionName { get; set; } = null!;

    }
}
