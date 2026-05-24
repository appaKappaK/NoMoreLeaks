using HarmonyLib;
using UnityEngine;

namespace NoMoreLeaks
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class Startup : MonoBehaviour
    {
        private const string HarmonyId = "matth.nomoreleaks";
        private const float SweepInterval = 0.5f;
        private float nextSweep;

        private void Awake()
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            new Harmony(HarmonyId).PatchAll();
            Debug.Log("[NoMoreLeaks] Harmony patches applied");
            InventoryCallbackSweeper.Sweep();
        }

        private void OnLevelWasLoaded(int level)
        {
            InventoryCallbackSweeper.Sweep();
            nextSweep = Time.realtimeSinceStartup + SweepInterval;
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextSweep) return;

            nextSweep = Time.realtimeSinceStartup + SweepInterval;
            InventoryCallbackSweeper.Sweep();
        }
    }
}
