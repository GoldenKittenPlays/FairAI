using System.Collections.Generic;
using BepInEx.Configuration;

namespace FairAI.Configuration
{
    /// <summary>
    /// Cache of FairAI's config entries. Binding is done in Utils:
    /// general entries by Utils.BindGeneralSettings() (once, from Plugin.Awake),
    /// per-mob entries by Utils.SetupConfig() (from the in-game patches).
    /// Read settings through the properties; they always reflect the live config value.
    /// </summary>
    internal sealed class PluginSettings
    {
        // ---- Entries, assigned by Utils.BindGeneralSettings() ----

        // [General]
        internal ConfigEntry<bool> immortalAffected;

        // [ExplosionConfig]
        internal ConfigEntry<float> explosionDamage;

        // [Quick Sand Config]
        internal ConfigEntry<float> sinkTime;
        internal ConfigEntry<float> slowingSpeed;

        // [TurretConfig]
        internal ConfigEntry<float> turretEnemyDamage;
        internal ConfigEntry<float> turretPlayerDamage;
        internal ConfigEntry<bool> hitOtherTurrets;

        // [Mobs] global toggles
        /*
        * Global mob settings are overriden by specific mob settings when the
        * former are set to True: the latter are ignored when the Global is False.
        */
        internal ConfigEntry<bool> quicksandAllMobs;
        internal ConfigEntry<bool> explodeAllMobs;
        internal ConfigEntry<bool> boombaAllMobs;
        internal ConfigEntry<bool> seamineAllMobs;
        internal ConfigEntry<bool> berthaAllMobs;
        internal ConfigEntry<bool> turretTargetAllMobs;
        internal ConfigEntry<bool> turretDamageAllMobs;
        internal ConfigEntry<bool> checkForPlayersInside;

        // ---- Read access ----

        public bool ImmortalAffected      => immortalAffected.Value;

        public float ExplosionDamage      => explosionDamage.Value;

        public float SinkTime             => sinkTime.Value;
        public float SlowingSpeed         => slowingSpeed.Value;

        public float TurretEnemyDamage    => turretEnemyDamage.Value;
        public float TurretPlayerDamage   => turretPlayerDamage.Value;
        public bool HitOtherTurrets       => hitOtherTurrets.Value;

        public bool QuicksandAllMobs      => quicksandAllMobs.Value;
        public bool ExplodeAllMobs        => explodeAllMobs.Value;
        public bool BoombaAllMobs         => boombaAllMobs.Value;
        public bool SeamineAllMobs        => seamineAllMobs.Value;
        public bool BerthaAllMobs         => berthaAllMobs.Value;
        public bool TurretTargetAllMobs   => turretTargetAllMobs.Value;
        public bool TurretDamageAllMobs   => turretDamageAllMobs.Value;
        public bool CheckForPlayersInside => checkForPlayersInside.Value;

        // ---- Per-mob settings, filled by Utils.SetupConfig() ----

        private readonly Dictionary<EnemyType, MobSettings> mobs = new();

        public bool HasMob(EnemyType enemy) => mobs.ContainsKey(enemy);

        public void AddMob(EnemyType enemy, MobSettings settings) => mobs[enemy] = settings;

        /// <summary>Returns null if this enemy hasn't been registered yet; callers should fail closed.</summary>
        public MobSettings GetMob(EnemyType enemy) => mobs.TryGetValue(enemy, out MobSettings m) ? m : null;
    }
}