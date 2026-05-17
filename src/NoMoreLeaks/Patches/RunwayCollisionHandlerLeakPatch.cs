using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(RunwayCollisionHandler), "OnDestroy")]
    internal static class RunwayCollisionHandlerLeakPatch
    {
        private static void Postfix(RunwayCollisionHandler __instance)
        {
            EventCleanup.RemoveGameEvent(DestructibleBuilding.OnLoaded, __instance, "OnSectionLoaded");
        }
    }
}
