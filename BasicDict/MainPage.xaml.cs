using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238
using NStarDict;

namespace BasicDict
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            using (var dict = new StarDict(@"E:\Downloads\dict\stardict-freedict-eng-nld-2.4.2\dictd_www.freedict.de_eng-nld"))
            {
                await dict.Init();
            }

        }

        private async void OpenFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                CommitButtonText = "Select",
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".idx");
            picker.FileTypeFilter.Add(".dz");
            picker.FileTypeFilter.Add(".ifo");
            picker.SettingsIdentifier = "FolderPicker";
            var folder = await picker.PickSingleFolderAsync();
            StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            var abstractFolder = new PCLStorage.WinRTFolder(folder);
            using (var dict = new StarDict(abstractFolder, @"dictd_www.freedict.de_eng-nld"))
            {
                await dict.Init();
            }
        }
    }
}
