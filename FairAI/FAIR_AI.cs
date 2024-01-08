using Unity.Netcode;

namespace FairAI
{
    internal class FAIR_AI : NetworkBehaviour
    {
        public EnemyAI targetWithRotation;

        [ClientRpc]
        public void SwitchedTargetedEnemyClientRpc(Turret turret, EnemyAI enemy, bool setModeToCharging = false)
        {
            targetWithRotation = enemy;
            if (setModeToCharging)
            {
                turret.SwitchTurretMode(1);
            }
        }

        [ClientRpc]
        public void RemoveTargetedEnemyClientRpc()
        {
            targetWithRotation = null;
        }
    }
}
