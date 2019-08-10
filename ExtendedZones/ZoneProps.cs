namespace ExtendedZones
{
    using System;
    public class ZoneProps
    {
        public bool enabled;
        public string join_message;
        public string leave_message;
        public string name;
        public string location;
        public string creation_time;
        public float radius;
        public bool radiation;
        public float radiationAmount;
        public double radiationInterval;
        public bool nosuicide;
        public bool godmode;
        public bool blockBuilding;
        public string[] blockedCommands;

        public ZoneProps(string name, string location, float radius, bool radiation, float radiationAmount, bool nosuicide, bool godmode, bool blockBuilding, string[] blockedCommands, DateTime time)
        {
            this.enabled = true;
            this.join_message = "You've entered the zone %zone%";
            this.leave_message = "You've left the zone %zone%";
            this.name = name;
            this.location = location;
            this.radius = radius;
            this.radiation = radiation;
            this.radiationAmount = radiationAmount;
            this.radiationInterval = 5000.0; // 5 seconds
            this.nosuicide = nosuicide;
            this.godmode = godmode;
            this.blockBuilding = blockBuilding;
            this.blockedCommands = blockedCommands;
            this.creation_time = time.ToString();
        }
    }
}