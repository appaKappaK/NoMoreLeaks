using HarmonyLib;
using UnityEngine;

namespace NoMoreLeaks
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public sealed class Startup : MonoBehaviour
    {
        private const string HarmonyId = "matth.nomoreleaks";

        private void Awake()
        {
            new Harmony(HarmonyId).PatchAll();
            Debug.Log("[NoMoreLeaks] Harmony patches applied");
        }
    }
}
