using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(VesselAutopilotUI))]
    internal static class VesselAutopilotUILeakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        private static void StartPrefix()
        {
            EventCleanup.RemoveDestroyedOwners(GameEvents.OnGameSettingsApplied, typeof(VesselAutopilotUI));
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPrefix(VesselAutopilotUI __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(VesselAutopilotUI __instance)
        {
            Cleanup(__instance);
        }

        private static void Cleanup(VesselAutopilotUI instance)
        {
            EventCleanup.RemoveOwner(GameEvents.OnGameSettingsApplied, instance);
            EventCleanup.RemoveGameEvent(GameEvents.OnGameSettingsApplied, instance, "onGameParametersChanged");
        }
    }
}
