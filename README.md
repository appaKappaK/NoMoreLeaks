# NoMoreLeaks

Small KSP/Harmony patch plugin for stock `Assembly-CSharp` event callback leaks reported by KSPCommunityFixes.

This mod does not replace KSPCommunityFixes. It tries to prevent selected stock callbacks from remaining subscribed after their owning Unity object is destroyed.

## Current Patch Targets

- `ModuleCargoPart | OnEVAConstructionMode`
- `ModuleInventoryPart | onPartActionUICreate`
- `ModuleInventoryPart | onModuleInventoryChanged`
- `ModuleInventoryPart | onEditorPartEvent`
- `ModuleInventoryPart | OnPartPurchased`
- `ModuleDeployableSolarPanel | onVesselChange`
- `ModuleControlSurface | onVesselReferenceTransformSwitch`
- `EVAConstructionModeEditor | OnClick`
- `RunwayCollisionHandler | OnDestructibleLoaded`
- `BuildingPickerItem | OnInViewChange`
- `BuildingPickerItem | OnClick`
- `OverlayGenerator | onPlanetariumTargetChange`
- `OverlayGenerator | onGameStateLoad`
- `OverlayGenerator | MapView.OnEnterMapView`
- `OverlayGenerator | MapView.OnExitMapView`
- `MapView | TimingManager.Instance.timing5.onLateUpdate`
- `VesselAutopilotUI | OnGameSettingsApplied`
- `NavBallToggle | OnMapExited`
- `CommNetVessel | onPlanetariumTargetChange`

## Dependency

Requires Harmony 2 installed as:

```text
GameData/000_Harmony/0Harmony.dll
```
