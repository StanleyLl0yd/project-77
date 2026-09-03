using Project77.Game;
using UnityEngine;

namespace Project77.Core
{
    public sealed class PrototypeEntryPoint : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<EnergyRoutingPrototypeController>() == null)
            {
                gameObject.AddComponent<EnergyRoutingPrototypeController>();
            }
        }
    }
}
