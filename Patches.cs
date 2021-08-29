using Devdog.General;
using HarmonyLib;
using Tiled2Unity;
using static StoreSellPrices.Mod;

namespace StoreSellPrices
{
    public static class Patches
    {
        [HarmonyPatch(typeof(TiggerStop), "Enter")]
        public static class TiggerStopEnterPatch
        {
            public static void Postfix(TiggerStop __instance)
            {
                Town.CurCity = __instance;
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
}
