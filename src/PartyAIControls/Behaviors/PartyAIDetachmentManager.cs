using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.Behaviors
{
  internal class PartyAIDetachmentManager : CampaignBehaviorBase
  {
    private List<PAIDetatchmentConfig> _detatchments = new();

    public override void RegisterEvents()
    {
    }

    internal void CreateNewDetatchment(TroopRoster roster, PAIDetatchmentConfig config)
    {
      CampaignVec2 spawnPoint;
      Settlement home;
      TextObject name = new("{=PAIxQtQFzxH}Detatchment of {NAME}");
      Clan clan;
      if (config.Target is Settlement s)
      {
        spawnPoint = s.GatePosition;
        home = s;
        name.SetTextVariable("NAME", s.Name);
        clan = s.OwnerClan;
      }
      else if (config.Target is MobileParty m)
      {
        spawnPoint = m.Position;
        home = m.Owner.HomeSettlement;
        name.SetTextVariable("NAME", m.Name);
        clan = m.ActualClan;
      }
      else
      {
        return;
      }

      config.Party = CustomPartyComponent.CreateCustomPartyWithTroopRoster(spawnPoint, 5f, home, name, clan, roster, TroopRoster.CreateDummyTroopRoster(), clan.Leader, customPartyBaseSpeed: 4f);
      _detatchments.Add(config);
    }

    internal bool IsDetatchment(MobileParty party) => party != null && _detatchments.Any(d => d.Party == party);

    public override void SyncData(IDataStore dataStore)
    {
      dataStore.SyncData("_detatchments", ref _detatchments);
    }
  }
}
