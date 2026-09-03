using Project77.Game;
using UnityEngine;

namespace Project77.Core
{
    public sealed class PrototypeEntryPoint : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<PrototypePlaytestRuntime>() == null)
            {
                gameObject.AddComponent<PrototypePlaytestRuntime>();
            }

            if (GetComponent<PrototypeVariantSelector>() == null)
            {
                gameObject.AddComponent<PrototypeVariantSelector>();
            }
        }
    }
}
