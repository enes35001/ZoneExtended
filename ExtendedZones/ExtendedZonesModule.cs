using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using Fougerite;
using Fougerite.Events;
using UnityEngine;

namespace ExtendedZones
{
    public class ExtendedZonesModule : Fougerite.Module
    {
        public override string Name => "ExtendedZones";
        public override string Description => "ExtendedZones plugin with many features and options";
        public override string Author => "ice cold";
        public override Version Version => new Version("1.0");

        public Dictionary<string, ZoneProps> Zones;
        public Dictionary<Player, Timer> PlayerListener;
        public Dictionary<Player, ZoneProps> PlayerZone;
        public Dictionary<Player, Timer> RadiationListener;
        private const string chat_Name = "Zones";

        private const string color_red = "[color red]";


        Util util = Util.GetUtil();
        public override void DeInitialize()
        {
            JsonHelper.SaveFile(Zones, GetAbsoluteFilePath("db_Zones.json"));
            Hooks.OnCommand -= OnCommand;
            Hooks.OnPlayerHurt -= OnPlayerHurt;
            Hooks.OnEntityDeployedWithPlacer -= OnEntityDeployed;
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
            Hooks.OnServerSaved -= OnServerSave;
        }

        public override void Initialize()
        {
            Zones = new Dictionary<string, ZoneProps>();
            PlayerListener = new Dictionary<Player, Timer>();
            PlayerZone = new Dictionary<Player, ZoneProps>();
            RadiationListener = new Dictionary<Player, Timer>();
            Zones = ReadyConfigChecked(Zones, "db_Zones.json");
            Hooks.OnCommand += OnCommand;
            Hooks.OnPlayerHurt += OnPlayerHurt;
            Hooks.OnEntityDeployedWithPlacer += OnEntityDeployed;
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
            Hooks.OnServerSaved += OnServerSave;


        }

        private void OnServerSave(int Amount, double Seconds)
        {
            JsonHelper.SaveFile(Zones, GetAbsoluteFilePath("db_Zones.json"));
        }

        private void OnPlayerDisconnected(Player player)
        {
            if(PlayerListener.ContainsKey(player))
            {
                PlayerListener[player].Dispose();
                PlayerListener.Remove(player);
                RadiationListener[player].Dispose();
                RadiationListener.Remove(player);
                PlayerZone.Remove(player);

            }
        }

        private void OnPlayerConnected(Player player)
        {
            if(player.IsOnline)
            {
                var timer = PlayerListener[player] = new Timer();
                timer.Interval = 500;
                timer.AutoReset = true;
                timer.Enabled = true;
                timer.Elapsed += (x, y) => CheckPlayerZone(player);
            }
        }

      

        private void OnPlayerHurt(HurtEvent he)
        {
            if(he.AttackerIsPlayer && he.VictimIsPlayer)
            {
                Player victim = (Player)he.Victim;
                Player attacker = (Player)he.Attacker;
                var nearbyzone = GetZoneAtLocation(victim.Location);
                if(nearbyzone != null)
                {
                    if (nearbyzone.godmode || nearbyzone.nosuicide)
                    {
                        string name = nearbyzone.name == string.Empty ? "Unknown" : nearbyzone.name;
                        float radius = nearbyzone.radius;
                        float dist = Vector3.Distance(victim.Location, util.ConvertStringToVector3(nearbyzone.location));
                        if (dist < radius)
                        {
                            if (victim == attacker && nearbyzone.nosuicide) { victim.MessageFrom(chat_Name, "You are not allowed to suicide in zone " + name); he.DamageAmount = 0.0f; return; }
                            he.DamageAmount = 0.0f;
                            attacker.MessageFrom(chat_Name, "Player godmode is enabled in the zone " + name);
                        }
                    }
             
                }
               
                
            }
        }        

        private void OnEntityDeployed(Player player, Entity e, Player actualplacer)
        {
            if(e != null && !e.IsDestroyed)
            {
                ZoneProps nearbyzone = GetZoneAtLocation(actualplacer.Location) ?? null;
                if (nearbyzone != null)
                {
                    if (nearbyzone.blockBuilding)
                    {
                        float radius = nearbyzone.radius;
                        float dist = Vector3.Distance(actualplacer.Location, util.ConvertStringToVector3(nearbyzone.location));
                        if (dist < radius)
                        {
                            e.Destroy();
                            player.MessageFrom(chat_Name, color_red + $"You are not allowed to build inside zone '{nearbyzone.name}'");
                        }
                    }
                }
            
             

            }
        }

       

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if(cmd == "zone")
            {
                if (!player.Admin) return;
                if(args.Length == 0)
                {
                    player.Notice("☢", "ExtendedZones by ice cold");
                    player.MessageFrom(chat_Name, "Syntax: /zone create Name, [float/radius], [bool/radiation], [float/radAmount], [bool/nosuicide], [bool/blockbuilding], [bool/godmode]");
                    player.MessageFrom(chat_Name, "Syntax: /zone remove Name");
                    player.MessageFrom(chat_Name, "Syntax: /zone reload");
                    player.MessageFrom(chat_Name, "Syntax: /zone save");
                    return;
                }
                if(args[0] == "create")
                {
                    if (args.Length == 8)
                    {
                        try
                        {
                            string name = args[1];
                            if(!Zones.Any(x => x.Value.name == name))
                            {
                                float radius = float.Parse(args[2]);
                                bool radiation = bool.Parse(args[3]);
                                float radA = float.Parse(args[4]);
                                bool nosuicide = bool.Parse(args[5]);
                                bool blockBuilding = bool.Parse(args[6]);
                                bool godmode = bool.Parse(args[7]);
                                string id = CreateUniqueID();
                                Zones.Add(id, new ZoneProps(name, player.Location.ToString(), radius, radiation, radA, nosuicide, godmode, blockBuilding, new string[] { "kit", "home", "sethome" }, DateTime.UtcNow));
                                player.MessageFrom(chat_Name, $"Succesfully created zone {name} with a radius of {radius} m, You can edit the options in the config file");                            
                            }
                            else
                                player.MessageFrom(chat_Name, "There is already a zone called " + name);


                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("[Zones]Error happend while trying to create a zone, " + ex.Message);
                            player.MessageFrom(chat_Name, $"Failed to create zone {args[0]} becaus of the error: {ex.Message}");                           
                        }
                    }                 
                }
                
                if(args[0] == "remove")
                {
                    if(args.Length == 2)
                    {
                        if (Zones.Any(x => x.Value.name == args[1]))
                        {
                            Zones.Remove(Zones.FirstOrDefault(x => x.Value.name == args[1]).Key);
                            player.MessageFrom(chat_Name, $"Succesfully removed zone {args[1]}");
                        }
                        else
                            player.MessageFrom(chat_Name, "There is not a zone called " + args[1]);
                    }
                }  
                if(args[0] == "reload")
                {
                    player.MessageFrom(chat_Name, "Reloading zones...");
                    Zones = ReadyConfigChecked(Zones, "db_Zones.json");
                    player.MessageFrom(chat_Name, "Zones have been reloaded!");
                }
                if(args[0] == "save")
                {
                    player.MessageFrom(chat_Name, "Saving zones to config file..");
                    this.SaveConfig();
                    player.MessageFrom(chat_Name, "Config has been succesfully saved!");

                }  
            }
        }

        private void SaveConfig()
        {
         
            JsonHelper.SaveFile(Zones, GetAbsoluteFilePath("db_Zones.json"));
          
        }

        private ZoneProps GetZoneAtLocation(Vector3 location)
        {
            try
            {
                if (Zones.Count > 0)
                {
                    var dict = new Dictionary<string, float>();
                    foreach (var pair in Zones)
                        dict.Add(pair.Key, Vector3.Distance(location, util.ConvertStringToVector3(pair.Value.location)));
                    return Zones[dict.Keys.Min()];
                }
                return null;
            }
            catch
            {
                return null;
            }
           
        }
        public void CheckPlayerZone(Player player)
        {
            var nearbyzone = GetZoneAtLocation(player.Location);
            string zone_name = nearbyzone.name == string.Empty ? "Unkown" : nearbyzone.name;
            float radius = nearbyzone.radius;
            float dist = Vector3.Distance(player.Location, util.ConvertStringToVector3(nearbyzone.location));
            if(dist < radius)
            {
                if(!PlayerZone.ContainsKey(player) || PlayerZone[player].name != nearbyzone.name)
                {
                    UnrestrictCommands(player);
                    PlayerZone[player] = nearbyzone;
                    player.MessageFrom(chat_Name, nearbyzone.join_message.Replace("%zone%", zone_name));
                    RestrictCommands(nearbyzone, player);

                    if (nearbyzone.radiation)
                        StartRadiationListener(player, nearbyzone);
                }              
            }
            else if(PlayerZone.ContainsKey(player))
            {
                PlayerZone.Remove(player);
                RadiationListener[player].Dispose();
                RadiationListener.Remove(player);
                player.MessageFrom(chat_Name, nearbyzone.leave_message.Replace("%zone%", zone_name));



            }
        }

        private void StartRadiationListener(Player player, ZoneProps zone)
        {
            var timer = RadiationListener[player] = new Timer();
            timer.Interval = zone.radiationInterval;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += (x, y) =>
            {
                player.AddRads(zone.radiationAmount);
            };
        }

        private void RestrictCommands(ZoneProps zone, Player player)
        {
            foreach (string cmd in zone.blockedCommands)
                player.RestrictCommand(cmd);
        }

        private void UnrestrictCommands(Player player)
        {
            if(PlayerZone.ContainsKey(player))
            {
                var zone = PlayerZone[player];
                foreach (string cmd in zone.blockedCommands)
                    player.UnRestrictCommand(cmd);
            }
        }

        private string CreateUniqueID()
        {
            StringBuilder builder = new StringBuilder();
            Enumerable
               .Range(65, 26)
                .Select(e => ((char)e).ToString())
                .Concat(Enumerable.Range(97, 26).Select(e => ((char)e).ToString()))
                .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
                .OrderBy(e => Guid.NewGuid())
                .Take(11)
                .ToList().ForEach(e => builder.Append(e));
            return builder.ToString();
        }
        public T ReadyConfigChecked<T>(T obj, string pathFile)
        {
            try
            {
                if (File.Exists(GetAbsoluteFilePath(pathFile)))
                {
                    return JsonHelper.ReadyFile<T>(GetAbsoluteFilePath(pathFile));
                }
                else
                {
                    JsonHelper.SaveFile(obj, GetAbsoluteFilePath(pathFile));
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error path: " + pathFile + "Error: " + ex);
                return default(T);
            }

        }
        public string GetAbsoluteFilePath(string fileName)
        {
            return Path.Combine(ModuleFolder, fileName);
        }   
    }
}
