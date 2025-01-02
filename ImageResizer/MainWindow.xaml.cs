using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageResizer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets the initial size of the window and navigates to the <see cref="OptionsPage"/>.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(600, 400));
            MainFrame.Navigate(typeof(OptionsPage), this);
        }
        /// <summary>
        /// Displays a dialog to indicate that all images have been processed.
        /// </summary>
        private void ShowCompletionDialog()
        {
            var dialog = new ContentDialog()
            {
                Title = "Processing Complete.",
                Content = "All images have been processed.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            _ = dialog.ShowAsync();
        }
        /// <summary>
        /// Handles the click event to close the main window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExitClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }


}
