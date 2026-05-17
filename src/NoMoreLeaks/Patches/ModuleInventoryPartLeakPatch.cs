using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(ModuleInventoryPart))]
    internal static class ModuleInventoryPartLeakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnStart")]
        private static void OnStartPrefix(ModuleInventoryPart __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPrefix(ModuleInventoryPart __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(ModuleInventoryPart __instance)
        {
            Cleanup(__instance);
        }

        private static void Cleanup(ModuleInventoryPart instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUICreate, instance, "onPartActionUIOpened");
            EventCleanup.RemoveGameEvent(GameEvents.onModuleInventoryChanged, instance, "OnModuleInventoryChanged");
            EventCleanup.RemoveGameEvent(GameEvents.onEditorPartEvent, instance, "OnEditorPartEvent");
            EventCleanup.RemoveGameEvent(GameEvents.OnPartPurchased, instance, "OnPartPurchased");
            EventCleanup.RemoveGameEvent(GameEvents.OnInventoryPartOnMouseChanged, instance, "VesselEditorPartHighlighter");
        }
    }
}
