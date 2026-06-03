using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.Core;

namespace PartyAIControls.Patches
{
    [HarmonyPatch(typeof(ArmyManagementVM), "ExecuteDone")]
    public static class ArmyManagementVMPatches
    {
        public static class RefreshValuesPatch
        {
            public static void Postfix(ArmyManagementVM __instance)
            {
                if (!SubModule.BannerKings)
                {

                }
                if (Clan.PlayerClan.IsUnderMercenaryService && __instance.PartyList != null)
                {
                    List<ArmyManagementItemVM> parties = __instance.PartyList.ToList();
                    __instance.PartyList.Clear();

                    foreach (ArmyManagementItemVM party in parties)
                    {
                        if (party.Clan == Clan.PlayerClan)
                        {
                            __instance.PartyList.Add(party);
                        }
                    }

                    __instance.OnPropertyChanged("PartyList");
                }
            }
        }

        [HarmonyPatch(typeof(ArmyManagementVM), "RefreshValues")]
        public static class ExecuteDonePatch
        {
            public static void Postfix(ArmyManagementVM __instance)
            {
                if (!SubModule.BannerKings)
                {
                    return;
                }
                if (__instance.PartiesInCart.Count > 1)
                {
                    if (MobileParty.MainParty.Army == null)
                    {
                        Hero armyLeader = Hero.MainHero;

                        if (armyLeader?.PartyBelongedTo.LeaderHero != null)
                        {
                            Army army = new Army(null, armyLeader.PartyBelongedTo, Army.ArmyTypes.Patrolling);
                            army.Gather(Hero.MainHero.HomeSettlement);
                            CampaignEventDispatcher.Instance.OnArmyCreated(army);
                        }
                        if (armyLeader == Hero.MainHero)
                        {
                            (Game.Current.GameStateManager.GameStates.Single((GameState S) => S is MapState) as MapState)?.OnArmyCreated(MobileParty.MainParty);
                        }

                    }
                    foreach (ArmyManagementItemVM item in __instance.PartiesInCart)
                    {
                        if (item.Party != MobileParty.MainParty)
                        {
                            item.Party.Army = MobileParty.MainParty.Army;
                            SetPartyAiAction.GetActionForEscortingParty(item.Party, MobileParty.MainParty, item.Party.NavigationCapability, false, item.Party.IsTargetingPort);
                        }
                    }
                }
            }
        }
    }
}
