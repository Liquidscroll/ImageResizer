using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageResizer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OptionsPage : Page
    {
        private ObservableCollection<StorageFile> selectedFiles = [];
        private MainWindow mainWindow;
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPage"/> class.
        /// </summary>
        public OptionsPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// Handles the click event to allow the user to select multiple image files.
        /// Filters for `.jpg` and `.png` file types.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void SelectImagesClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files != null)
            {
                selectedFiles.Clear();
                foreach (var file in files)
                {
                    selectedFiles.Add(file);
                }
            }
        }
        /// <summary>
        /// Handles the click event to allow the user to select a folder containing images.
        /// Filters for `.jpg` and `.png` files within the selected folder.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void SelectDirectoryClick(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFolder folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                selectedFiles.Clear();
                var files = await folder.GetFilesAsync();
                var imageFiles = files.Where(file =>
                    file.FileType.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    file.FileType.Equals(".png", StringComparison.OrdinalIgnoreCase));


                foreach (var file in imageFiles)
                {
                    selectedFiles.Add(file);
                }
            }
        }
        /// <summary>
        /// Handles the click event to start resizing the selected images based on user-provided dimensions.
        /// Validates the input and navigates to the <see cref="BatchCroppingPage"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void StartResizingClick(object sender, RoutedEventArgs e)
        {
            if (uint.TryParse(TargetWidthTextBox.Text, out uint targetWidth) &&
                uint.TryParse(TargetHeightTextBox.Text, out uint targetHeight) &&
                selectedFiles.Any())
            {
                var parameter = new BatchCroppingPageNavigationParameter
                {
                    SelectedFiles = selectedFiles,
                    TargetWidth = targetWidth,
                    TargetHeight = targetHeight
                };

                this.Frame.Navigate(typeof(BatchCroppingPage), parameter);
            }
            else
            {
                var dialog = new ContentDialog()
                {
                    Title = "Invalid Input",
                    Content = "Please ensure you've selected images and entered valid dimensions.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }
        /// <summary>
        /// Handles the click event to close the application window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            App.m_window.Close();
        }
    }
    public class BatchCroppingPageNavigationParameter
    {
        public ObservableCollection<StorageFile> SelectedFiles { get; set; }
        public uint TargetWidth { get; set; }
        public uint TargetHeight { get; set; }
    }
}
