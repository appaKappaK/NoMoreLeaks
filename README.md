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
- `SpaceTracking | OnVesselIconClicked`

Optional third-party cleanup currently covered:

- `PlanetarySurfaceStructures.ModuleKPBSCorridorNodes | onEditorShipModified`

`RealAntennas` callbacks are intentionally not patched here.

## How It Works

The plugin uses Harmony patches on known stock teardown paths such as module `OnDestroy()`, `Part.OnDestroy`, `Part.RemoveModule`, and `Part.RemoveModules`.

It also runs a persistent sweeper from a `DontDestroyOnLoad` KSP addon. The sweeper checks selected `GameEvents` every frame/scene transition and removes event entries whose owners are already destroyed. This is needed because some stock owners become destroyed before their normal unsubscribe path runs.

Some fixes are direct lifecycle patches. For example, `ModuleRobotArmScanner` hides `ModuleDeployablePart.OnDestroy()`, so the base `onVesselChange` unsubscribe can be skipped unless patched directly.

Tracking-station cleanup also sweeps vessel `OrbitRenderer.onVesselIconClicked` callbacks, because `SpaceTracking` registers per-vessel icon callbacks outside the central `GameEvents` list.

## Validation

Use KSPCF memory leak logging and compare the exported summary before and after a run.

Expected improvement is that covered callback owners either disappear from the KSPCF summary or drop significantly. If a covered owner still appears, check `KSP.log` for:

```text
[NoMoreLeaks] Removed
```

If that line never appears during gameplay, the sweeper did not catch any destroyed owners before KSPCF did.

## Observed Leak History

Based on archived KSPCF summaries from `~/.local/share/Trash/files`, the repo `leak-sums/` folder, and the `memleaks/nomoreleaks-off` control run, the stock leak trend has been consistent enough to justify this mod.

Recurring stock leak classes from the older trash reports:

- `ModuleInventoryPart` inventory callbacks were the most persistent remaining stock issue: `866` removals each for `onPartActionUICreate` and `onModuleInventoryChanged` across `17` archived summary files.
- `ModuleCargoPart | OnEVAConstructionMode` was the worst early stock leak: `4482` removals across `5` archived summary files.
- `ModuleDeployableSolarPanel | onVesselChange` appeared repeatedly: `232` removals across `10` archived summary files.
- `OverlayGenerator`, `MapView`, `RunwayCollisionHandler`, `VesselAutopilotUI`, and `NavBallToggle` showed up in many early and mid-May runs, but usually at much lower counts than the inventory leaks.
- `CommNetVessel | onPlanetariumTargetChange` stayed near `1` per run and `RealAntennas` entries were intentionally left out of this mod's scope.

Short timeline:

| Date | Source | Main stock leaks seen | Read |
| --- | --- | --- | --- |
| 2026-05-16 | archived trash baseline | `ModuleCargoPart` `2124`, `ModuleDeployableSolarPanel` `73`, `ModuleInventoryPart` `63/63`, plus overlay/map/autopilot/navball leaks | very noisy stock baseline |
| 2026-05-17 | archived trash runs | inventory leaks dominated many runs at `15` to `59`; some runs still showed ground science, overlay/map, control surface, and EVA editor leaks | partial improvement, not stable |
| 2026-05-18 | archived trash runs | inventory leaks spiked again up to `221/221`, with `OnPartPurchased` and `onEditorPartEvent` at `95` each; `UIPartActionInventorySlot` and `ModuleRobotArmScanner` also appeared | clear regression around inventory teardown |
| 2026-05-20 | repo `leak-sums/5-20-26` | only `VesselAutopilotUI`, `CommNetVessel`, and `RealAntennas` remained | best observed stock result so far |
| 2026-05-21 to 2026-05-23 | repo `leak-sums/` | `ModuleInventoryPart` returned strongly (`28` to `85`), `UIPartActionInventorySlot` reached `50/50`, `VesselAutopilotUI` stayed present, and `SpaceTracking` / `InternalNavBall` showed up in the worst run on `2026-05-23` | current focus area that drove the newer sweeper changes |

Control comparison:

- With `NoMoreLeaks` off, the `memleaks/nomoreleaks-off` summary still showed broad stock leaks on `2026-05-24`, led by `ModuleGroundExperiment`, `ModuleInventoryPart`, `ModuleGroundPart`, `ModuleDeployableSolarPanel`, `OverlayGenerator`, and `VesselAutopilotUI`.
- That control run matters because it shows the goal of this mod is still valid: the stock game is retaining destroyed callback owners on its own, and the remaining work is about narrowing that list, not proving the problem exists.

## Build Notes

The project targets old .NET Framework for KSP compatibility. On Linux, the DLL can be built with Mono `mcs` against the local KSP assemblies.

Generated binaries under `GameData/NoMoreLeaks/Plugins/` and build intermediates under `bin/` / `obj/` are ignored by git.
