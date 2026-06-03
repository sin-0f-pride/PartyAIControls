using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace PartyAIControls.Patches
{
    [HarmonyPatch(typeof(CampaignEventDispatcher), "AiHourlyTick")]
    internal class CampaignEventDispatcherPatches
    {
        private static void Postfix(MobileParty party, ref PartyThinkParams partyThinkParams)
        {
            SubModule.PartyAIThinker.ProcessOrder(party, partyThinkParams);
        }
    }
}
