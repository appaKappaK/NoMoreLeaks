# NoMoreLeaks

Small KSP/Harmony patch plugin for stock `Assembly-CSharp` event callback leaks reported by KSPCommunityFixes.

This mod does not replace KSPCommunityFixes. It tries to prevent selected callbacks from remaining subscribed after their owning Unity object is destroyed, so KSPCF has less cleanup work to do later.

## Install

Copy the packaged folder into KSP `GameData`:

```text
GameData/NoMoreLeaks/
  NoMoreLeaks.version
  Plugins/NoMoreLeaks.dll
```

Harmony 2 is required:

```text
GameData/000_Harmony/0Harmony.dll
```

On startup, the log should contain:

```text
[NoMoreLeaks] Harmony patches applied
```

When the runtime sweeper removes stale callbacks, the log should contain:

```text
[NoMoreLeaks] Removed N destroyed callback owners
```

## Current Patch Targets

Stock KSP / DLC callback owners:

- `ModuleCargoPart | OnEVAConstructionMode`
- `ModuleInventoryPart | onPartActionUICreate`
- `ModuleInventoryPart | onModuleInventoryChanged`
- `ModuleInventoryPart | onEditorPartEvent`
- `ModuleInventoryPart | OnPartPurchased`
- `ModuleInventoryPart | OnInventoryPartOnMouseChanged`
- `UIPartActionInventorySlot | OnEVACargoMode`
- `UIPartActionInventorySlot | onEditorPartDeleted`
- `ModuleDeployableSolarPanel | onVesselChange`
- `ModuleControlSurface | onEditorPartEvent`
- `ModuleControlSurface | onVesselReferenceTransformSwitch`
- `ModuleRobotArmScanner | onVesselChange`
- `ModuleGroundPart | onPartActionUIShown`
- `ModuleGroundPart | onPartActionUIDismiss`
- `ModuleGroundPart | onVesselChange`
- `ModuleGroundPart | onPartWillDie`
- `ModuleGroundPart | onLevelConfirmExit`
- `ModuleGroundPart | OnEVAConstructionMode`
- `ModuleGroundSciencePart | onGroundScienceDeregisterCluster`
- `ModuleGroundSciencePart | onGroundScienceClusterUpdated`
- `ModuleGroundSciencePart | onGroundScienceClusterPowerStateChanged`
- `ModuleGroundExperiment | onGroundScienceGenerated`
- `ModuleGroundExperiment | onGroundScienceTransmitted`
- `ModuleGroundExpControl | onGroundSciencePartDeployed`
- `ModuleGroundExpControl | onGroundSciencePartEnabledStateChanged`
- `ModuleGroundExpControl | onGroundSciencePartRemoved`
- `KerbalEVA | OnROCExperimentStored`
- `KerbalEVA | OnROCExperimentReset`
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

Optional third-party cleanup currently covered:

- `PlanetarySurfaceStructures.ModuleKPBSCorridorNodes | onEditorShipModified`

`RealAntennas` callbacks are intentionally not patched here.

## How It Works

The plugin uses Harmony patches on known stock teardown paths such as module `OnDestroy()`, `Part.OnDestroy`, `Part.RemoveModule`, and `Part.RemoveModules`.

It also runs a persistent sweeper from a `DontDestroyOnLoad` KSP addon. The sweeper checks selected `GameEvents` every frame/scene transition and removes event entries whose owners are already destroyed. This is needed because some stock owners become destroyed before their normal unsubscribe path runs.

## Validation

Use KSPCF memory leak logging and compare the exported summary before and after a run.

Expected improvement is that covered callback owners either disappear from the KSPCF summary or drop significantly. If a covered owner still appears, check `KSP.log` for:

```text
[NoMoreLeaks] Removed
```

If that line never appears during gameplay, the sweeper did not catch any destroyed owners before KSPCF did.

## Build Notes

The project targets old .NET Framework for KSP compatibility. On Linux, the DLL can be built with Mono `mcs` against the local KSP assemblies.

Generated binaries under `GameData/NoMoreLeaks/Plugins/` and build intermediates under `bin/` / `obj/` are ignored by git.
