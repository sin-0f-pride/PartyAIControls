using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior;

namespace PartyAIControls.Patches
{
    internal class RecruitmentCampaignBehaviorPatches
    {
        [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyInternal")]
        internal class ApplyInternal
        {
            private static bool Prefix(MobileParty side1Party, Settlement settlement, Hero individual, CharacterObject troop, int number, int bitCode, RecruitingDetail detail)
            {
                if (!SubModule.PartyAIClanPartySettingsManager.IsManageable(side1Party.LeaderHero))
                {
                    return true;
                }

                PartyAIClanPartySettings heroSettings = SubModule.PartyAIClanPartySettingsManager.Settings(side1Party.LeaderHero);

                if (!heroSettings.AllowRecruitment)
                {
                    return false;
                }

                // if we're going to convert the troop anyway, it doesn't matter
                if (SubModule.PartyAIClanPartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
                {
                    return true;
                }

                PartyCompositionObect comp = SubModule.PartyAITroopRecruiter.GetPartyComposition(side1Party.Party, heroSettings);
                if (!SubModule.PartyAITroopRecruiter.ShouldRecruit(comp, heroSettings, troop, side1Party.Party))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
