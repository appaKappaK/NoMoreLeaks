using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(MapView), "OnDestroy")]
    internal static class MapViewLeakPatch
    {
        private static void Postfix(MapView __instance)
        {
            object timingManager = EventCleanup.GetStaticMember(typeof(TimingManager), "Instance");
            object timing5 = EventCleanup.GetInstanceField(timingManager, "timing5");
            EventCleanup.RemoveDelegatesOwnedBy(timing5, "onLateUpdate", __instance);
        }
    }
}
