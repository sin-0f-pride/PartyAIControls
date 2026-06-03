using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PartyAIControls.HarmonyPatches
{
    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "GetApproximateVolunteersCanBeRecruitedDataFromSettlement")]
    internal class AiVisitSettlementBehaviorPatches
    {
        internal static void Postfix(ref ValueTuple<int,float> __result, Hero hero, Settlement settlement)
        {
            if (!SubModule.PartySettingsManager.IsHeroManageable(hero) || hero.PartyBelongedTo == null || !hero.PartyBelongedTo.LeaderHero.Equals(hero))
            {
                return;
            }

            MobileParty mobileParty = hero.PartyBelongedTo;
            PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(hero);

            if (!heroSettings.AllowRecruitment)
            {
                __result.Item1 = 0;
                return;
            }

            // if we're going to convert the troop anyway, it doesn't matter
            if (SubModule.PartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
            {
                return;
            }

            PartyCompositionObect comp = SubModule.PartyTroopRecruiter.GetPartyComposition(mobileParty.Party, heroSettings);

            __result.Item1 = 0;
            foreach (Hero notable in settlement.Notables)
            {
                int max = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(mobileParty.IsGarrison ? mobileParty.Party.Owner : mobileParty.LeaderHero, notable);
                for (int i = 0; i <= max && i < notable.VolunteerTypes.Length; i++)
                {
                    CharacterObject troop = notable.VolunteerTypes[i];
                    if (troop != null && SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, troop, mobileParty.Party))
                    {
                        __result.Item1++;
                    }
                }
            }
        }
    }
}
