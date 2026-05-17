using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(OverlayGenerator), "OnDestroy")]
    internal static class OverlayGeneratorLeakPatch
    {
        private static void Postfix(OverlayGenerator __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onGameStateLoad, __instance, "OnGameLoaded");
            EventCleanup.RemoveGameEvent(GameEvents.onPlanetariumTargetChanged, __instance, "OnMapFocusChange");
            EventCleanup.RemoveStaticDelegateField(typeof(MapView), "OnEnterMapView", __instance, "OnEnterMapView");
            EventCleanup.RemoveStaticDelegateField(typeof(MapView), "OnExitMapView", __instance, "OnExitMapView");
        }
    }
}
