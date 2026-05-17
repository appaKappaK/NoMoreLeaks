using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(ModuleControlSurface))]
    internal static class ModuleControlSurfaceLeakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnStart")]
        private static void OnStartPrefix(ModuleControlSurface __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPrefix(ModuleControlSurface __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(ModuleControlSurface __instance)
        {
            Cleanup(__instance);
        }

        private static void Cleanup(ModuleControlSurface instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onVesselReferenceTransformSwitch, instance, "onVesselReferenceTransformSwitch");
        }
    }
}
