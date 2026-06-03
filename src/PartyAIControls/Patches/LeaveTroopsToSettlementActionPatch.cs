using HarmonyLib;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PartyAIControls.HarmonyPatches
{
    [HarmonyPatch]
    internal class LeaveTroopsToSettlementActionPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GarrisonTroopsCampaignBehavior), "LeaveTroopsToGarrison")]
        private static bool Prefix(MobileParty mobileParty, Settlement settlement, int numberOfTroopsToLeave, bool archersAreHighPriority)
        {
            if (mobileParty.LeaderHero == null) { return true; }
            if (numberOfTroopsToLeave > 0)
            {
                return SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero).AllowDonateTroops;
            }
            else
            {
                return SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero).AllowTakeTroopsFromSettlement;
            }
        }
    }
}
