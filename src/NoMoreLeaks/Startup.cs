using HarmonyLib;
using UnityEngine;

namespace NoMoreLeaks
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class Startup : MonoBehaviour
    {
        private const string HarmonyId = "matth.nomoreleaks";
        private const float InventorySweepInterval = 2f;
        private float nextInventorySweep;

        private void Awake()
        {
            new Harmony(HarmonyId).PatchAll();
            Debug.Log("[NoMoreLeaks] Harmony patches applied");
            SweepDestroyedInventoryCallbacks();
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup < nextInventorySweep) return;

            nextInventorySweep = Time.realtimeSinceStartup + InventorySweepInterval;
            SweepDestroyedInventoryCallbacks();
        }

        private static void SweepDestroyedInventoryCallbacks()
        {
            int removed = 0;
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onPartActionUICreate, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onModuleInventoryChanged, typeof(ModuleInventoryPart));

            if (removed > 0)
                Debug.Log("[NoMoreLeaks] Removed " + removed + " destroyed ModuleInventoryPart callbacks");
        }
    }
}
