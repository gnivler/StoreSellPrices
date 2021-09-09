using System;
using System.Collections.Generic;
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
        private const string PluginVersion = "1.1.0";

        private void Awake()
        {
            Harmony harmony = new("ca.gnivler.d2e.StoreSellPrices");
            harmony.PatchAll(typeof(Patches));
            Log("StoreSellPrices Startup");
        }

        internal static void Log(object input)
        {
            //File.AppendAllText("log.txt",$"{input ?? "null"}\n");
        }
        
        internal static void PrintInstructionsAroundInsertion(List<CodeInstruction> codes, int insertPoint, int insertSize, int adjacentNum = 5)
        {
            Log($"Inserting {insertSize} at {insertPoint}.");

            // in case insertPoint is near the start of the method's IL
            var adjustedAdjacent = codes.Count - adjacentNum >= 0 ? adjacentNum : Math.Max(0, codes.Count - adjacentNum);
            for (var i = 0; i < adjustedAdjacent; i++)
            {
                // codes[266 - 5 + 0].opcode
                // codes[266 - 5 + 4].opcode
                Log($"{codes[insertPoint - adjustedAdjacent + i].opcode,-10}{codes[insertPoint - adjustedAdjacent + i].operand}");
            }

            for (var i = 0; i < insertSize; i++)
            {
                Log($"{codes[insertPoint + i].opcode,-10}{codes[insertPoint + i].operand}");
            }

            // in case insertPoint is near the end of the method's IL
            adjustedAdjacent = insertPoint + adjacentNum <= codes.Count ? adjacentNum : Math.Max(codes.Count, adjustedAdjacent);
            for (var i = 0; i < adjustedAdjacent; i++)
            {
                // 266 + 2 - 5 + 0
                // 266 + 2 - 5 + 4
                Log($"{codes[insertPoint + insertSize + adjustedAdjacent + i].opcode,-10}{codes[insertPoint + insertSize + adjustedAdjacent + i].operand}");
            }
        }
    }
}
