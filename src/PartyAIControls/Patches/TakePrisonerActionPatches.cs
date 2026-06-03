using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace PartyAIControls.Patches
{
    [HarmonyPatch(typeof(TakePrisonerAction), "ApplyInternal")]
    internal class TakePrisonerActionPatches
    {
        private static void Prefix(PartyBase capturerParty, Hero prisonerCharacter, ref bool isEventCalled)
        {
            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(capturerParty?.LeaderHero))
            {
                return;
            }

            if (!SubModule.PartyAIClanPartySettingsManager.Settings(capturerParty?.LeaderHero).AllowLordPrisoners)
            {
                isEventCalled = false;
            }
        }

        private static void Postfix(PartyBase capturerParty, Hero prisonerCharacter, bool isEventCalled)
        {
            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(capturerParty?.LeaderHero))
            {
                return;
            }

            if (!SubModule.PartyAIClanPartySettingsManager.Settings(capturerParty?.LeaderHero).AllowLordPrisoners)
            {
                EndCaptivityAction.ApplyByReleasedAfterBattle(prisonerCharacter);
            }
        }
    }
}
