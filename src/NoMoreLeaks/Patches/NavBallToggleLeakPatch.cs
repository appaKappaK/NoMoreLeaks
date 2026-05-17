using HarmonyLib;
using KSP.UI.Screens.Flight;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(NavBallToggle), "OnDestroy")]
    internal static class NavBallToggleLeakPatch
    {
        private static void Postfix(NavBallToggle __instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.OnMapExited, __instance, "OnMapExited");
        }
    }
}
