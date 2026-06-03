using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace PartyAIControls.Patches
{
    [HarmonyPatch]
    internal class MobilePartyAiPatches
    {
        //TODO: GetNavalPatrolBehavior
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobilePartyAi), "GetLandPatrolBehavior")]
        private static void Postfix(ref AiBehavior patrolBehavior, ref CampaignVec2 patrolTargetPoint, ref CampaignVec2 patrollingCenterPoint, bool forceUpdate, MobileParty ____mobileParty)
        {
            if (!SubModule.PartyAIClanPartySettingsManager.AggressivePatrols) { return; }
            if (____mobileParty?.MapFaction == null || ____mobileParty.LeaderHero == null) { return; }
            if (!____mobileParty.MapFaction.IsKingdomFaction && ____mobileParty.ActualClan != Clan.PlayerClan) { return; }

            LocatableSearchData<MobileParty> data = MobileParty.StartFindingLocatablesAroundPosition(____mobileParty.Position.ToVec2(), 15f);
            for (MobileParty mobilePartySearch = MobileParty.FindNextLocatable(ref data); mobilePartySearch != null; mobilePartySearch = MobileParty.FindNextLocatable(ref data))
            {
                if (mobilePartySearch?.Position == null) { continue; }
                if (!mobilePartySearch.IsActive || mobilePartySearch.IsMainParty || mobilePartySearch.IsDisbanding) { continue; }

                IFaction mapFaction = mobilePartySearch.MapFaction;
                if (mobilePartySearch.CurrentSettlement != null) { continue; }
                if (mapFaction == null || !mapFaction.IsAtWarWith(____mobileParty.MapFaction) || (mobilePartySearch.Army != null && mobilePartySearch != mobilePartySearch.Army.LeaderParty))
                {
                    continue;
                }

                float totalStrength = ____mobileParty.GetTotalLandStrengthWithFollowers();
                totalStrength = totalStrength > 0 ? totalStrength : 1;
                float strengthRatio = mobilePartySearch.GetTotalLandStrengthWithFollowers() / totalStrength;
                if (strengthRatio > 0.8f || strengthRatio < 0.05f) { continue; }
                if (mobilePartySearch.Speed > ____mobileParty.Speed) { continue; }
                if (mobilePartySearch.Position.Distance(patrolTargetPoint) > 100f) { continue; }

                SetPartyAiAction.GetActionForEngagingParty(____mobileParty, mobilePartySearch, ____mobileParty.NavigationCapability, false);
                return;
            }
        }
    }
}
