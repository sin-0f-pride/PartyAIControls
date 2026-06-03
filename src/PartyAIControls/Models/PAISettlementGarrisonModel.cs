using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PartyAIControls.Models
{
    internal class PAISettlementGarrisonModel : SettlementGarrisonModel
    {
        readonly SettlementGarrisonModel _model;

        public PAISettlementGarrisonModel(SettlementGarrisonModel previousModel)
        {
            _model = previousModel;
            _model ??= new DefaultSettlementGarrisonModel();
        }

        public override int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement)
        {
            int result = _model.FindNumberOfTroopsToLeaveToGarrison(mobileParty, settlement);

            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
            {
                return result;
            }

            PartyAIClanPartySettings heroSettings = SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero);

            if (!heroSettings.AllowDonateTroops)
            {
                result = 0;
            }

            return result;
        }

        public override int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float idealGarrisonStrengthPerWalledCenter = 0)
        {
            int result = _model.FindNumberOfTroopsToTakeFromGarrison(mobileParty, settlement, idealGarrisonStrengthPerWalledCenter);

            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
            {
                return result;
            }

            PartyAIClanPartySettings heroSettings = SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero);

            if (!heroSettings.AllowTakeTroopsFromSettlement)
            {
                result = 0;
            }

            if (!heroSettings.AllowRecruitment)
            {
                result = 0;
            }

            return result;
        }

        public override int GetMaximumDailyAutoRecruitmentCount(Town town) => _model.GetMaximumDailyAutoRecruitmentCount(town);

        public override float GetMaximumDailyRepairAmount(Settlement settlement) => _model.GetMaximumDailyRepairAmount(settlement);

        public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false) => _model.CalculateBaseGarrisonChange(settlement, includeDescriptions);
    }
}
