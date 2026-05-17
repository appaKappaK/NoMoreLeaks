using HarmonyLib;
using KSP.UI.Screens.SpaceCenter;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(BuildingPickerItem), "OnDestroy")]
    internal static class BuildingPickerItemLeakPatch
    {
        private static void Postfix(BuildingPickerItem __instance)
        {
            object building = EventCleanup.GetInstanceField(__instance, "building");
            EventCleanup.RemoveInstanceEventField(building, "OnClick", __instance, "OnBuildingClick");
            EventCleanup.RemoveInstanceEventField(building, "OnInViewChange", __instance, "OnBuildingInView");
        }
    }
}
