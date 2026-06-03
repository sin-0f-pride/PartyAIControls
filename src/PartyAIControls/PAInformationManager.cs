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

namespace PartyAIControls
{
    internal class PAInformationManager : GlobalLayer
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

        public PAInformationManager()
        {
            _layer1 = new(4497);
            _layer2 = new(4498);
            _layer3 = new(4499);
        }

        // Token: 0x06000075 RID: 117 RVA: 0x000032D0 File Offset: 0x000014D0
        public void ShowPartyCompositionInquiry(PartyAIClanPartySettings settings, Action<PartyCompositionObect> callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer3.LoadMovie("PartyAICompositionSliders", new PartyAICompositionSlidersVM(settings, delegate (PartyCompositionObect comp)
                {
                    this._layer3.CloseQuery();
                    Action<PartyCompositionObect> callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2(comp);
                    }
                }));
                this._layer3.RegisterState();
            }
        }

        // Token: 0x06000076 RID: 118 RVA: 0x00003334 File Offset: 0x00001534
        public void ShowDetachmentsInquiry()
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer1.LoadMovie("PartyAIDetachments", new PartyAIDetachmentsVM(delegate ()
                {
                    this._layer1.CloseQuery();
                }));
                this._layer1.RegisterState();
            }
        }

        // Token: 0x06000077 RID: 119 RVA: 0x00003380 File Offset: 0x00001580
        public void ShowNumberPickerInquiry(int initialValue, int minValue, int maxValue, string title, string description, Action<int> callback, bool isPercentage = true)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer3.LoadMovie("PartyAINumberPicker", new PartyAINumberPickerVM(initialValue, minValue, maxValue, title, description, delegate (int value)
                {
                    this._layer3.CloseQuery();
                    Action<int> callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2(value);
                    }
                }, isPercentage));
                this._layer3.RegisterState();
            }
        }

        // Token: 0x06000078 RID: 120 RVA: 0x000033EC File Offset: 0x000015EC
        public void ShowModOptionsInquiry(Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer1.LoadMovie("PartyAIModOptions", new PartyAIModOptionsVM(delegate ()
                {
                    this._layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                this._layer1.RegisterState();
            }
        }

        // Token: 0x06000079 RID: 121 RVA: 0x0000344C File Offset: 0x0000164C
        public void ShowPartyOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer2.LoadMovie("PartyAIPartyOptions", new PartyAIPartyOptionsVM(settings, delegate ()
                {
                    this._layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                this._layer2.RegisterState();
            }
        }

        // Token: 0x0600007A RID: 122 RVA: 0x000034B0 File Offset: 0x000016B0
        public void ShowCaravanOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer2.LoadMovie("PartyAICaravanOptions", new PartyAICaravanOptionsVM(settings, delegate ()
                {
                    this._layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                this._layer2.RegisterState();
            }
        }

        // Token: 0x0600007B RID: 123 RVA: 0x00003514 File Offset: 0x00001714
        public void ShowGarrisonOptionsInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer2.LoadMovie("PartyAIGarrisonOptions", new PartyAIGarrisonOptionsVM(settings, delegate ()
                {
                    this._layer2.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                this._layer2.RegisterState();
            }
        }

        // Token: 0x0600007C RID: 124 RVA: 0x00003578 File Offset: 0x00001778
        public void ShowOrderQueueInquiry(PartyAIClanPartySettings settings, Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer1.LoadSpriteCategories(new string[]
                {
                    "ui_partyscreen"
                });
                this._layer1.LoadMovie("PartyAIOrderQueue", new PartyAIOrderQueueVM(settings, delegate ()
                {
                    this._layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                    this._layer1.UnloadSpriteCategories();
                }));
                this._layer1.RegisterState();
            }
        }

        // Token: 0x0600007D RID: 125 RVA: 0x000035F4 File Offset: 0x000017F4
        public void ShowDefaultSettingsInquiry(Action callback)
        {
            bool flag = !this.CheckContext();
            if (!flag)
            {
                this._layer1.LoadMovie("PartyAIDefaultSettings", new PartyAIDefaultSettingsVM(delegate ()
                {
                    this._layer1.CloseQuery();
                    Action callback2 = callback;
                    if (callback2 != null)
                    {
                        callback2();
                    }
                }));
                this._layer1.RegisterState();
            }
        }

        private bool CheckContext()
        {
            bool flag = Campaign.Current == null;
            bool result;
            if (flag)
            {
                InformationManager.ShowInquiry(new InquiryData(new TextObject("{=oZrVNUOk}Error", null).ToString(), new TextObject("{=PAI0m3MyqBm}You must load a save game to use this menu.", null).ToString(), true, false, new TextObject("{=Y94H6XnK}Accept", null).ToString(), string.Empty, delegate ()
                {
                }, null, "", 0f, null, null, null), false, false);
                result = false;
            }
            else
            {
                bool flag2 = Mission.Current != null;
                result = !flag2;
            }
            return result;
        }
    }
}
