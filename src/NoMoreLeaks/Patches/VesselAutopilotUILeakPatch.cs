using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(VesselAutopilotUI), "OnDestroy")]
    internal static class VesselAutopilotUILeakPatch
    {
        private static void Postfix(VesselAutopilotUI __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.OnGameSettingsApplied, __instance, "onGameParametersChanged");
        }
    }
}
