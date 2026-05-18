using HarmonyLib;
using UnityEngine;

namespace NoMoreLeaks
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class Startup : MonoBehaviour
    {
        private const string HarmonyId = "matth.nomoreleaks";
        private const float InventorySweepInterval = 0.5f;
        private float nextInventorySweep;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            new Harmony(HarmonyId).PatchAll();
            Debug.Log("[NoMoreLeaks] Harmony patches applied");
            InventoryCallbackSweeper.Sweep();
        }

        private void OnLevelWasLoaded(int level)
        {
            InventoryCallbackSweeper.Sweep();
            nextInventorySweep = Time.realtimeSinceStartup + InventorySweepInterval;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextInventorySweep) return;

            nextInventorySweep = Time.realtimeSinceStartup + InventorySweepInterval;
            InventoryCallbackSweeper.Sweep();
        }

        private void LateUpdate()
        {
            InventoryCallbackSweeper.Sweep();
        }
    }
}
