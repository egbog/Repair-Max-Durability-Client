/* LICENSE:
 * MIT
 * 
 * AUTHOR:
 * egbog
 * */

using BepInEx;
using RepairMaxDurabilityClient.Patches;

namespace RepairMaxDurabilityClient {
    [BepInPlugin("com.egbog.maxdura", "MaxDurability", "2.0.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            new RepairMaxDurability().Enable();
            new RepairWindowPatch().Enable();
        }
    }
}