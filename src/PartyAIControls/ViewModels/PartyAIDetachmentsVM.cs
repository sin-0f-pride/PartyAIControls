using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels
{
  internal class PartyAIDetachmentsVM : ViewModel
  {
    private readonly Action _onClose;
    private bool _isVisible;

    public PartyAIDetachmentsVM(Action callback)
    {
      TitleText = new TextObject("{=PAIgAsElRcK}Detatchment Management").ToString();
      IsVisible = true;

      _onClose = callback;

      RefreshValues();
    }

    [DataSourceProperty] public string AcceptText => GameTexts.FindText("str_done").ToString();
    [DataSourceProperty] public string CreateDetachmentText => new TextObject("{=PAYjAC3mQzN}Create").ToString();
    [DataSourceProperty] public string TitleText { get; private set; }
    [DataSourceProperty]
    public bool IsVisible
    {
      get
      {
        return _isVisible;
      }
      set
      {
        if (value != _isVisible)
        {
          _isVisible = value;
          OnPropertyChangedWithValue(value, "IsVisible");
        }
      }
    }

    public void CreateDetachment()
    {
      string title = new TextObject("{=PAIdmeoGhXk}Choose Parent Party").ToString();
      string description = new TextObject("{=PAIALZhTNO9}Choose which party the new detachment will detach from").ToString();
      List<InquiryElement> partyList = PartyAIControlsMenuVM.Instance.PartyList.Where(p => !p.IsCaravan).ToList().ConvertAll(p =>
      {
        if (p.Party.MobileParty.IsGarrison)
        {
          return new InquiryElement(p.Party.MobileParty, p.Party.Name.ToString(), new BannerImageIdentifier(p.Party.Owner.ClanBanner));
        }
        return new InquiryElement(p.Party.MobileParty, p.Party.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(p.Party.Owner.CharacterObject)));
      });
      IsVisible = false;
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, description, partyList, true, 1, 1, GameTexts.FindText("str_next", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), delegate (List<InquiryElement> results)
            {
                InquiryElement inquiryElement = results.FirstOrDefault<InquiryElement>();
                object obj = (inquiryElement != null) ? inquiryElement.Identifier : null;
                MobileParty party = obj as MobileParty;
                bool flag = party == null;
                if (!flag)
                {
                    List<InquiryElement> list = new List<InquiryElement>();
                    IMapPoint mapPoint2;
                    if (!party.IsGarrison)
                    {
                        IMapPoint mapPoint = party;
                        mapPoint2 = mapPoint;
                    }
                    else
                    {
                        IMapPoint mapPoint = party.CurrentSettlement;
                        mapPoint2 = mapPoint;
                    }
                    IMapPoint target = mapPoint2;
                    list.Add(new InquiryElement(new PAIDetatchmentConfig(PAIDetatchmentConfig.DetatchmentType.Recruiter, target), new TextObject("{=PAINxQlJwm0}Recruiter", null).ToString(), null));
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(new TextObject("{=PAIlhut21a1}Choose Detatchment Type", null).ToString(), string.Empty, list, true, 1, 1, GameTexts.FindText("str_next", null).ToString(), GameTexts.FindText("str_cancel", null).ToString(), delegate (List<InquiryElement> config)
                    {
                        VMUtilities.OpenPartyScreen(TroopRoster.CreateDummyTroopRoster(), party.MemberRoster, new TextObject("{=PAIduQc8nb6}Select troops for detachment", null), null, new TextObject("{=PAIUP5cqkl4}Create Detachment", null), delegate (TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
                        {
                            SubModule.DetatchmentManager.CreateNewDetatchment(leftMemberRoster, config.First<InquiryElement>().Identifier as PAIDetatchmentConfig);
                            this.IsVisible = true;
                            return true;
                        }, (TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum) => new Tuple<bool, TextObject>(true, new TextObject("", null)), null, party.Party, 0, null, delegate
                        {
                            this.IsVisible = true;
                        });
                    }, delegate (List<InquiryElement> _)
                    {
                        this.IsVisible = true;
                    }, "", false), false, false);
                }
            }, delegate (List<InquiryElement> _)
            {
                this.IsVisible = true;
            }, "", true), false, false);
        }

        public void Accept() => _onClose?.Invoke();
  }
}
