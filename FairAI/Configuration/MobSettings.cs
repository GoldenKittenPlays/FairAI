using BepInEx.Configuration;

namespace FairAI.Configuration
{
    /// <summary>
    /// Per-enemy settings. Holds the ConfigEntry references ("cache"),
    /// so changes made at runtime (e.g. through LethalConfig) are seen immediately.
    /// </summary>
    internal sealed class MobSettings
    {
        private readonly ConfigEntry<bool> quicksandKill, mine, seamine, bertha, boomba, turretTarget, turretDamage;

        public MobSettings(ConfigEntry<bool> quicksandKill, ConfigEntry<bool> mine, ConfigEntry<bool> seamine,
                           ConfigEntry<bool> bertha, ConfigEntry<bool> boomba,
                           ConfigEntry<bool> turretTarget, ConfigEntry<bool> turretDamage)
        {
            this.quicksandKill = quicksandKill;
            this.mine = mine;
            this.seamine = seamine;
            this.bertha = bertha;
            this.boomba = boomba;
            this.turretTarget = turretTarget;
            this.turretDamage = turretDamage;
        }

        public bool QuicksandKill => quicksandKill.Value;
        public bool Mine          => mine.Value;
        public bool Seamine       => seamine.Value;
        public bool Bertha        => bertha.Value;
        public bool Boomba        => boomba.Value;
        public bool TurretTarget  => turretTarget.Value;
        public bool TurretDamage  => turretDamage.Value;
    }
}