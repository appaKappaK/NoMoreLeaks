using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(EVAConstructionModeEditor), "OnDestroy")]
    internal static class EVAConstructionModeEditorLeakPatch
    {
        private static void Postfix(EVAConstructionModeEditor __instance)
        {
            object angleSnapButton = EventCleanup.GetInstanceField(__instance, "angleSnapButton");
            EventCleanup.RemoveInstanceEventField(angleSnapButton, "onClick", __instance, "SnapButton");
        }
    }
}
