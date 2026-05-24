using UnityEngine;

namespace NoMoreLeaks
{
    internal static class InventoryCallbackSweeper
    {
        internal static void Sweep()
        {
            int removed = 0;
            removed += EventCleanup.RemoveDestroyedStockGameEventOwners();
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onPartActionUICreate, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onModuleInventoryChanged, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onEditorPartEvent, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.OnPartPurchased, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.OnInventoryPartOnMouseChanged, typeof(ModuleInventoryPart));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.OnEVACargoMode, typeof(UIPartActionInventorySlot));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onEditorPartDeleted, typeof(UIPartActionInventorySlot));
            removed += EventCleanup.RemoveDestroyedOwners(GameEvents.onVesselChange, typeof(Expansions.Serenity.ModuleRobotArmScanner));
            removed += EventCleanup.RemoveDestroyedOwnersByTypeName(GameEvents.onEditorShipModified, "PlanetarySurfaceStructures.ModuleKPBSCorridorNodes");
            removed += RemoveDestroyedSpaceTrackingCallbacks();

            if (removed > 0)
                Debug.Log("[NoMoreLeaks] Removed " + removed + " destroyed callback owners");
        }

        private static int RemoveDestroyedSpaceTrackingCallbacks()
        {
            if (FlightGlobals.Vessels == null) return 0;

            int removed = 0;
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel vessel = FlightGlobals.Vessels[i];
                if (vessel == null || vessel.orbitRenderer == null) continue;

                removed += EventCleanup.RemoveDestroyedDelegateMemberOwners(vessel.orbitRenderer, "onVesselIconClicked");
            }

            return removed;
        }
    }
}
