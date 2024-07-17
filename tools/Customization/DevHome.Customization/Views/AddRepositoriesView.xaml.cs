// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Threading.Tasks;
using DevHome.Common.Extensions;
using DevHome.Common.Services;
using DevHome.Customization.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.DevHome.SDK;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace DevHome.Customization.Views;

public sealed partial class AddRepositoriesView : UserControl
{
    public FileExplorerViewModel ViewModel
    {
        get;
    }

    public AddRepositoriesView()
    {
        this.InitializeComponent();
        ViewModel = Application.Current.GetService<FileExplorerViewModel>();
        PopulateProviderSelectorDropDown();
    }

    private void PopulateProviderSelectorDropDown()
    {
        var extensionService = Application.Current.GetService<IExtensionService>();
        var sourceControlExtensions = Task.Run(async () => await extensionService.GetInstalledExtensionsAsync(ProviderType.LocalRepository)).Result.ToList();
        sourceControlExtensions.Sort((a, b) => string.Compare(a.ExtensionDisplayName, b.ExtensionDisplayName, System.StringComparison.OrdinalIgnoreCase));
        sourceControlExtensions.ForEach((sourceControlExtension) =>
        {
            SourceControlProviderSelector.Items.Add(
                new MenuFlyoutItem()
                {
                    Text = sourceControlExtension.ExtensionDisplayName,
                    Command = ViewModel.SourceControlProviderSelection_Click(),
                    CommandParameter = sourceControlExtension,
                });
        });
    }

    public void SourceControlProviderSelection_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SourceControlProviderSelection_Click();
    }
}
