using UnityEngine;

namespace FairAI.Component
{
    internal class FAIR_AI : MonoBehaviour
    {
        void Awake()
        {
            Plugin.logger.LogInfo("Fairness has been dealt.");
        }
    }
}
