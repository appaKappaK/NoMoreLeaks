using CommNet;
using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(CommNetVessel), "OnDestroy")]
    internal static class CommNetVesselLeakPatch
    {
        private static void Postfix(CommNetVessel __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onPlanetariumTargetChanged, __instance, "OnMapFocusChange");
        }
    }
}
