using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Localization;

namespace PartyAIControls.Patches
{
    [HarmonyPatch(typeof(MapBarVM), "UpdateCanGatherArmyAndReason")]
    internal class MapBarVMPatches
    {
        private static void Postfix(MapBarVM __instance)
        {
            if (SubModule.BannerKings)
            {
                if (Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.IsUnderMercenaryService)
                {
                    __instance.GatherArmyHint = new(new("{=PAIoLtvzpKU}PartyAIControls: Feature to enable armies without being a Vassal is not compatible with BannerKings."));
                }
                return;
            }

            IFaction mapFaction = Hero.MainHero.MapFaction;
            if (mapFaction != null && !mapFaction.IsKingdomFaction)
            {
                __instance.CanGatherArmy = true;
                __instance.GatherArmyHint.HintText = TextObject.GetEmpty();
            }
            else if (Clan.PlayerClan.IsUnderMercenaryService)
            {
                __instance.CanGatherArmy = true;
                __instance.GatherArmyHint.HintText = TextObject.GetEmpty();
            }
        }
    }
}
