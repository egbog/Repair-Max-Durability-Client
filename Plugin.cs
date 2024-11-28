/* LICENSE:
 * MIT
 * 
 * AUTHOR:
 * egbog
 * */

using BepInEx;

namespace MaxDura
{
    [BepInPlugin("com.egbog.maxdura", "MaxDurability", "1.3.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            new MaxDura.RepairMaxDurability().Enable();
			new MaxDura.RepairWindowPatch().Enable();
        }
    }
}