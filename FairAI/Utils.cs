using BepInEx.Configuration;
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

        public static void SetupConfig()
        {
            //This happens at the end of waiting for entrance teleport spawn
            if (!Plugin.itemList.SequenceEqual(Plugin.items) || Plugin.items.Count == 0)
            {
                Plugin.items = [.. Resources.FindObjectsOfTypeAll<Item>()];
                Plugin.itemList = Plugin.items;
            }
            Plugin.allHittablesMask = StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers | 2621448 | Plugin.enemyMask;
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("General", "ImmortalAffected")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("General", // Section Title
                "ImmortalAffected", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If set to on/true immortal enemies will be targeted and trip off traps."); // Description
                if (Plugin.lethalConfigEnabled)
                {
                    AddLethalConfigBoolItem(tempEntry);
                }
                if (Plugin.enemies.Count == 0)
                {
                    if (!Plugin.GetBool("General", "ImmortalAffected"))
                    {
                        Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e.canDie)];
                        Plugin.allEnemies = [.. Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null)];
                        Plugin.enemyList = Plugin.enemies;
                    }
                    else
                    {
                        Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null)];
                        Plugin.allEnemies = [.. Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null)];
                        Plugin.enemyList = Plugin.enemies;
                    }
                }
                else if (!Plugin.enemyList.SequenceEqual(Plugin.enemies))
                {
                    if (!Plugin.GetBool("General", "ImmortalAffected"))
                    {
                        Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null && e.canDie)];
                        Plugin.allEnemies = [.. Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null)];
                        Plugin.enemyList = Plugin.enemies;
                    }
                    else
                    {
                        Plugin.enemies = [.. Resources.FindObjectsOfTypeAll<EnemyType>().Where(e => e != null)];
                        Plugin.allEnemies = [.. Resources.FindObjectsOfTypeAll(typeof(EnemyType)).Cast<EnemyType>().Where(e => e != null)];
                        Plugin.enemyList = Plugin.enemies;
                    }
                }
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("ExplosionConfig", "Damage")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("ExplosionConfig", // Section Title
                "Damage", // The key of the configuration option in the configuration file
                                             1f, // The default value
                                             "Damage explosions will do outside the kill radius"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigFloatItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Quick Sand Config", "Sink Time")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("Quick Sand Config", // Section Title
                "Sink Time", // The key of the configuration option in the configuration file
                                             5f, // The default value
                                             "Time Until A Enemy Is Considered Sunk And Will Be Killed"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigFloatItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Quick Sand Config", "Slowing Speed")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("Quick Sand Config", // Section Title
                "Slowing Speed", // The key of the configuration option in the configuration file
                                             33f, // The default value
                                             "Percentage of Original Speed Enemies Move In Quick Sand"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigFloatItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "Enemy Damage")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "Enemy Damage", // The key of the configuration option in the configuration file
                                             1f, // The default value
                                             "Damage Turrets will Do To Enemies"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigFloatItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "Player Damage")))
            {
                ConfigEntry<float> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "Player Damage", // The key of the configuration option in the configuration file
                                             50f, // The default value
                                             "Damage Turrets will Do To Players"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigFloatItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("TurretConfig", "HitOtherTurrets")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("TurretConfig", // Section Title
                "HitOtherTurrets", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "If turrets can hit other turrets when firing.(Does not make them target other turrets)"); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "QuicksandAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "QuicksandAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Quicksand interactions."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "ExplodeAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "ExplodeAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Mines."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BoombaAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BoombaAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Boombas."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "SeamineAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "SeamineAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Seamines."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "BerthaAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "BerthaAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Set Off Big Berthas."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "TurretTargetAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "TurretTargetAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Targeted By Turrets."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "TurretDamageAllMobs")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "TurretDamageAllMobs", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Leave On To Customise Mobs Below Or Turn Off To Make All Mobs Unable To Be Killed By Turrets."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", "CheckForPlayersInside")))
            {
                ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // Section Title
                "CheckForPlayersInside", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "Whether to check for players inside the dungeon before anything else occurs."); // Description
                if (Plugin.lethalConfigEnabled)
                    AddLethalConfigBoolItem(tempEntry);
            }
            foreach (EnemyType enemy in Plugin.allEnemies)
            {
                string mobName = Plugin.RemoveInvalidCharacters(enemy.enemyName);
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Quicksand Kill")))
                {
                    ConfigEntry<bool> tempEntry;
                    if (enemy.canDie)
                    {
                        tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Quicksand Kill", // The key of the configuration option in the configuration file
                                             true, // The default value
                                             "Is it killable by quicksand?"); // Description
                        if (Plugin.lethalConfigEnabled)
                            AddLethalConfigBoolItem(tempEntry);
                    }
                    else
                    {
                        tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Quicksand Kill", // The key of the configuration option in the configuration file
                                             false, // The default value
                                             "Is it killable by quicksand?"); // Description
                        if (Plugin.lethalConfigEnabled)
                            AddLethalConfigBoolItem(tempEntry);
                    }
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Mine")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Mine", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Does it set off the landmine or not?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Seamine")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Seamine", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Does it set off the Surfaced Seamine or not?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Bertha")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Bertha", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Does it set off the Surfaced Big Bertha or not?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Boomba")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Boomba", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Does it set off the LethalThings Boomba or not?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Turret Target")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Turret Target", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Is it targetable by turrets?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
                if (!Plugin.Instance.Config.ContainsKey(new ConfigDefinition("Mobs", mobName + ".Turret Damage")))
                {
                    ConfigEntry<bool> tempEntry = Plugin.Instance.Config.Bind("Mobs", // The section under which the option is shown
                                             mobName + ".Turret Damage", // The key of the configuration option in the configuration file
                                             enemy.canDie, // The default value
                                             "Is it damageable by turrets?"); // Description
                    if (Plugin.lethalConfigEnabled)
                        AddLethalConfigBoolItem(tempEntry);
                }
            }
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
