using System.Collections.ObjectModel;
using fasterLegit.Domain.Abstractions;
using fasterLegit.Domain.Customize.Models;
using fasterLegit.Domain.UI;
using fasterLegit.Services.System;
using fasterLegit.UI.ViewModels.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wpf.Ui.Controls;

namespace fasterLegit.Test.UI.ViewModels;

public sealed class CustomizeCategoryViewModelTests
{
    private sealed class FakeCategory : ICustomizeCategory
    {
        public string Name => "Test";
        public string Description => "Test";
        public SymbolRegular Icon { get; init; } = SymbolRegular.Settings24;
        public CustomizeOrder Order { get; init; }
        public ObservableCollection<ICustomizeSetting> Features { get; init; } = [];
    }

    private sealed class FakeRegistryWatcher : IRegistryWatcher
    {
        // The view model only subscribes to this; the fake never raises it, so it needs no
        // backing field and no warning about one.
        public event EventHandler<string>? RegistryKeyChanged
        {
            add { }
            remove { }
        }

        public void Watch(string registryPath) { }

        public void Unwatch(string registryPath) { }

        public void Dispose() { }
    }

    [Fact]
    public void SettingCountNotifiesHeader()
    {
        var vm = new CustomizeCategoryViewModel(
            new FakeCategory(),
            NullLoggerFactory.Instance,
            new FakeRegistryWatcher(),
            new SystemInfoService(NullLogger<SystemInfoService>.Instance)
        );

        var notified = new List<string>();
        vm.PropertyChanged += (_, e) => notified.Add(e.PropertyName!);

        vm.UnsupportedSettingsCount = 1;

        Assert.Contains(nameof(CustomizeCategoryViewModel.UnsupportedSettingsHeader), notified);
        Assert.Contains("(1)", vm.UnsupportedSettingsHeader);
    }
}
