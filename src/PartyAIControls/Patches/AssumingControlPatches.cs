using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static PartyAIControls.PAICustomOrder;

namespace PartyAIControls.Patches
{
    [HarmonyPatch]
    internal class AssumingControlPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveGoToPoint")]
        private static void SetMoveGoToPoint(MobileParty __instance, CampaignVec2 point)
        {
            if (!Input.IsKeyDown(SubModule.PartyAIClanPartySettingsManager.CommandPartiesKey) || __instance != MobileParty.MainParty) { return; }

            foreach (MobileParty controlling in SubModule.PartyAIThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null) { continue; }
                if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(controlling.LeaderHero)) { continue; }
                if (controlling.MapEvent != null) { continue; }
                if (controlling.Position.Distance(MobileParty.MainParty.Position) > MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(new(new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly").SetTextVariable("NAME", controlling.Name).ToString(), Colors.Magenta));
                    continue;
                }
                SetPartyAiAction.GetActionForEscortingParty(controlling, MobileParty.MainParty, controlling.NavigationCapability, false, controlling.IsTargetingPort);
                controlling.Ai.SetDoNotMakeNewDecisions(true);
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();
                settings.SetOrder(new(MobileParty.MainParty, OrderType.EscortParty));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveEngageParty")]
        private static void SetMoveEngageParty(MobilePartyAi __instance, MobileParty party)
        {
            if (!Input.IsKeyDown(SubModule.PartyAIClanPartySettingsManager.CommandPartiesKey) || __instance != MobileParty.MainParty.Ai) { return; }

            foreach (MobileParty controlling in SubModule.PartyAIThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null) { continue; }
                if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(controlling.LeaderHero)) { continue; }
                if (controlling.MapEvent != null) { continue; }
                if (controlling.Position.Distance(MobileParty.MainParty.Position) > MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(new(new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly").SetTextVariable("NAME", controlling.Name).ToString(), Colors.Magenta));
                    continue;
                }

                controlling.Ai.SetDoNotMakeNewDecisions(true);
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();
                if (FactionManager.IsAtWarAgainstFaction(party.MapFaction, controlling.MapFaction))
                {
                    SetPartyAiAction.GetActionForEngagingParty(controlling, party, controlling.NavigationCapability, false);
                    settings.SetOrder(new(party, OrderType.AttackParty));
                }
                else
                {
                    SetPartyAiAction.GetActionForEscortingParty(controlling, party, controlling.NavigationCapability, false, controlling.IsTargetingPort);
                    settings.SetOrder(new(party, OrderType.EscortParty));
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveEscortParty")]
        private static void SetMoveEscortParty(MobilePartyAi __instance, MobileParty mobileParty) => SetMoveEngageParty(__instance, mobileParty);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveGoToSettlement")]
        private static void SetMoveGoToSettlement(MobilePartyAi __instance, Settlement settlement)
        {
            if (!Input.IsKeyDown(SubModule.PartyAIClanPartySettingsManager.CommandPartiesKey) || __instance != MobileParty.MainParty.Ai) { return; }

            foreach (MobileParty controlling in SubModule.PartyAIThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null) { continue; }
                if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(controlling.LeaderHero)) { continue; }
                if (controlling.MapEvent != null) { continue; }
                if (controlling.Position.Distance(MobileParty.MainParty.Position) > MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(new(new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly").SetTextVariable("NAME", controlling.Name).ToString(), Colors.Magenta));
                    continue;
                }

                controlling.Ai.SetDoNotMakeNewDecisions(true);
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();
                if (FactionManager.IsAtWarAgainstFaction(settlement.MapFaction, controlling.MapFaction))
                {
                    SetPartyAiAction.GetActionForBesiegingSettlement(controlling, settlement, controlling.NavigationCapability, false);
                    settings.SetOrder(new(settlement, OrderType.BesiegeSettlement));
                }
                else
                {
                    if (settlement.IsUnderSiege)
                    {
                        SetPartyAiAction.GetActionForDefendingSettlement(controlling, settlement, controlling.NavigationCapability, false, controlling.IsTargetingPort);
                        settings.SetOrder(new(settlement, OrderType.DefendSettlement));
                    }
                    else
                    {
                        SetPartyAiAction.GetActionForVisitingSettlement(controlling, settlement, controlling.NavigationCapability, false, controlling.IsTargetingPort);
                        settings.SetOrder(new(settlement, OrderType.DefendSettlement));
                    }
                }
            }
        }
    }
}
