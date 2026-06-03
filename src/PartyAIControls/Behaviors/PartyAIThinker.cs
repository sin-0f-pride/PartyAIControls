using Helpers;
using PartyAIControls.HarmonyPatches;
using PartyAIControls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using static PartyAIControls.PAICustomOrder;
using static TaleWorlds.CampaignSystem.Party.MobileParty;

namespace PartyAIControls.CampaignBehaviors
{
    public class PAISettlementVisitLog
    {
        [SaveableProperty(1)] public Settlement Settlement { get; private set; }
        [SaveableProperty(2)] public CampaignTime Visited { get; private set; }
        [SaveableProperty(3)] public MobileParty Party { get; private set; }
        public PAISettlementVisitLog(Settlement settlement, CampaignTime visited, MobileParty party)
        {
            Settlement = settlement;
            Visited = visited;
            Party = party;
        }
    }

    internal class PartyAIThinker : CampaignBehaviorBase
    {
        private List<MobileParty> _assumingDirectControl = new();
        private List<PAISettlementVisitLog> _recentlyRecruitedFromSettlements = new();

        internal MBReadOnlyList<MobileParty> AssumingDirectControl { get => _assumingDirectControl.ToMBList(); }

        internal void ClearAssumingDirectControl() => _assumingDirectControl.Clear();
        internal void AddToAssumingDirectControl(MobileParty party) => _assumingDirectControl.Add(party);

        public override void RegisterEvents()
        {
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(OnMobilePartyDestroyed));
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(OnHeroPrisonerTaken));
            CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, new Action<MobileParty>(OnPartyJoinedArmy));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(OnSettlementOwnerChanged));
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(OnHourlyTickParty));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(ImplementAutoCreateClanParties));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTick));
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new(OnMobilePartyCreated));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new(OnSettlementEntered));
        }

        private void OnDailyTick()
        {
            _recentlyRecruitedFromSettlements.RemoveAll(l => l.Visited.ElapsedDaysUntilNow > 10f);
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(party?.LeaderHero))
            {
                return;
            }

            PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(party.LeaderHero);
            if (settings.HasActiveOrder && (settings.Order.Behavior == OrderType.RecruitFromTemplate || settings.Order.Behavior == OrderType.VisitSettlement))
            {
                if (settlement == settings.Order.Target)
                {
                    if (settings.Order.Behavior == OrderType.VisitSettlement)
                    {
                        settings.ClearOrder();
                    }
                    else
                    {
                        settings.Order.Target = null;
                    }
                }
            }
        }

        private void ImplementAutoCreateClanParties()
        {
            if (!SubModule.PartyAIClanPartySettingsManager.AutoCreateClanParties)
            {
                return;
            }

            if (SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesMax > 0 && ActiveClanParties(Clan.PlayerClan).Count() >= SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesMax)
            {
                return;
            }

            do
            {
                ClanPartiesVM stockVM = new(() => { }, null, () => { }, (i) => { });

                if (!stockVM.CanCreateNewParty)
                {
                    return;
                }

                IEnumerable<Hero> eligibleLeaders = Clan.PlayerClan.Heroes.Where((Hero h) => !h.IsDisabled).Union(Clan.PlayerClan.Companions).Where(h =>
                  h.IsActive && !h.IsReleased && !h.IsFugitive && !h.IsPrisoner && !h.IsChild && h != Hero.MainHero && h.CanLeadParty() && !h.IsPartyLeader && h.GovernorOf == null && h.PartyBelongedTo == null && (!h.CurrentSettlement?.IsUnderSiege ?? true)
                );

                if (SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesRoster.Count > 0)
                {
                    eligibleLeaders = eligibleLeaders.Where(h => SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesRoster.Contains(h));
                }

                if (eligibleLeaders.Count() == 0)
                {
                    return;
                }

                Hero leader = TaleWorlds.Core.Extensions.GetRandomElementInefficiently(eligibleLeaders);
                Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(leader.PartyBelongedTo, leader.PartyBelongedTo.NavigationCapability, s => true);
                MobilePartyHelper.CreateNewClanMobileParty(leader, Clan.PlayerClan);
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=PAIJPxU5978}{HERO} has created a new party near {SETTLEMENT}").SetTextVariable("HERO", leader.Name).SetTextVariable("SETTLEMENT", settlement?.Name).ToString(), Colors.Gray));

                if (SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesMax > 0 && ActiveClanParties(Clan.PlayerClan).Count() >= SubModule.PartyAIClanPartySettingsManager.AutoCreateClanPartiesMax)
                {
                    break;
                }
            } while (Clan.PlayerClan.WarPartyComponents.Count < Clan.PlayerClan.WarPartyLimit);
        }

        private void OnHourlyTickParty(MobileParty party)
        {
            if (party?.LeaderHero == null) { return; }
            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(party.LeaderHero))
            {
                if (party.Ai.DoNotMakeNewDecisions && party.DefaultBehavior == AiBehavior.Hold && party.IsLordParty)
                {
                    party.Ai.SetDoNotMakeNewDecisions(false);
                }
                return;
            }
            else if (SubModule.PartyAIThinker.AssumingDirectControl.Contains(party) && !SubModule.PartyAIClanPartySettingsManager.Settings(party.LeaderHero).HasActiveOrder)
            {
                if (party.DefaultBehavior == AiBehavior.Hold)
                {
                    SubModule.PartyAIClanPartySettingsManager.Settings(party.LeaderHero).SetOrder(new(MainParty, OrderType.EscortParty));
                }
            }

            // buy horses while waiting in settlements
            if (party.CurrentSettlement != null)
            {
                PartiesBuyHorseCampaignBehaviorPatch.Prefix(party, party.CurrentSettlement, party.LeaderHero);
            }

            PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(party.LeaderHero);

            if (settings.AutoRecruitment && party.PartySizeRatio < settings.AutoRecruitmentPercentage && !SubModule.PartyAIThinker.AssumingDirectControl.Contains(party) && party.Army == null)
            {
                if (settings.HasActiveOrder)
                {
                    if (settings.Order.Behavior != OrderType.RecruitFromTemplate && !settings.OrderQueue.Any(o => o.Behavior == OrderType.RecruitFromTemplate))
                    {
                        settings.SetOrder(new(null, OrderType.RecruitFromTemplate));
                    }
                }
                else
                {
                    settings.SetOrder(new(null, OrderType.RecruitFromTemplate));
                }
            }

            if (settings.DismissUnwantedTroops && party.PartySizeRatio > settings.DismissUnwantedTroopsPercentage)
            {
                int max = (int)((party.PartySizeRatio - settings.DismissUnwantedTroopsPercentage) * party.Party.PartySizeLimit);
                if (max > 0)
                {
                    SubModule.PartyAITroopRecruiter.DismissUnwantedTroops(settings, party, max);
                }
            }

            if (settings.HasActiveOrder)
            {
                if (party.GetNumDaysForFoodToLast() < 3 && party.GetNumDaysForFoodToLast() > 0)
                {
                    AbandonOrderForNoFood(party, settings);
                    return;
                }

                if (settings.Order.Behavior == OrderType.DefendSettlement)
                {
                    ImplementDefendSettlement(settings, party, out _);
                    return;
                }
                if (settings.Order.Behavior == OrderType.StayInSettlement)
                {
                    ImplementStayInSettlement(settings, party, out _);
                    return;
                }
                if (settings.Order.Behavior == OrderType.VisitSettlement)
                {
                    ImplementVisitSettlement(settings, party, out _);
                    return;
                }
                if (settings.Order.Behavior == OrderType.AttackParty)
                {
                    ImplementAttackParty(settings, party, settings.Order.Target, out _);
                    return;
                }
                if (settings.Order.Behavior == OrderType.EscortParty)
                {
                    ImplementEscortParty(settings, party, settings.Order.Target, out _);
                    return;
                }
                if (settings.Order.Behavior == OrderType.RecruitFromTemplate)
                {
                    ImplementRecruitFromTemplate(settings, party);
                    return;
                }
            }
            else if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != OrderType.None && party.Army == null)
            {
                settings.SetOrder(settings.FallbackOrder);
            }
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            foreach (PartyAIClanPartySettings settings in SubModule.PartyAIClanPartySettingsManager.HeroesWithOrders)
            {
                if (settings.Order.Behavior == OrderType.BesiegeSettlement && settings.Order.Target == settlement)
                {
                    if (!FactionManager.IsAtWarAgainstFaction(settings.Hero.MapFaction, settlement.MapFaction))
                    {
                        settings.ClearOrder();
                    }
                }
            }
        }

        private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
        {
            if (SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(prisoner))
            {
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(prisoner);
                settings.ClearOrder();
                settings.OrderQueue.Clear();
            }
        }

        private void OnPartyJoinedArmy(MobileParty mobileParty)
        {
            if (SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(mobileParty?.LeaderHero))
            {
                if (!SubModule.PartyAIClanPartySettingsManager.HasActiveOrder(mobileParty.LeaderHero))
                {
                    return;
                }

                TextObject text = new TextObject("{=PAIOEWao2aI}{PARTY} is no longer {ORDER} because they were called to {ARMY}").SetTextVariable("PARTY", mobileParty.Name).SetTextVariable("ORDER", SubModule.PartyAIClanPartySettingsManager.GetOrderText(mobileParty.LeaderHero)).SetTextVariable("ARMY", mobileParty.Army.Name);
                InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Magenta));

                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero);
                settings.ClearOrder();
                settings.OrderQueue.Clear();
            }
        }

        private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
        {
            if (SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(mobileParty?.LeaderHero))
            {
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero);
                settings.ClearOrder();
                settings.OrderQueue.Clear();
            }

            foreach (PartyAIClanPartySettings settings in SubModule.PartyAIClanPartySettingsManager.HeroesWithOrders)
            {
                PAICustomOrder order = settings.Order;
                switch (order.Behavior)
                {
                    case OrderType.AttackParty:
                    case OrderType.EscortParty:
                        if (order.Target is not MobileParty m || m != mobileParty)
                        {
                            continue;
                        }
                        settings.ClearOrder();
                        if (SubModule.PartyAIThinker.AssumingDirectControl.Contains(settings.Hero.PartyBelongedTo))
                        {
                            settings.SetOrder(new(MainParty, OrderType.EscortParty));
                            SetPartyAiAction.GetActionForEscortingParty(mobileParty, MainParty, MainParty.NavigationCapability, false, MainParty.IsTargetingPort);
                            mobileParty.Ai.SetDoNotMakeNewDecisions(true);
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void OnMobilePartyCreated(MobileParty mobileParty)
        {
            if (SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(mobileParty?.LeaderHero))
            {
                PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(mobileParty.LeaderHero);
                settings.ClearOrder();
                settings.OrderQueue.Clear();
                settings.ResetBudgets();
                if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != OrderType.None)
                {
                    settings.SetOrder(settings.FallbackOrder);
                }
            }
        }

        internal void ProcessOrder(MobileParty party, PartyThinkParams thinkParams)
        {
            if (!SubModule.PartyAIClanPartySettingsManager.IsHeroManageable(party.LeaderHero))
            {
                return;
            }

            PartyAIClanPartySettings settings = SubModule.PartyAIClanPartySettingsManager.Settings(party.LeaderHero);
            ImplementAllowRaidingVillages(party, thinkParams, settings);
            ImplementAllowJoiningArmies(party, thinkParams, settings);
            ImplementAllowBesieging(party, thinkParams, settings);

            if (!settings.HasActiveOrder)
            {
                return;
            }

            if (party.GetNumDaysForFoodToLast() < 3 && party.GetNumDaysForFoodToLast() > 0)
            {
                AbandonOrderForNoFood(party, settings);
                return;
            }

            IMapPoint target = settings.Order.Target;
            PartyObjective existingObjective = party.Objective;
            List<(AIBehaviorData, float)> newParams;

            switch (settings.Order.Behavior)
            {
                case OrderType.EscortParty:
                    ImplementEscortParty(settings, party, target, out newParams);
                    break;
                case OrderType.RecruitFromTemplate:
                    ImplementRecruitFromTemplate(settings, party);
                    newParams = new(thinkParams.AIBehaviorScores);
                    break;
                case OrderType.AttackParty:
                    ImplementAttackParty(settings, party, target, out newParams);
                    break;
                case OrderType.PatrolAroundPoint:
                    ImplementPatrolAroundSettlement(settings, party, target, thinkParams, out newParams, distanceFactor: settings.PatrolRadius);
                    break;
                case OrderType.BesiegeSettlement:
                    ImplementBesiegeSettlement(settings, party, target, thinkParams, out newParams);
                    break;
                case OrderType.StayInSettlement:
                    ImplementStayInSettlement(settings, party, out newParams);
                    break;
                case OrderType.VisitSettlement:
                    ImplementVisitSettlement(settings, party, out newParams);
                    break;
                case OrderType.DefendSettlement:
                    ImplementDefendSettlement(settings, party, out newParams);
                    break;
                case OrderType.PatrolClanLands:
                    ImplementPatrolClanLands(settings.Hero, party, target, thinkParams, out newParams);
                    break;
                default:
                    return;
            }

            SwapParams(thinkParams, party, newParams);

            if (existingObjective != party.Objective)
            {
                settings.CachedPartyObjective = existingObjective;
            }
        }

        private void SwapParams(PartyThinkParams thinkParams, MobileParty party, List<(AIBehaviorData, float)> newParams)
        {
            thinkParams.Reset(party);
            float threshold = 0.3f;
            bool aboveThreshold = newParams.Any(p => p.Item2 > threshold);
            for (int i = 0; i < newParams.Count; i++)
            {
                (AIBehaviorData, float) param = newParams[i];
                if (!aboveThreshold)
                {
                    param.Item2 += threshold;
                }
                thinkParams.AddBehaviorScore(param);
            }
        }

        private void ImplementStayInSettlement(PartyAIClanPartySettings settings, MobileParty party, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new List<(AIBehaviorData, float)>();
            IMapPoint target = settings.Order.Target;
            Settlement settlement = (Settlement)target;

            if (FactionManager.IsAtWarAgainstFaction(target.MapFaction, party.MapFaction))
            {
                settings.ClearOrder();
                return;
            }

            party.Ai.SetDoNotMakeNewDecisions(true);

            if (party.GetNumDaysForFoodToLast() < 4 && party.GetNumDaysForFoodToLast() > 0)
            {
                Settlement town = SettlementHelper.FindNearestTownToMobileParty(party, party.NavigationCapability, s => (DiplomacyHelper.HasAllianceWithFaction(target.MapFaction, party.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)) && s != target).Settlement;
                SetPartyAiAction.GetActionForVisitingSettlement(party, town, party.NavigationCapability, false, party.IsTargetingPort);
                return;
            }

            if (party.CurrentSettlement != target)
            {
                if (settlement.IsUnderSiege)
                {
                    settings.ClearOrder();
                }
                else
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                }
            }
        }

        private void ImplementVisitSettlement(PartyAIClanPartySettings settings, MobileParty party, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new List<(AIBehaviorData, float)>();
            IMapPoint target = settings.Order.Target;
            Settlement settlement = (Settlement)target;

            if (FactionManager.IsAtWarAgainstFaction(target.MapFaction, party.MapFaction))
            {
                settings.ClearOrder();
                return;
            }

            party.Ai.SetDoNotMakeNewDecisions(true);

            if (party.GetNumDaysForFoodToLast() < 4 && party.GetNumDaysForFoodToLast() > 0)
            {
                Settlement town = SettlementHelper.FindNearestTownToMobileParty(party, party.NavigationCapability, s => (DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, s.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)) && s != target).Settlement;
                SetPartyAiAction.GetActionForVisitingSettlement(party, town, party.NavigationCapability, false, party.IsTargetingPort);
                return;
            }

            if (party.CurrentSettlement != target)
            {
                if (settlement.IsUnderSiege)
                {
                    settings.ClearOrder();
                }
                else
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                }
            }
        }

        private void ImplementDefendSettlement(PartyAIClanPartySettings settings, MobileParty party, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new List<(AIBehaviorData, float)>();
            IMapPoint target = settings.Order.Target;
            Settlement settlement = (Settlement)target;
            if (!DiplomacyHelper.HasAllianceWithFaction(target.MapFaction, party.MapFaction))
            {
                settings.ClearOrder();
                return;
            }

            party.Ai.SetDoNotMakeNewDecisions(true);

            if (party.GetNumDaysForFoodToLast() < 4 && party.GetNumDaysForFoodToLast() > 0)
            {
                Settlement town = SettlementHelper.FindNearestTownToMobileParty(party, party.NavigationCapability, s => DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, s.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)).Settlement;
                SetPartyAiAction.GetActionForVisitingSettlement(party, town, party.NavigationCapability, false, party.IsTargetingPort);
                return;
            }

            if (party.CurrentSettlement != target)
            {
                if (settlement.IsUnderSiege)
                {
                    SetPartyAiAction.GetActionForDefendingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                }
                else
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                }
            }
        }

        private void ImplementEscortParty(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new();
            if (target is not MobileParty targetParty || targetParty == null)
            {
                settings.ClearOrder();
                ResetPartyAi(party);
                return;
            }

            if (FactionManager.IsAtWarAgainstFaction(party.MapFaction, targetParty.MapFaction))
            {
                settings.ClearOrder();
                ResetPartyAi(party);
                return;
            }

            if (!party.Ai.DoNotMakeNewDecisions || party.DefaultBehavior == AiBehavior.Hold)
            {
                SetPartyAiAction.GetActionForEscortingParty(party, targetParty, party.NavigationCapability, false, party.IsTargetingPort);
                party.Ai.SetDoNotMakeNewDecisions(true);
            }
        }

        private void ImplementAttackParty(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new();
            if (target is not MobileParty targetParty || targetParty == null)
            {
                settings.ClearOrder();
                ResetPartyAi(party);
                return;
            }

            if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, targetParty.MapFaction))
            {
                settings.ClearOrder();
                ResetPartyAi(party);
                return;
            }

            if (!party.Ai.DoNotMakeNewDecisions || party.DefaultBehavior == AiBehavior.Hold)
            {
                SetPartyAiAction.GetActionForEngagingParty(party, targetParty, party.NavigationCapability, false);
                party.Ai.SetDoNotMakeNewDecisions(true);
            }
        }

        private void ImplementRecruitFromTemplate(PartyAIClanPartySettings settings, MobileParty party)
        {
            int freeSlots = (int)((1f - party.PartySizeRatio) * party.Party.PartySizeLimit);
            if (freeSlots < 1)
            {
                settings.ClearOrder();
                return;
            }

            if (settings.Order.Target is not Settlement currentTarget || party.CurrentSettlement == currentTarget)
            {
                IEnumerable<Settlement> settlements = Settlement.All.Where(s => (s.IsVillage || s.IsTown) && (settings.PartyTemplate?.TroopCultures.Contains(s.Culture) ?? true));
                currentTarget = SettlementHelper.FindNearestCastleToMobileParty(party, party.NavigationCapability, s =>
                {
                    if (s == party.CurrentSettlement || s.Position.Distance(party.Position) < 2f) { return false; }
                    if (_recentlyRecruitedFromSettlements.Any(l => l.Settlement == s && l.Party == party)) { return false; }
                    int count = 0;

                    AiVisitSettlementBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AiVisitSettlementBehavior>();
                    ValueTuple<int, float> result = (count, 0f);
                    AiVisitSettlementBehaviorPatches.Postfix(ref result, party.LeaderHero, s);
                    if (count < 3 && freeSlots > 3) { return false; }
                    if (count == 0) { return false; }
                    return true;
                });

                if (currentTarget != null)
                {
                    _recentlyRecruitedFromSettlements.Add(new(currentTarget, CampaignTime.Now, party));
                    settings.Order.Target = currentTarget;
                }
            }

            if (currentTarget == null)
            {
                if (party.Ai.DoNotMakeNewDecisions)
                {
                    party.Ai.SetDoNotMakeNewDecisions(false);
                    ResetPartyAi(party);
                }
                return;
            }

            party.Ai.SetDoNotMakeNewDecisions(true);
            SetPartyAiAction.GetActionForVisitingSettlement(party, currentTarget, party.NavigationCapability, false, party.IsTargetingPort);
        }

        private void ImplementBesiegeSettlement(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target, in PartyThinkParams thinkParams, out List<(AIBehaviorData, float)> newParams)
        {
            newParams = new List<(AIBehaviorData, float)>();
            if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction))
            {
                settings.ClearOrder();
                ResetPartyAi(party);
                return;
            }

            if (!party.Ai.DoNotMakeNewDecisions || party.DefaultBehavior == AiBehavior.Hold)
            {
                SetPartyAiAction.GetActionForBesiegingSettlement(party, (Settlement)target, party.NavigationCapability, false);
                party.Ai.SetDoNotMakeNewDecisions(true);
            }
        }

        private void ImplementPatrolClanLands(Hero hero, MobileParty party, IMapPoint target, in PartyThinkParams thinkParams, out List<(AIBehaviorData, float)> newParams, float distanceFactor = 1.0f, bool useQuickDistance = false)
        {
            newParams = new List<(AIBehaviorData, float)>();
            float range = Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(party.NavigationCapability) * 0.9f * distanceFactor;

            if (hero?.Clan?.Settlements?.Count == 0)
            {
                newParams = thinkParams.AIBehaviorScores.ConvertAll(s => (s.Item1, s.Item2));
                return;
            }

            if (party.GetNumDaysForFoodToLast() < 8 && party.GetNumDaysForFoodToLast() > 0)
            {
                Settlement town = SettlementHelper.FindNearestTownToMobileParty(party, party.NavigationCapability, s => DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, s.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)).Settlement;
                if (town != null)
                {
                    newParams.Add((new AIBehaviorData(town, AiBehavior.GoToSettlement, NavigationType.Default, false, false, false), 2f));
                }
                return;
            }

            if (hero?.Clan == null) { return; }
            Settlement nearestClan = SettlementHelper.FindNearestSettlementToMobileParty(party, NavigationType.All, (Settlement x) => x.OwnerClan == hero.Clan);
            if (nearestClan == null) { return; }

            // Low chance to redirect around another clan settlement
            if (MBRandom.RandomFloat < 0.05f)
            {
                nearestClan = SettlementHelper.FindRandomSettlement((Settlement x) => x.OwnerClan == hero.Clan);
            }

            foreach (Settlement clanSettlement in hero.Clan.Settlements)
            {
                if (party.Position.Distance(clanSettlement.Position) > range * 8) { continue; }
                if (clanSettlement.IsFortification && clanSettlement.IsUnderSiege)
                {
                    SetPartyAiAction.GetActionForDefendingSettlement(party, clanSettlement, party.NavigationCapability, false, party.IsTargetingPort);
                    return;
                }
                if (clanSettlement.IsVillage && clanSettlement.Village.VillageState == Village.VillageStates.BeingRaided)
                {
                    SetPartyAiAction.GetActionForDefendingSettlement(party, clanSettlement, party.NavigationCapability, false, party.IsTargetingPort);
                    return;
                }
            }

            if (party.Position.Distance(nearestClan.Position) > range * 4)
            {
                SetPartyAiAction.GetActionForVisitingSettlement(party, nearestClan, party.NavigationCapability, false, party.IsTargetingPort);
                return;
            }

            foreach ((AIBehaviorData, float) param in thinkParams.AIBehaviorScores)
            {
                float distance = party.Position.Distance(nearestClan.Position);

                if (distance < range)
                {
                    newParams.Add(param);
                }
            }

            if (party.Objective != PartyObjective.Aggressive)
            {
                party.SetPartyObjective(PartyObjective.Aggressive);
            }
        }

        private void ImplementPatrolAroundSettlement(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target, in PartyThinkParams thinkParams, out List<(AIBehaviorData, float)> newParams, float distanceFactor = 1.0f)
        {
            newParams = new List<(AIBehaviorData, float)>();
            float range = CalculateAverageDistanceBetweenTowns() * 0.9f * distanceFactor;

            if (party.GetNumDaysForFoodToLast() < 8 && party.GetNumDaysForFoodToLast() > 0)
            {
                Settlement town = SettlementHelper.FindNearestTownToMobileParty(party, party.NavigationCapability, s => DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, s.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)).Settlement;
                if (town != null)
                {
                    newParams.Add((new AIBehaviorData(town, AiBehavior.GoToSettlement, NavigationType.Default, false, false, false), 2f));
                }
                return;
            }

            int index = -1;
            do
            {
                index = SettlementHelper.FindNextSettlementAroundMobileParty(party, party.NavigationCapability, range, index, null);
                if (index >= 0)
                {
                    Settlement settlementInRange = Settlement.All[index];
                    if (!DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, settlementInRange.MapFaction))
                    {
                        continue;
                    }
                    if (settlementInRange.IsFortification && settlementInRange.IsUnderSiege)
                    {
                        SetPartyAiAction.GetActionForDefendingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                        return;
                    }
                    if (settlementInRange.IsVillage && settlementInRange.Village.VillageState == Village.VillageStates.BeingRaided)
                    {
                        SetPartyAiAction.GetActionForDefendingSettlement(party, (Settlement)target, party.NavigationCapability, false, party.IsTargetingPort);
                        return;
                    }
                }
            } while (index >= 0);

            if (party.Position.Distance(target.Position) > range * 4 && target is Settlement settlement)
            {
                SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, party.NavigationCapability, false, party.IsTargetingPort);
                return;
            }

            foreach ((AIBehaviorData, float) param in thinkParams.AIBehaviorScores)
            {
                float distance = param.Item1.Party.Position.Distance(target.Position);

                if (distance < range)
                {
                    newParams.Add(param);
                }
            }

            if (party.Objective != PartyObjective.Aggressive)
            {
                party.SetPartyObjective(PartyObjective.Aggressive);
            }
        }
        private float CalculateAverageDistanceBetweenTowns()
        {
            float num = 0f;
            int num2 = 0;
            IEnumerable<Town> allTowns = Settlement.All.Where(x => x.IsTown).Select(x => x.Town);
            foreach (Town town in allTowns)
            {
                float num3 = 5000f;
                foreach (Town town2 in allTowns)
                {
                    if (town != town2)
                    {
                        float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(town.Settlement, town2.Settlement.Position, false, NavigationType.Default);
                        if (distance < num3)
                        {
                            num3 = distance;
                        }
                    }
                }
                num += num3;
                num2++;
            }
            return num / (float)num2;
        }
        
        private void ImplementAllowRaidingVillages(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
        {
            if (settings.AllowRaidVillages)
            {
                return;
            }

            // prevent raiding in army (leave if they raid)
            // The other half of this is in HarmonyPatches.AiMilitaryBehaviorPatches
            if (party.Army != null && !party.Army.LeaderParty.LeaderHero.Equals(party.LeaderHero) && party.DefaultBehavior == AiBehavior.RaidSettlement && party.Army.ArmyType == Army.ArmyTypes.Raider)
            {
                // refund influence
                int influence = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(party.Army.LeaderParty, party);
                ChangeClanInfluenceAction.Apply(party.Army.LeaderParty.LeaderHero.Clan, influence);

                LeaveArmy(party, thinkParams);
            }
        }

        private void ImplementAllowJoiningArmies(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
        {
            if (settings.AllowAllowJoinArmies)
            {
                return;
            }

            // leave army if setting is disabled
            if (party.Army != null && !party.Army.LeaderParty.LeaderHero.Equals(party.LeaderHero) && !party.Army.LeaderParty.LeaderHero.Equals(Hero.MainHero))
            {
                LeaveArmy(party, thinkParams);
            }
        }

        private void ImplementAllowBesieging(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
        {
            if (settings.AllowSieging)
            {
                return;
            }

            // prevent besieging in army (leave if they besiege)
            // The other half of this is in HarmonyPatches.AiMilitaryBehaviorPatches
            if (party.Army != null && !party.Army.LeaderParty.LeaderHero.Equals(party.LeaderHero) && party.DefaultBehavior == AiBehavior.BesiegeSettlement && party.DefaultBehavior == AiBehavior.AssaultSettlement && party.Army.ArmyType == Army.ArmyTypes.Besieger)
            {
                LeaveArmy(party, thinkParams);
            }
        }

        private void AbandonOrderForNoFood(MobileParty party, PartyAIClanPartySettings settings)
        {
            TextObject text = new TextObject("{=PAIw38SHHlH}{PARTY} is no longer {ORDER} because their food supplies ran low.").SetTextVariable("PARTY", party.Name).SetTextVariable("ORDER", SubModule.PartyAIClanPartySettingsManager.GetOrderText(party.LeaderHero));
            InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Magenta));
            settings.ClearOrder();
            ResetPartyAi(party);
        }

        private void LeaveArmy(MobileParty party, PartyThinkParams thinkParams)
        {
            // refund influence
            int influence = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(party.Army.LeaderParty, party);
            ChangeClanInfluenceAction.Apply(party.Army.LeaderParty.LeaderHero.Clan, influence);
            party.Army = null;
            party.SetMoveGoToSettlement(SettlementHelper.FindNearestFortificationToMobileParty(party, party.NavigationCapability, (Settlement settlement) => DiplomacyHelper.IsSameFactionAndNotEliminated(party.MapFaction, settlement.MapFaction) && (DiplomacyHelper.HasAllianceWithFaction(party.MapFaction, settlement.MapFaction) || FactionManager.IsNeutralWithFaction(party.MapFaction, settlement.MapFaction))), party.NavigationCapability, false);
            ResetPartyAi(party);
            thinkParams.Reset(party);
        }

        private void ResetPartyAi(MobileParty party)
        {
            party.Ai.RethinkAtNextHourlyTick = true;
            party.RecalculateShortTermBehavior();
        }

        private IEnumerable<WarPartyComponent> ActiveClanParties(Clan c) => c.WarPartyComponents.Where(p => p.MobileParty != MobileParty.MainParty);

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_assumingDirectControl", ref _assumingDirectControl);
            dataStore.SyncData("_recentlyRecruitedFromSettlements", ref _recentlyRecruitedFromSettlements);
        }
    }
}
