using PartyAIControls.ViewModels;
using PartyAIControls.ViewModels.MenuOptionVMs;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace PartyAIControls.GauntletUI
{
    internal class PACInformationManager : GlobalLayer
    {
        private class PAInfoLayer : GlobalLayer
        {
            private readonly GauntletLayer _layer;
            private readonly List<SpriteCategory> _spriteCategories = new();
            private GauntletMovieIdentifier _movie = null;
            internal PAInfoLayer(int order)
            {
                _layer = new GauntletLayer("PAInfoLayer", order);
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
                Layer = _layer;
                ScreenManager.AddGlobalLayer(this, true);
                ScreenManager.SetSuspendLayer(Layer, true);
            }

            internal GauntletMovieIdentifier LoadMovie(string movie, ViewModel datasource) => _movie = _layer.LoadMovie(movie, datasource);

            internal void LoadSpriteCategories(string[] categories)
            {
                foreach (string cat in categories)
                {
                    SpriteCategory category = UIResourceManager.SpriteData.SpriteCategories[cat];
                    category.Load(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot);
                    _spriteCategories.Add(category);
                }
            }

            internal void UnloadSpriteCategories()
            {
                foreach (SpriteCategory category in _spriteCategories)
                {
                    category.Unload();
                }
                _spriteCategories.Clear();
            }
            internal void RegisterState()
            {
                InformationManager.HideTooltip();
                SetLayerFocus(isFocused: true);

                GameStateManager.Current.RegisterActiveStateDisableRequest(this);
                MBCommon.PauseGameEngine();
            }

            internal void CloseQuery()
            {
                SetLayerFocus(isFocused: false);
                if (_movie != null)
                {
                    _layer.ReleaseMovie(_movie);
                    _movie = null;
                }

                if (_movie == null)
                {
                    UnloadSpriteCategories();
                    GameStateManager.Current.UnregisterActiveStateDisableRequest(this);
                    MBCommon.UnPauseGameEngine();
                }
                UnloadSpriteCategories();
            }

            private void SetLayerFocus(bool isFocused)
            {
                if (isFocused)
                {
                    ScreenManager.SetSuspendLayer(Layer, isSuspended: false);
                    Layer.IsFocusLayer = true;
                    ScreenManager.TrySetFocus(Layer);
                    Layer.InputRestrictions.SetInputRestrictions();
                }
                else
                {
                    Layer.InputRestrictions.ResetInputRestrictions();
                    ScreenManager.SetSuspendLayer(Layer, isSuspended: true);
                    Layer.IsFocusLayer = false;
                    ScreenManager.TryLoseFocus(Layer);
                }
            }

            protected override void OnEarlyTick(float dt)
            {
                base.OnEarlyTick(dt);
                if (_movie != null)
                {
                    if (ScreenManager.FocusedLayer != Layer)
                    {
                        SetLayerFocus(isFocused: true);
                    }
                    if (_layer.Input.IsHotKeyDown("Confirm"))
                    {
                        UISoundsHelper.PlayUISound("event:/ui/panels/next");
                        CloseQuery();
                    }
                    else if (_layer.Input.IsHotKeyReleased("Exit"))
                    {
                        UISoundsHelper.PlayUISound("event:/ui/panels/next");
                        CloseQuery();
                    }
                }
            }
        }

        private readonly PAInfoLayer _layer1;
        private readonly PAInfoLayer _layer2;
        private readonly PAInfoLayer _layer3;

        public PACInformationManager()
        {
            _layer1 = new(4497);
            _layer2 = new(4498);
            _layer3 = new(4499);
        }

        public void ShowPartyCompositionInquiry(PartyAIClanPartySettings settings, Action<PartyCompositionObect> callback)
        {
            bool flag = !CheckContext();
            if (!flag)
            {
                _layer3.LoadMovie("PartyAICompositionSliders", new PartyAICompositionSlidersVM(settings, delegate (PartyCompositionObect comp)
                {
                    _layer3.CloseQuery();
                    Action<PartyCompositionObect> callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2(comp);
                    }
                }));
                _layer3.RegisterState();
            }
        }

        public void ShowDetachmentsInquiry()
        {
            if (CheckContext())
            {
                _layer1.LoadMovie("PartyAIDetachments", new PartyAIDetachmentsVM(delegate ()
                {
                    _layer1.CloseQuery();
                }));
                _layer1.RegisterState();
            }
        }

        public void ShowNumberPickerInquiry(int initialValue, int minValue, int maxValue, string title, string description, Action<int> callback, bool isPercentage = true)
        {
            if (CheckContext())
            {
                _layer3.LoadMovie("PartyAINumberPicker", new PartyAINumberPickerVM(initialValue, minValue, maxValue, title, description, delegate (int value)
                {
                    _layer3.CloseQuery();
                    Action<int> callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2(value);
                    }
                }, isPercentage));
                _layer3.RegisterState();
            }
        }

        public void ShowModOptionsInquiry(Action callback)
        {
            if (CheckContext())
            {
                _layer1.LoadMovie("PartyAIModOptions", new PartyAIModOptionsVM(delegate ()
                {
                    _layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                _layer1.RegisterState();
            }
        }

        public void ShowPartyOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            if (CheckContext())
            {
                _layer2.LoadMovie("PartyAIPartyOptions", new PartyAIPartyOptionsVM(settings, delegate ()
                {
                    _layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                _layer2.RegisterState();
            }
        }

        public void ShowCaravanOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            if (CheckContext())
            {
                _layer2.LoadMovie("PartyAICaravanOptions", new PartyAICaravanOptionsVM(settings, delegate ()
                {
                    _layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                _layer2.RegisterState();
            }
        }

        public void ShowGarrisonOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            if (CheckContext())
            {
                _layer2.LoadMovie("PartyAIGarrisonOptions", new PartyAIGarrisonOptionsVM(settings, delegate ()
                {
                    _layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                _layer2.RegisterState();
            }
        }

        public void ShowOrderQueueInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            if (CheckContext())
            {
                _layer1.LoadSpriteCategories(new string[]
                {
                    "ui_partyscreen"
                });
                _layer1.LoadMovie("PartyAIOrderQueue", new PartyAIOrderQueueVM(settings, delegate ()
                {
                    _layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                    _layer1.UnloadSpriteCategories();
                }));
                _layer1.RegisterState();
            }
        }

        public void ShowDefaultSettingsInquiry(Action callback)
        {
            if (CheckContext())
            {
                _layer1.LoadMovie("PartyAIDefaultSettings", new PartyAIDefaultSettingsVM(delegate ()
                {
                    _layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                _layer1.RegisterState();
            }
        }

        private bool CheckContext()
        {
            if (Campaign.Current == null)
            {
                InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZrVNUOk}Error", null).ToString(), new TextObject("{=PAI0m3MyqBm}You must load a save game to use this menu.", null).ToString(), true, false, new TextObject("{=Y94H6XnK}Accept", null).ToString(), string.Empty, null, null, "", 0f, null, null, null), false, false);
                return false;
            }
            return Mission.Current == null;
        }
    }
}
