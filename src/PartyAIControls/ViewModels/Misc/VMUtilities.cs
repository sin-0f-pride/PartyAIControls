using HarmonyLib;
using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using static Helpers.PartyScreenHelper;

namespace PartyAIControls.ViewModels.Misc
{
    internal static class VMUtilities
    {
        internal static void OpenPartyScreen(TroopRoster leftRoster, TroopRoster rightRoster, TextObject leftPartyName = null, TextObject rightPartyName = null, TextObject header = null, PartyPresentationDoneButtonDelegate doneDelegate = null, PartyPresentationDoneButtonConditionDelegate doneCondition = null, PartyBase leftParty = null, PartyBase rightParty = null, int leftSizeLimit = 0, IsTroopTransferableDelegate transferableDelegate = null, PartyPresentationCancelButtonDelegate cancelDelegate = null)
        {
            Game current = Game.Current;
            PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
            if (leftRoster == null)
            {
                leftRoster = TroopRoster.CreateDummyTroopRoster();
            }
            if (rightRoster == null)
            {
                rightRoster = TroopRoster.CreateDummyTroopRoster();
            }
            PartyScreenLogicInitializationData initializationData = new()
            {
                LeftOwnerParty = leftParty,
                RightOwnerParty = rightParty ?? PartyBase.MainParty,
                LeftMemberRoster = leftRoster,
                LeftPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
                RightMemberRoster = rightRoster,
                RightPrisonerRoster = TroopRoster.CreateDummyTroopRoster(),
                LeftLeaderHero = null,
                RightLeaderHero = null,
                LeftPartyMembersSizeLimit = leftParty != null ? leftParty.PartySizeLimit : leftSizeLimit,
                LeftPartyPrisonersSizeLimit = 0,
                RightPartyMembersSizeLimit = rightParty != null ? rightParty.PartySizeLimit : leftRoster.Count + rightRoster.Count,
                RightPartyPrisonersSizeLimit = 0,
                LeftPartyName = leftParty == null ? leftPartyName : leftParty.Name,
                RightPartyName = rightParty == null ? rightPartyName : (rightParty ?? PartyBase.MainParty).Name,
                TroopTransferableDelegate = transferableDelegate ?? IsTroopTransferable,
                PartyPresentationDoneButtonDelegate = doneDelegate,
                PartyPresentationDoneButtonConditionDelegate = doneCondition != null ? doneCondition : IsTemplateRosterValid,
                PartyPresentationCancelButtonActivateDelegate = null,
                PartyPresentationCancelButtonDelegate = cancelDelegate,
                IsDismissMode = false,
                IsTroopUpgradesDisabled = true,
                Header = header,
                PartyScreenClosedDelegate = null,
                TransferHealthiesGetWoundedsFirst = false,
                ShowProgressBar = false,
                MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable
            };

            partyScreenLogic.Initialize(initializationData);
            PartyState partyState = current.GameStateManager.CreateState<PartyState>();
            partyState.PartyScreenLogic = partyScreenLogic;
            partyState.IsDonating = true;
            partyState.PartyScreenMode = PartyScreenMode.Normal;
            current.GameStateManager.PushState(partyState);
        }

        private static bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
        {
            if (!character.IsHero && !character.IsNotTransferableInPartyScreen && type != PartyScreenLogic.TroopType.Prisoner)
            {
                return true;
            }
            return false;
        }

        internal static Tuple<bool, TextObject> IsTemplateRosterValid(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
        {
            if (rightMemberRoster.TotalManCount > 0)
            {
                return new Tuple<bool, TextObject>(true, null);
            }
            else
            {
                return new Tuple<bool, TextObject>(false, new TextObject("{=PAIAAm1PQy1}Not enough troops in template."));
            }
        }
    }
}
