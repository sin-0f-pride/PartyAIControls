using Bannerlord.UIExtenderEx;
using HarmonyLib;
using PartyAIControls.CampaignBehaviors;
using PartyAIControls.GauntletUI;
using PartyAIControls.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace PartyAIControls
{
    public class SubModule : MBSubModuleBase
    {
        internal static PartyAIThinker PartyAIThinker;
        internal static PartyAIClanPartySettingsManager PartyAIClanPartySettingsManager;
        internal static PartyAITroopRecruiter PartyAITroopRecruiter;
        public static bool BannerKings = false;
        private static bool IsPopupOpen = false;
        internal static PartyAIDetachmentManager DetatchmentManager;

        internal static PACInformationManager PACInformationManager;

        protected override void OnSubModuleLoad()
        {
            BannerKings = Utilities.GetModulesNames().Contains("BannerKings");

            var extender = UIExtender.Create("PartyAIControls");
            extender.Register(typeof(SubModule).Assembly);
            extender.Enable();

            var harmony = new Harmony("bannerlord.partyaicontrols.patches");
            harmony.PatchAll();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=!}Party AI Controls loaded").ToString(), Color.FromUint(0x00E67E22)));
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (gameStarterObject is CampaignGameStarter starter)
            {
                starter.AddBehavior(PartyAIThinker = new PartyAIThinker());
                starter.AddBehavior(PartyAIClanPartySettingsManager = new PartyAIClanPartySettingsManager());
                starter.AddBehavior(PartyAITroopRecruiter = new PartyAITroopRecruiter());
                starter.AddModel(new PAITroopUpgradeModel(GetModel<PartyTroopUpgradeModel>(starter)));
                starter.AddModel(new PAIPrisonerRecruitmentCalculationModel(GetModel<PrisonerRecruitmentCalculationModel>(starter)));
                starter.AddModel(new PAISettlementGarrisonModel(GetModel<SettlementGarrisonModel>(starter)));
                PACInformationManager = new PACInformationManager();
            }
        }

        public override void OnGameInitializationFinished(Game game)
        {
            if (game.GameType is not Campaign)
            {
                return;
            }
            /*
            ValidateGameModel(Campaign.Current.Models.PartyTroopUpgradeModel);
            ValidateGameModel(Campaign.Current.Models.ArmyManagementCalculationModel);
            ValidateGameModel(Campaign.Current.Models.PrisonerRecruitmentCalculationModel);
            ValidateGameModel(Campaign.Current.Models.SettlementGarrisonModel);
            ValidateGameModel(Campaign.Current.Models.PartyFoodBuyingModel);
            // TODO: Make a savegame variable to only show this on the first start with the mod.
            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=PAIEUwVpMPm}Thank you for using Party AI Controls! To access the configuration panel, press {KEYBIND}!").SetTextVariable("KEYBIND", PartyAIClanPartySettingsManager.ControlPanelModiferKey.ToString() + "+" + PartyAIClanPartySettingsManager.ControlPanelKey.ToString()).ToString(), Colors.Green));
            */
        }

        /*
        private void ValidateGameModel(GameModel model)
        {
            if (model.GetType().Assembly == GetType().Assembly) { return; }
            if (!model.GetType().BaseType.IsAbstract)
            {
                TextObject error = new("{=I2LlBDKr}Game Model Error: Please move " + GetType().Assembly.GetName().Name + " below " + model.GetType().Assembly.GetName().Name + " in your load order to ensure mod compatibility");
                InformationManager.DisplayMessage(new InformationMessage(error.ToString(), Colors.Red));
            }
        }*/

        protected override void OnApplicationTick(float dt)
        {
            if (Game.Current?.GameStateManager?.ActiveState == null || Game.Current.GameStateManager.ActiveState is not MapState || Game.Current.GameStateManager.ActiveState.IsMenuState || CampaignMission.Current != null)
            {
                return;
            }

            if ((Input.IsKeyDown(PartyAIClanPartySettingsManager.ControlPanelModiferKey) || PartyAIClanPartySettingsManager.ControlPanelModiferKey == InputKey.Invalid) && Input.IsKeyDown(PartyAIClanPartySettingsManager.ControlPanelKey))
            {
                GameStateManager.Current.PushState(GameStateManager.Current.CreateState<PartyAIControlsMenuState>());
                return;
            }

            if ((Input.IsKeyDown(PartyAIClanPartySettingsManager.CommandedPartiesModiferKey) || PartyAIClanPartySettingsManager.CommandedPartiesModiferKey == InputKey.Invalid) && Input.IsKeyDown(PartyAIClanPartySettingsManager.CommandedPartiesKey))
            {
                if (IsPopupOpen) { return; }
                CampaignTimeControlMode mode = Campaign.Current.TimeControlMode;
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.FastForwardStop;
                string title = new TextObject("{=PAIFHytp3D7}Choose which parties to directly command").ToString();
                string desc = new TextObject("{=PAIRzSgh49H}Parties must be manageable and in visual range to appear here.").ToString();
                List<InquiryElement> list = MobileParty.AllLordParties.Where(m => PartyAIClanPartySettingsManager.IsHeroManageable(m.LeaderHero) && m.Position.Distance(MobileParty.MainParty.Position) <= MobileParty.MainParty.SeeingRange).Where(m => m.Army == null || m.Army.LeaderParty == m).OrderByDescending(m => m.ActualClan.Equals(Clan.PlayerClan)).ThenBy(m => m.Name?.ToString()).ToList().ConvertAll(m => new InquiryElement(m, m.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(m.LeaderHero?.CharacterObject))));

                MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, desc, list, true, minSelectableOptionCount: 0, maxSelectableOptionCount: list.Count, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(),
                    (List<InquiryElement> results) =>
                    {
                        PartyAIThinker.ClearAssumingDirectControl();
                        foreach (InquiryElement e in results)
                        {
                            if (e.Identifier is MobileParty m)
                            {
                                PartyAIThinker.AddToAssumingDirectControl(m);
                            }
                        }
                        IsPopupOpen = false;
                        Campaign.Current.TimeControlMode = mode;
                    }, (List<InquiryElement> results) =>
                    {
                        IsPopupOpen = false;
                        Campaign.Current.TimeControlMode = mode;
                    }, isSeachAvailable: true)
                );
                IsPopupOpen = true;
            }
        }

        private T GetModel<T>(CampaignGameStarter starter) where T : GameModel
        {
            foreach (GameModel mode in starter.Models)
            {
                if (mode is T gameModel)
                {
                    return gameModel;
                }
            }
            return default!;
        }

        public static void Log(string message)
        {
            string text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Mount and Blade II Bannerlord", "Logs");
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }
            string path = System.IO.Path.Combine(text, "PartyAIControls.txt");
            using (StreamWriter streamWriter = new StreamWriter(path, true))
            {
                streamWriter.WriteLine("[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + message);
            }
        }
    }
}
