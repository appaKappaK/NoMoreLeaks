using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(ModuleCargoPart))]
    internal static class ModuleCargoPartLeakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnInitialize")]
        private static void OnInitializePrefix(ModuleCargoPart __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.OnEVAConstructionMode, __instance, "OnEVAConstructionMode");
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(ModuleCargoPart __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.OnEVAConstructionMode, __instance, "OnEVAConstructionMode");
        }
    }
}
