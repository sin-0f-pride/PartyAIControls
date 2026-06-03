using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace PartyAIControls.PrefabExtensions
{
    [ViewModelMixin]
    internal class InquiryElementVMMixin : BaseViewModelMixin<InquiryElementVM>
    {
        private readonly InquiryElementVM _vm;

        public InquiryElementVMMixin(InquiryElementVM vm) : base(vm)
        {
            _vm = vm;

            if (!MultiSelectionQueryPopupVMMixin.AddClanBanners) { return; }

            if (_vm.InquiryElement?.Identifier is PartyAIClanPartySettings settings)
            {
                Banner_9 = new BannerImageIdentifierVM(new Banner(settings.Hero?.ClanBanner.BannerCode), true);
            }

            if (_vm.InquiryElement?.Identifier is Hero hero)
            {
                Banner_9 = new BannerImageIdentifierVM(new Banner(hero.ClanBanner.BannerCode), true);
            }
        }

        [DataSourceProperty] public ImageIdentifierVM Banner_9 { get; private set; }
    }
}
