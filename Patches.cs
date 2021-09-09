using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Devdog.General;
using Devdog.InventoryPro;
using HarmonyLib;
using static StoreSellPrices.Mod;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace StoreSellPrices
{
    public static class Patches
    {
        // rewrite
        [HarmonyPatch(typeof(InfoBoxUI), "SellTip")]
        public static bool Prefix(InfoBoxUI __instance, InventoryItemBase ___currentItem, ref bool __runOriginal)
        {
            __runOriginal = false;
            // resize array of 3 into 6
            if (__instance.m_SellInfoObj.Length == 3)
            {
                Array.Resize(ref __instance.m_SellInfoObj, 6);
                for (var i = 0; i < 3; i++)
                {
                    __instance.m_SellInfoObj[3 + i] = Object.Instantiate(__instance.m_SellInfoObj[0], __instance.m_SellObj, false);
                    __instance.m_SellInfoObj[3 + i].gameObject.SetActive(false);
                }
            }

            foreach (var commonObj in __instance.m_SellInfoObj)
            {
                commonObj.gameObject.SetActive(false);
            }

            var myID = ___currentItem.MyID;
            var itemSellInfo = UnitySingleton<BigTable>.Instance.m_ItemSellInfo;
            if (itemSellInfo.TryGetValue(myID, out var itemTradeInfos))
            {
                itemTradeInfos = itemTradeInfos.OrderBy(i => i.m_Price).ToList();
                for (var i = 0; i < __instance.m_SellInfoObj.Length && i < itemTradeInfos.Count; i++)
                {
                    var tradeInfo = itemTradeInfos[i];
                    Log($"{___currentItem.name} {__instance.m_SellInfoObj[i].m_Text[0].text,-25} {__instance.m_SellInfoObj[i].m_Text[1].text}");
                    __instance.m_SellInfoObj[i].gameObject.SetActive(true);
                    __instance.m_SellInfoObj[i].m_Text[0].text = Town.m_Cities[tradeInfo.m_CityID].TownName;
                    __instance.m_SellInfoObj[i].m_Text[1].text = GlobalManager.FormatGoldValue(tradeInfo.m_Price);
                }
            }
            else
            {
                __instance.m_SellInfoObj[0].m_Text[0].text = LanguageManager.Instance.GetKey("NoTradeInfo");
                __instance.m_SellInfoObj[0].m_Text[1].text = string.Empty;
                __instance.m_SellInfoObj[0].gameObject.SetActive(value: true);
            }

            return false;
        }

        // TODO  add items to price storage when crafting 
        // rewrite - maybe keeps more than 3 entries
        [HarmonyPatch(typeof(BigTable), "AddItemSellInfo")]
        public static bool Prefix(BigTable __instance, string itemid, string cityid, float price, ref bool __runOriginal)
        {
            __runOriginal = false;
            if (!__instance.m_ItemSellInfo.ContainsKey(itemid))
            {
                var itemTradeInfo = new ItemTradeInfo();
                itemTradeInfo.m_CityID = cityid;
                itemTradeInfo.m_Price = price;
                __instance.m_ItemSellInfo[itemid] = new List<ItemTradeInfo>();
                __instance.m_ItemSellInfo[itemid].Add(itemTradeInfo);
                return false;
            }

            var num = 0;
            while (num < __instance.m_ItemSellInfo[itemid].Count)
            {
                if (__instance.m_ItemSellInfo[itemid][num] == null)
                {
                    __instance.m_ItemSellInfo[itemid].RemoveAt(num);
                    continue;
                }

                if (__instance.m_ItemSellInfo[itemid][num].m_CityID == cityid)
                {
                    __instance.m_ItemSellInfo[itemid].RemoveAt(num);
                    break;
                }

                num++;
            }

            var itemTradeInfo2 = new ItemTradeInfo();
            itemTradeInfo2.m_CityID = cityid;
            itemTradeInfo2.m_Price = price;
            __instance.m_ItemSellInfo[itemid].Add(itemTradeInfo2);
            return false;
        }

        // resize limited List
        [HarmonyPatch(typeof(InventoryItemBase), "GetInfo")]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // strategy
            // change new List<>(3) into an unspecified size new List<>(), so it's a different ctor
            var codes = new List<CodeInstruction>(instructions);
            var intCtor = AccessTools.Constructor(typeof(List<ItemInfoRow>), new[] { typeof(int) });
            var defaultCtor = AccessTools.Constructor(typeof(List<ItemInfoRow>), new Type[] { });

            for (var index = 0; index < codes.Count; index++)
            {
                var code = codes[index];
                if (code.opcode == OpCodes.Ldc_I4_3
                    && ReferenceEquals(codes[index + 1]?.operand, intCtor))
                {
                    Log("InventoryItemBaseGetInfoTranspiler");
                    code.opcode = OpCodes.Nop;
                    codes[index + 1].operand = defaultCtor;
                    break;
                }
            }

            return codes.AsEnumerable();
        }

        // populate DB with sell info
        [HarmonyPatch(typeof(TiggerStop), "Enter")]
        public static void Postfix(TiggerStop __instance)
        {
            Town.CurCity = __instance;
            AccessTools.Method(typeof(TiggerStop), "RefreshItemPrice").Invoke(__instance, new object[] { });
            foreach (var inventoryItemBase in __instance.container.items)
            {
                UnitySingleton<BigTable>.Instance.ResetPrice(inventoryItemBase);
                UnitySingleton<BigTable>.Instance.AddItemSellInfo(inventoryItemBase.MyID, __instance.TownID, inventoryItemBase.sellPrice.amount);
            }

            var collection = PlayerManager.instance.currentPlayer.inventoryPlayer.inventoryCollections[0];
            foreach (var collectionItem in collection)
            {
                if (collectionItem.item is null
                    || !collectionItem.item.isSellable)
                {
                    continue;
                }

                UnitySingleton<BigTable>.Instance.ResetPrice(collectionItem.item);
                UnitySingleton<BigTable>.Instance.AddItemSellInfo(collectionItem.item.MyID, __instance.TownID, collectionItem.item.sellPrice.amount);
            }
        }
    }
}
