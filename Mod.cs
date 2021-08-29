using System.IO;
using BepInEx;
using HarmonyLib;

namespace StoreSellPrices
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        private const string PluginGUID = "ca.gnivler.d2e.StoreSellPrices";
        private const string PluginName = "StoreSellPrices";
        private const string PluginVersion = "1.0.0";

        private void Awake()
        {
            Harmony harmony = new("ca.gnivler.d2e.StoreSellPrices");
            harmony.PatchAll();
            //Log("StoreSellPrices Startup");
        }

        internal static void Log(object input)
        {
            File.AppendAllText("log.txt",$"{input ?? "null"}\n");
        }
    }
}
