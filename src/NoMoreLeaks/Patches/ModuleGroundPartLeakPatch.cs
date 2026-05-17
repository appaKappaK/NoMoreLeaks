using HarmonyLib;

namespace NoMoreLeaks.Patches
{
    [HarmonyPatch(typeof(ModuleGroundPart))]
    internal static class ModuleGroundPartLeakPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPrefix(ModuleGroundPart __instance)
        {
            Cleanup(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnDestroy")]
        private static void OnDestroyPostfix(ModuleGroundPart __instance)
        {
            Cleanup(__instance);
        }

        internal static void Cleanup(ModuleGroundPart instance)
        {
            if (instance == null) return;

            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIShown, instance, typeof(ModuleGroundPart), "OnPartActionUIShown");
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIDismiss, instance, typeof(ModuleGroundPart), "OnPartActionUIDismiss");
            EventCleanup.RemoveGameEvent(GameEvents.onVesselChange, instance, typeof(ModuleGroundPart), "OnVesselChange");
            EventCleanup.RemoveGameEvent(GameEvents.onPartWillDie, instance, typeof(ModuleGroundPart), "OnPartWillDie");
            EventCleanup.RemoveGameEvent(GameEvents.onSceneConfirmExit, instance, typeof(ModuleGroundPart), "OnLeavingScene");
            EventCleanup.RemoveGameEvent(GameEvents.OnAnimationGroupRetractComplete, instance, typeof(ModuleGroundPart), "OnRetractCompleted");
            EventCleanup.RemoveGameEvent(GameEvents.OnEVAConstructionMode, instance, typeof(ModuleCargoPart), "OnEVAConstructionMode");

            if (instance is ModuleGroundSciencePart sciencePart)
                CleanupSciencePart(sciencePart);

            if (instance is ModuleGroundExperiment experiment)
                CleanupExperiment(experiment);

            if (instance is ModuleGroundExpControl control)
                CleanupControl(control);
        }

        private static void CleanupSciencePart(ModuleGroundSciencePart instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIShown, instance, typeof(ModuleGroundSciencePart), "OnPartActionUIOpened");
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIDismiss, instance, typeof(ModuleGroundSciencePart), "OnPartActionUIDismiss");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceDeregisterCluster, instance, typeof(ModuleGroundSciencePart), "OnGroundScienceDeregisterCluster");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceClusterUpdated, instance, typeof(ModuleGroundSciencePart), "OnGroundScienceClusterUpdated");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceClusterPowerStateChanged, instance, typeof(ModuleGroundSciencePart), "OnGroundScienceClusterPowerStateChanged");
        }

        private static void CleanupExperiment(ModuleGroundExperiment instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceGenerated, instance, typeof(ModuleGroundExperiment), "OnGroundScienceGenerated");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceTransmitted, instance, typeof(ModuleGroundExperiment), "OnGroundScienceTransmitted");
        }

        private static void CleanupControl(ModuleGroundExpControl instance)
        {
            EventCleanup.RemoveGameEvent(GameEvents.onGroundSciencePartDeployed, instance, typeof(ModuleGroundExpControl), "OnGroundSciencePartDeployed");
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIShown, instance, typeof(ModuleGroundExpControl), "OnPartActionUIOpened");
            EventCleanup.RemoveGameEvent(GameEvents.onPartActionUIDismiss, instance, typeof(ModuleGroundExpControl), "OnPartActionUIDismiss");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceClusterUpdated, instance, typeof(ModuleGroundExpControl), "OnGroundScienceClusterUpdated");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundSciencePartEnabledStateChanged, instance, typeof(ModuleGroundExpControl), "OnGroundSciencePartEnabledStateChanged");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundSciencePartRemoved, instance, typeof(ModuleGroundExpControl), "OnGroundScienceModuleRemoved");
            EventCleanup.RemoveGameEvent(GameEvents.onGroundScienceClusterPowerStateChanged, instance, typeof(ModuleGroundExpControl), "OnGroundScienceClusterPowerStateChanged");
        }
    }

    [HarmonyPatch(typeof(ModuleGroundSciencePart), "OnDestroy")]
    internal static class ModuleGroundSciencePartDestroyLeakPatch
    {
        private static void Prefix(ModuleGroundSciencePart __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }

        private static void Postfix(ModuleGroundSciencePart __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }
    }

    [HarmonyPatch(typeof(ModuleGroundExperiment), "OnDestroy")]
    internal static class ModuleGroundExperimentDestroyLeakPatch
    {
        private static void Prefix(ModuleGroundExperiment __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }

        private static void Postfix(ModuleGroundExperiment __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }
    }

    [HarmonyPatch(typeof(ModuleGroundExpControl), "OnDestroy")]
    internal static class ModuleGroundExpControlDestroyLeakPatch
    {
        private static void Prefix(ModuleGroundExpControl __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }

        private static void Postfix(ModuleGroundExpControl __instance)
        {
            ModuleGroundPartLeakPatch.Cleanup(__instance);
        }
    }
}
