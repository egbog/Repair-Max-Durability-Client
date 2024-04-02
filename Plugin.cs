/* LICENSE:
 * MIT
 * 
 * AUTHOR:
 * egbog
 * */

using BepInEx;
using System.Reflection;

namespace MaxDura
{
    [BepInPlugin("com.egbog.maxdura", "MaxDurability", "1.0.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public class Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            new MaxDura.RepairMaxDurability().Enable();
        }
    }
}