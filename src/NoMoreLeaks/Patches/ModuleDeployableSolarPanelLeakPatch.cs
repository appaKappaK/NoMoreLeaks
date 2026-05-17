using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(ModuleDeployableSolarPanel), "OnDestroy")]
    internal static class ModuleDeployableSolarPanelLeakPatch
    {
        private static void Prefix(ModuleDeployableSolarPanel __instance)
        {
            Cleanup(__instance);
        }

        private static void Postfix(ModuleDeployableSolarPanel __instance)
        {
            Cleanup(__instance);
        }

        private static void Cleanup(ModuleDeployableSolarPanel instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onVesselChange, instance, "onVesselFocusChange");
        }
    }
}
