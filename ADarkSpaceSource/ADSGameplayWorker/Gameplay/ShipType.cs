namespace ADSGameplayWorker.Gameplay
{
    public class ShipType
    {
        public string Name { get; set; } = "None";
        public float Speed { get; set; }

        public float MaxHull { get; set; }
        public float MaxShield { get; set; }

        public float ShieldRegen { get; set; }
        public float HullRegen { get; set; }

        public float Attack { get; set; }
        public float Agility { get; set; }

        public List<string> Actions { get; set; } = new();
        public List<string> Moves { get; set; } = new();

        public int Score => (int)(MaxHull + MaxShield + Attack * 1.5f + Agility + Actions.Count);
        public static List<ShipType> ShipTypes { get; } = new ()
        { 
            new ShipType
            {
                Name = "X-Wing",
                Speed = 100,
                MaxHull = 4,
                MaxShield = 2,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 3,
                Agility = 2,
                Actions = new() { "Focus", "Target Lock", "Barrel Roll" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "A-Wing",
                Speed = 120,
                MaxHull = 2,
                MaxShield = 2,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 2,
                Agility = 3,
                Actions = new() { "Focus", "Target Lock", "Barrel Roll", "Boost", "Evade" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "Y-Wing",
                Speed = 80,
                MaxHull = 6,
                MaxShield = 2,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 2,
                Agility = 1,
                Actions = new() { "Focus", "Target Lock", "Red Barrel Roll", "Red Reload" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "B-Wing",
                Speed = 90,
                MaxHull = 4,
                MaxShield = 4,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 3,
                Agility = 1,
                Actions = new() { "Focus", "Target Lock", "Barrel Roll", "Reload" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "TIE Fighter",
                Speed = 100,
                MaxHull = 3,
                MaxShield = 0,
                ShieldRegen = 0,
                HullRegen = 0,
                Attack = 2,
                Agility = 3,
                Actions = new() { "Focus", "Evade", "Barrel Roll" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "TIE Interceptor",
                Speed = 115,
                MaxHull = 3,
                MaxShield = 0,
                ShieldRegen = 0,
                HullRegen = 0,
                Attack = 3,
                Agility = 3,
                Actions = new() { "Focus", "Barrel Roll", "Boost" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "TIE Bomber",
                Speed = 80,
                MaxHull = 6,
                MaxShield = 0,
                ShieldRegen = 0,
                HullRegen = 0,
                Attack = 2,
                Agility = 2,
                Actions = new() { "Focus", "Target Lock", "Barrel Roll", "Red Reload" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "TIE Advanced",
                Speed = 120,
                MaxHull = 3,
                MaxShield = 2,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 2,
                Agility = 3,
                Actions = new() { "Focus", "Target Lock", "Barrel Roll" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "Shuttle",
                Speed = 45,
                MaxHull = 6,
                MaxShield = 4,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 3,
                Agility = 1,
                Actions = new() { "Focus", "Reinforce", "Coordinate", "Red Jam" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },
            new ShipType
            {
                Name = "Starwing",
                Speed = 80,
                MaxHull = 4,
                MaxShield = 3,
                ShieldRegen = 1,
                HullRegen = 0,
                Attack = 2,
                Agility = 2,
                Actions = new() { "Focus", "Target Lock", "Reload", "SLAM" },
                Moves = new() { "1 Straight", "2 Straight", "3 Straight", "4 Straight", "5 Straight", "1 Bank Left", "1 Bank Right", "2 Bank Left", "2 Bank Right", "1 Turn Left", "1 Turn Right", "2 Turn Left", "2 Turn Right", "3 Turn Left", "3 Turn Right", "4 Turn Left", "4 Turn Right", "5 Turn Left", "5 Turn Right", "K-Turn", "S-Loop Left", "S-Loop Right", "T-Roll Left", "T-Roll Right", "Stop" }
            },  
        };
    }
    /*
    { name: "X-Wing", img: 'img/x-wing.png', imgdir: Math.PI, speed: 100 },
    { name: "A-Wing", img: 'img/a-wing.png', imgdir: Math.PI, speed: 120 },
    { name: "Y-Wing", img: 'img/y-wing.png', imgdir: Math.PI, speed: 80 },
    { name: "B-Wing", img: 'img/b-wing.png', imgdir: Math.PI, speed: 90 },
    { name: "TIE Fighter", img: 'img/t-f.png', imgdir: Math.PI, speed: 100 },
    { name: "TIE Interceptor", img: 'img/t-i.png', imgdir: Math.PI, speed: 115 },
    { name: "TIE Bomber", img: 'img/t-b.png', imgdir: Math.PI, speed: 180 },
    { name: "TIE Advanced", img: 'img/t-a.png', imgdir: Math.PI, speed: 120 },
    { name: "TIE Defender", img: 'img/t-d.png', imgdir: Math.PI, speed: 120 },
    { name: "Shuttle", img: 'img/shuttle.png', imgdir: Math.PI, speed: 60 },
    { name: "Starwing", img: 'img/starwing.png', imgdir: Math.PI, speed: 90 },
     * */
}
