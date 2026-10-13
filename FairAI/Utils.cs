using BepInEx.Configuration;
using FairAI.Configuration;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FairAI
{
    public class Utils
    {
        public static void AddLethalConfigBoolItem(ConfigEntry<bool> tempEntry)
        {
            Type boolCheckBoxType =
            Plugin.lethalConfigAssembly.GetType("LethalConfig.ConfigItems.BoolCheckBoxConfigItem");
            ConstructorInfo ctor = boolCheckBoxType.GetConstructor([typeof(ConfigEntry<bool>)]);
            object checkBoxItem = ctor.Invoke([tempEntry]);
            Type lethalConfigManagerType = Plugin.lethalConfigAssembly.GetType("LethalConfig.LethalConfigManager");
            MethodInfo addConfigItemMethod = lethalConfigManagerType.GetMethod(
            "AddConfigItem",
            [boolCheckBoxType.BaseType]
            );
            addConfigItemMethod.Invoke(null, [checkBoxItem]);
        }

        public static void AddLethalConfigFloatItem(ConfigEntry<float> tempEntry)
        {
            Type floatInputType =
            Plugin.lethalConfigAssembly.GetType("LethalConfig.ConfigItems.FloatInputFieldConfigItem");
            ConstructorInfo ctor = floatInputType.GetConstructor([typeof(ConfigEntry<float>)]);
            object checkBoxItem = ctor.Invoke([tempEntry]);
            Type lethalConfigManagerType = Plugin.lethalConfigAssembly.GetType("LethalConfig.LethalConfigManager");
            MethodInfo addConfigItemMethod = lethalConfigManagerType.GetMethod(
            "AddConfigItem",
            [floatInputType.BaseType]
            );
            addConfigItemMethod.Invoke(null, [checkBoxItem]);
        }

        // ------------------------------------------------------------------
        // Binding helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Binds (or fetches the already-bound) entry and registers it with LethalConfig
        /// only the first time, so repeated calls never create duplicate LethalConfig items.
        /// </summary>
        private static ConfigEntry<T> BindEntry<T>(string section, string key, T defaultValue, string description,
                                                   Action<ConfigEntry<T>> addLethalConfigItem)
        {
            var definition = new ConfigDefinition(section, key);
            bool isNew = !Plugin.Instance.Config.ContainsKey(definition);
            ConfigEntry<T> entry = Plugin.Instance.Config.Bind(definition, defaultValue, new ConfigDescription(description));
            if (isNew && Plugin.lethalConfigEnabled)
                addLethalConfigItem(entry);
            return entry;
        }

        private static ConfigEntry<bool> BindBool(string section, string key, bool defaultValue, string description)
            => BindEntry(section, key, defaultValue, description, AddLethalConfigBoolItem);

        private static ConfigEntry<float> BindFloat(string section, string key, float defaultValue, string description)
            => BindEntry(section, key, defaultValue, description, AddLethalConfigFloatItem);

        // ------------------------------------------------------------------
        // General settings: called once from Plugin.Awake(), after Plugin.Settings is created.
        // None of these depend on game state. Section/key strings are unchanged so
        // existing config files keep their values.
        // ------------------------------------------------------------------

        public static void BindGeneralSettings()
        {
            PluginSettings s = Plugin.Settings;

            s.immortalAffected = BindBool("General", "ImmortalAffected", false,
                "If set to on/true immortal enemies will be targeted and trip off traps.");

            s.explosionDamage = BindFloat("ExplosionConfig", "Damage", 1f,
                "Damage explosions will do outside the kill radius");

            s.sinkTime = BindFloat("Quick Sand Config", "Sink Time", 5f,
                "Time Until A Enemy Is Considered Sunk And Will Be Killed");
            s.slowingSpeed = BindFloat("Quick Sand Config", "Slowing Speed", 33f,
                "Percentage of Original Speed Enemies Move In Quick Sand");

            s.turretEnemyDamage = BindFloat("TurretConfig", "Enemy Damage", 1f,
                "Damage Turrets will Do To Enemies");
            s.turretPlayerDamage = BindFloat("TurretConfig", "Player Damage", 50f,
                "Damage Turrets will Do To Players");
            s.hitOtherTurrets = BindBool("TurretConfig", "HitOtherTurrets", false,
                "If turrets can hit other turrets when firing.(Does not make them target other turrets)");

            s.quicksandAllMobs = BindBool("Mobs", "QuicksandAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Quicksand interactions.");
            s.explodeAllMobs = BindBool("Mobs", "ExplodeAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Mines.");
            s.boombaAllMobs = BindBool("Mobs", "BoombaAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Boombas.");
            s.seamineAllMobs = BindBool("Mobs", "SeamineAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Seamines.");
            s.berthaAllMobs = BindBool("Mobs", "BerthaAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Big Berthas.");
            s.turretTargetAllMobs = BindBool("Mobs", "TurretTargetAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Targeted By Turrets.");
            s.turretDamageAllMobs = BindBool("Mobs", "TurretDamageAllMobs", true,
                "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Killed By Turrets.");
            s.checkForPlayersInside = BindBool("Mobs", "CheckForPlayersInside", false,
                "Whether to check for players inside the dungeon before anything else occurs.");
        }

        // ------------------------------------------------------------------
        // Dynamic Settings: still called by patches after a "game" has started.
        // The first call scans items/enemies and registers per-mob settings;
        // later calls cost one dictionary check per enemy.
        // ------------------------------------------------------------------

        private static bool s_enemiesScanned;

        public static void SetupConfig()
        {
            //This happens at the end of waiting for entrance teleport spawn
            if (!Plugin.itemList.SequenceEqual(Plugin.items) || Plugin.items.Count == 0)
            {
                Plugin.items = [.. Resources.FindObjectsOfTypeAll<Item>()];
                Plugin.itemList = Plugin.items;
            }

            Plugin.allHittablesMask = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | 2621448 | Plugin.enemyMask;

            // Same conditions as before. Previously this lived inside the
            // "ImmortalAffected not yet bound" block, so it only ever ran on the first call.
            if (!s_enemiesScanned)
            {
                s_enemiesScanned = true;
                if (Plugin.enemies.Count == 0 || !Plugin.enemyList.SequenceEqual(Plugin.enemies))
                    ScanEnemies();
            }

            foreach (EnemyType enemy in Plugin.allEnemies)
            {
                if (Plugin.Settings.HasMob(enemy))
                    continue; // already registered on an earlier call

                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName);
                bool d = enemy.canDie;

                Plugin.Settings.AddMob(enemy, new MobSettings(
                    BindBool("Mobs", mobName + ".Quicksand Kill", d, "Is it killable by quicksand?"),
                    BindBool("Mobs", mobName + ".Mine",           d, "Does it set off the landmine or not?"),
                    BindBool("Mobs", mobName + ".Seamine",        d, "Does it set off the Surfaced Seamine or not?"),
                    BindBool("Mobs", mobName + ".Bertha",         d, "Does it set off the Surfaced Big Bertha or not?"),
                    BindBool("Mobs", mobName + ".Boomba",         d, "Does it set off the LethalThings Boomba or not?"),
                    BindBool("Mobs", mobName + ".Turret Target",  d, "Is it targetable by turrets?"),
                    BindBool("Mobs", mobName + ".Turret Damage",  d, "Is it damageable by turrets?")));
            }
        }

        private static void ScanEnemies()
        {
            Plugin.allEnemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null)]; 

            if (Plugin.Settings.ImmortalAffected)
                Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null)]; // possibly duplicate from first line of this method
            else
                Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null && e.canDie)];

            Plugin.enemyList = Plugin.enemies;
        }

        public static int[] GetLateGameUpgradeTier(String upgradeName)
        {
            try
            {
                // 2️⃣ Get the UpgradeApi type.
                Type upgradeApiType = Plugin.lguAssembly.GetType("MoreShipUpgrades.API.UpgradeApi");
                if (upgradeApiType == null)
                {
                    Plugin.logger.LogError("[FairAI] Could not find UpgradeApi type!");
                    return [];
                }

                // 3️⃣ Get the GetRankableUpgradeNodes() method.
                MethodInfo getRankableMethod = upgradeApiType.GetMethod(
                    "GetRankableUpgradeNodes",
                    BindingFlags.Public | BindingFlags.Static
                );

                if (getRankableMethod == null)
                {
                    Plugin.logger.LogError("[FairAI] Could not find GetRankableUpgradeNodes() method!");
                    return [];
                }

                // 4️⃣ Invoke it (since it’s static, no instance is needed).
                object result = getRankableMethod.Invoke(null, null);

                // 5️⃣ Cast to IEnumerable (we don’t know the exact generic type at compile time).
                var enumerable = result as System.Collections.IEnumerable;
                if (enumerable == null)
                {
                    Plugin.logger.LogError("[FairAI] Result is not an IEnumerable!");
                    return [];
                }

                // 6️⃣ For each CustomTerminalNode, extract the data.
                foreach (var node in enumerable)
                {
                    Type nodeType = node.GetType();

                    PropertyInfo nameProp = nodeType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo maxProp = nodeType.GetProperty("MaxUpgrade", BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo currentProp = nodeType.GetProperty("CurrentUpgrade", BindingFlags.Public | BindingFlags.Instance);

                    string name = nameProp?.GetValue(node)?.ToString() ?? "(unknown)";
                    if (!name.Equals("(unknown)"))
                    {
                        if (name.Equals(upgradeName, StringComparison.OrdinalIgnoreCase))
                        {
                            int maxUpgrade = (int)(maxProp?.GetValue(node) ?? 0);
                            int currentUpgrade = (int)(currentProp?.GetValue(node) ?? 0);
                            Plugin.logger.LogInfo($"[FairAI] {name} — Current: {currentUpgrade}, Max: {maxUpgrade}");
                            return [currentUpgrade, maxUpgrade];
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.logger.LogError($"[FairAI] Error: {ex}");
            }
            return [];
        }
    }
}