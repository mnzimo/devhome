﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Settings.TelemetryEvents;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using Windows.System;

namespace DevHome.ExtensionLibrary.ViewModels;

public partial class InstalledExtensionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _extensionUniqueId;

    [ObservableProperty]
    private bool _hasSettingsProvider;

    private bool _isExtensionEnabled;

    public bool IsExtensionEnabled
    {
        get => _isExtensionEnabled;

        set
        {
            if (_isExtensionEnabled != value)
            {
                Task.Run(() =>
                {
                    var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
                    return localSettingsService.SaveSettingAsync(ExtensionUniqueId + "-ExtensionDisabled", !value);
                }).Wait();

                _isExtensionEnabled = value;

                var extensionService = Application.Current.GetService<IExtensionService>();
                if (_isExtensionEnabled)
                {
                    extensionService.EnableExtension(ExtensionUniqueId);
                }
                else
                {
                    extensionService.DisableExtension(ExtensionUniqueId);
                }
            }
        }
    }

    public InstalledExtensionViewModel(string displayName, string extensionUniqueId, bool hasSettingsProvider)
    {
        _displayName = displayName;
        _extensionUniqueId = extensionUniqueId;
        _hasSettingsProvider = hasSettingsProvider;

        _isExtensionEnabled = GetIsExtensionEnabled();
    }

    private bool GetIsExtensionEnabled()
    {
        var isDisabled = Task.Run(() =>
        {
            var localSettingsService = Application.Current.GetService<ILocalSettingsService>();
            return localSettingsService.ReadSettingAsync<bool>(ExtensionUniqueId + "-ExtensionDisabled");
        }).Result;
        return !isDisabled;
    }

    [RelayCommand]
    private void NavigateSettings()
    {
        TelemetryFactory.Get<ITelemetry>().Log("ExtensionsSettings_Navigate_Event", LogLevel.Critical, new NavigateToExtensionSettingsEvent("InstalledExtensionViewModel"));

        var navigationService = Application.Current.GetService<INavigationService>();
        navigationService.NavigateTo(typeof(ExtensionSettingsViewModel).FullName!, ExtensionUniqueId);
    }
}

public partial class InstalledPackageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _publisher;

    [ObservableProperty]
    private string _packageFamilyName;

    [ObservableProperty]
    private DateTimeOffset _installedDate;

    [ObservableProperty]
    private PackageVersion _version;

    public ObservableCollection<InstalledExtensionViewModel> InstalledExtensionsList { get; set; }

    public InstalledPackageViewModel(string title, string publisher, string packageFamilyName, DateTimeOffset installedDate, PackageVersion version)
    {
        _title = title;
        _publisher = publisher;
        _packageFamilyName = packageFamilyName;
        _installedDate = installedDate;
        _version = version;
        InstalledExtensionsList = new();
    }

    [RelayCommand]
    public async Task LaunchStoreButton(string packageId)
    {
        var linkString = $"ms-windows-store://pdp/?ProductId={packageId}&mode=mini";
        await Launcher.LaunchUriAsync(new(linkString));
    }

    [RelayCommand]
    public async Task UninstallButton()
    {
        await Launcher.LaunchUriAsync(new("ms-settings:appsfeatures"));
    }

    public string GeneratePackageDetails(PackageVersion version, string publisher, DateTimeOffset installedDate)
    {
        var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader("DevHome.ExtensionLibrary.pri", "DevHome.ExtensionLibrary/Resources");
        var versionLabel = resourceLoader.GetString("Version");
        var lastUpdatedLabel = resourceLoader.GetString("LastUpdated");

        var versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

        return $"{versionLabel} {versionString} | {publisher} | {lastUpdatedLabel} {installedDate.LocalDateTime}";
    }
}
