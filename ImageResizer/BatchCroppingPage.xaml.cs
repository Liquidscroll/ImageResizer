using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ImageResizer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BatchCroppingPage : Page
    {
        public event EventHandler ProcessingCompleted;

        private ObservableCollection<ImageFileWrapper> imageFiles;
        private int currentImageIndex = 0;
        private uint targetWidth, targetHeight;

        // Cropping-specific fields
        private IRandomAccessStream imageStream;
        private WriteableBitmap bitmap;
        private double imageScale = 1.0;
        private bool isDragging = false;
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCroppingPage"/> class.
        /// </summary>
        public BatchCroppingPage()
        {
            this.InitializeComponent();
        }

        private async void ImageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageListView.SelectedIndex >= 0 && ImageListView.SelectedIndex < imageFiles.Count)
            {
                currentImageIndex = ImageListView.SelectedIndex;
                ImageListView.ScrollIntoView(ImageListView.SelectedItem);
                await LoadCurrentImage();
            }
        }
        /// <summary>
        /// Loads the currently selected image into the cropping interface.
        /// </summary>
        private async Task LoadCurrentImage()
        {
            if (currentImageIndex < imageFiles.Count)
            {
                var currentFile = imageFiles[currentImageIndex].File;
                using (IRandomAccessStream fileStream = await currentFile.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                    uint origWidth = decoder.PixelWidth;
                    uint origHeight = decoder.PixelHeight;

                    double origAspectRatio = (double)origWidth / origHeight;

                    imageStream = await ResizeImageAsync(currentFile, GetScaledDimensions(origWidth, origHeight), false);
                    await LoadImageForCropping(imageStream);
                    ImageListView.SelectedIndex = currentImageIndex;
                    ImageListView.ScrollIntoView(ImageListView.SelectedItem);
                }
            }
            else
            {
                // All images processed
                ProcessingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Calculates the scaled dimensions of the image based on the target dimensions while maintaining aspect ratio.
        /// </summary>
        /// <param name="origWidth">The original width of the image.</param>
        /// <param name="origHeight">The original height of the image.</param>
        /// <returns>A tuple containing the scaled width and height.</returns>
        private (uint Width, uint Height) GetScaledDimensions(uint origWidth, uint origHeight)
        {
            double origAspectRatio = (double)origWidth / origHeight;
            double targetAspectRatio = (double)targetWidth / targetHeight;

            if (Math.Abs(origAspectRatio - targetAspectRatio) < 0.01)
            {
                // If aspect ratios match, return target dimensions
                return (targetWidth, targetHeight);
            }
            else
            {
                // Calculate scaling to fit the target dimensions
                double widthScale = (double)targetWidth / origWidth;
                double heightScale = (double)targetHeight / origHeight;
                double scale = Math.Max(widthScale, heightScale);

                return ((uint)(origWidth * scale), (uint)(origHeight * scale));
            }
        }
        /// <summary>
        /// Displays the selected image in the cropping interface, scaling it to fit the screen.
        /// </summary>
        /// <param name="stream">The image data stream.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task LoadImageForCropping(IRandomAccessStream stream)
        {
            bitmap = new WriteableBitmap(1, 1);
            await bitmap.SetSourceAsync(stream);

            DisplayedImage.Source = bitmap;

            double maxWidth = this.ActualWidth * 0.8;
            double maxHeight = this.ActualHeight * 0.8;

            if (maxWidth == 0 || maxHeight == 0)
            {
                maxWidth = 800 * 0.8;
                maxHeight = 600 * 0.8;
            }

            double scaleX = maxWidth / bitmap.PixelWidth;
            double scaleY = maxHeight / bitmap.PixelHeight;
            imageScale = Math.Min(Math.Min(scaleX, scaleY), 1.0);

            DisplayedImage.Width = bitmap.PixelWidth * imageScale;
            DisplayedImage.Height = bitmap.PixelHeight * imageScale;
            OverlayCanvas.Width = DisplayedImage.Width;
            OverlayCanvas.Height = DisplayedImage.Height;

            // Set initial crop size, scaled
            CropRectangle.Width = targetWidth * imageScale;
            CropRectangle.Height = targetHeight * imageScale;

            // Position crop rectangle at the center
            Canvas.SetLeft(CropRectangle, (OverlayCanvas.Width - CropRectangle.Width) / 2);
            Canvas.SetTop(CropRectangle, (OverlayCanvas.Height - CropRectangle.Height) / 2);

            // Enable manipulation
            CropRectangle.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
            CropRectangle.ManipulationStarted += CropRectManipulationStarted;
            CropRectangle.ManipulationDelta += CropRectManipulationDelta;
            CropRectangle.ManipulationCompleted += CropRectManipulationCompleted;


            CropRectangle.StrokeDashArray = new DoubleCollection { 1, 2 };
        }
        /// <summary>
        /// Handles the manipulation start event for the crop rectangle.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CropRectManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            isDragging = true;
        }
        /// <summary>
        /// Handles the manipulation delta event to adjust the crop rectangle's position.
        /// Ensures the rectangle stays within the bounds of the image.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CropRectManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (isDragging)
            {
                double newX = Canvas.GetLeft(CropRectangle) + (e.Delta.Translation.X / 10);
                double newY = Canvas.GetTop(CropRectangle) + (e.Delta.Translation.Y / 10);

                // Ensure cropping rectangle stays within image boundaries
                newX = Math.Max(0, Math.Min(newX, OverlayCanvas.Width - CropRectangle.Width));
                newY = Math.Max(0, Math.Min(newY, OverlayCanvas.Height - CropRectangle.Height));

                Canvas.SetLeft(CropRectangle, newX);
                Canvas.SetTop(CropRectangle, newY);
            }
        }

        private void CropRectManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            isDragging = false;
        }
        /// <summary>
        /// Handles the click event to crop the current image and proceed to the next one.
        /// Updates the button to 'exit' when all images are processed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void CropAndNextClick(object sender, RoutedEventArgs e)
        {
            if ((string)CropAndNextButton.Content == "Exit")
            {
                App.Current.Exit();
                return;
            }

            await CropAndSaveCurrentImage();

            // Move to next image
            currentImageIndex++;
            if (currentImageIndex >= imageFiles.Count)
            {
                // All images processed, change button text to 'Exit'
                CropAndNextButton.Content = "Exit";
            }
            else
            {
                await LoadCurrentImage();
            }
        }

        private async Task CropAndSaveCurrentImage()
        {
            if (currentImageIndex >= imageFiles.Count)
            {
                // All images processed - this should not be triggered here now,
                // because we will handle this scenario after incrementing currentImageIndex.
                return;
            }
            var currentFile = imageFiles[currentImageIndex];

            // Calculate crop coordinates
            double x = Canvas.GetLeft(CropRectangle) / imageScale;
            double y = Canvas.GetTop(CropRectangle) / imageScale;

            imageStream.Seek(0);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);

            BitmapTransform transform = new BitmapTransform();
            transform.Bounds = new BitmapBounds()
            {
                X = (uint)x,
                Y = (uint)y,
                Width = targetWidth,
                Height = targetHeight
            };

            PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Straight,
                transform,
                ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);

            byte[] pixels = pixelData.DetachPixelData();

            StorageFolder parentFolder = await currentFile.File.GetParentAsync();
            StorageFolder resizedFolder = await parentFolder.CreateFolderAsync("resized", CreationCollisionOption.OpenIfExists);
            StorageFile croppedFile = await resizedFolder.CreateFileAsync(
                $"{currentFile.DisplayName}-{targetWidth}x{targetHeight}.png",
                CreationCollisionOption.GenerateUniqueName);

            using (IRandomAccessStream destStream = await croppedFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destStream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    targetWidth,
                    targetHeight,
                    decoder.DpiX,
                    decoder.DpiY,
                    pixels);

                await encoder.FlushAsync();
            }

            currentFile.IsProcessed = true;
        }
        /// <summary>
        /// Resizes the specified image to the given dimensions.
        /// Optionally saves the resized image.
        /// </summary>
        /// <param name="file">The image file to resize.</param>
        /// <param name="dimensions">The target dimensions.</param>
        /// <param name="save">Indicates whether to save the resized image.</param>
        /// <returns>The resized image stream, or null if the image was saved.</returns>
        private async Task<IRandomAccessStream> ResizeImageAsync(StorageFile file, (uint Width, uint Height) dimensions, bool save = true)
        {
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                BitmapTransform transform = new BitmapTransform()
                {
                    ScaledHeight = dimensions.Height,
                    ScaledWidth = dimensions.Width,
                    InterpolationMode = BitmapInterpolationMode.Fant
                };

                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                byte[] pixels = pixelData.DetachPixelData();

                InMemoryRandomAccessStream resizedStream = new InMemoryRandomAccessStream();

                if (save)
                {
                    StorageFolder parentFolder = await file.GetParentAsync();
                    StorageFolder resizedFolder = await parentFolder.CreateFolderAsync("resized", CreationCollisionOption.OpenIfExists);

                    StorageFile resizedFile = await resizedFolder.CreateFileAsync(
                        $"{file.DisplayName}-{dimensions.Width}x{dimensions.Height}.png",
                        CreationCollisionOption.GenerateUniqueName);

                    using (IRandomAccessStream destStream = await resizedFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, destStream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Straight,
                            dimensions.Width,
                            dimensions.Height,
                            decoder.DpiX,
                            decoder.DpiY,
                            pixels);

                        await encoder.FlushAsync();
                    }
                    return null;
                }
                else
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resizedStream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        dimensions.Width,
                        dimensions.Height,
                        decoder.DpiX,
                        decoder.DpiY,
                        pixels);

                    await encoder.FlushAsync();

                    resizedStream.Seek(0);
                    return resizedStream;
                }
            }
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is BatchCroppingPageNavigationParameter parameter)
            {
                targetWidth = parameter.TargetWidth;
                targetHeight = parameter.TargetHeight;
                imageFiles = new ObservableCollection<ImageFileWrapper>();
                ImageListView.ItemsSource = imageFiles;

                var semaphore = new SemaphoreSlim(5);

                var processingTasks = parameter.SelectedFiles.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                        {
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);
                            uint origWidth = decoder.PixelWidth;
                            uint origHeight = decoder.PixelHeight;

                            (uint scaledWidth, uint scaledHeight) = GetScaledDimensions(origWidth, origHeight);

                            if (origWidth == parameter.TargetWidth && origHeight == parameter.TargetHeight)
                            {
                                // File matches target size, copy it without changes
                                await CopyFileWithoutChange(file, parameter.TargetWidth, parameter.TargetHeight);
                            }
                            else if (scaledWidth == parameter.TargetWidth && scaledHeight == parameter.TargetHeight)
                            {
                                // File can be resized without manual intervention
                                await ResizeImageAsync(file, (parameter.TargetWidth, parameter.TargetHeight), save: true);
                            }
                            else
                            {
                                // File requires user intervention, add to the UI list
                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    imageFiles.Add(new ImageFileWrapper(file));

                                    // Load the first image if the list was previously empty
                                    if (imageFiles.Count == 1)
                                    {
                                        _ = LoadCurrentImage();
                                    }
                                });
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(processingTasks);

                if (!imageFiles.Any())
                {
                    ProcessingCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        /// <summary>
        /// Copies the file to a resized folder without changing its dimensions.
        /// </summary>
        /// <param name="file">The image file to copy.</param>
        /// <param name="width">The target width for naming.</param>
        /// <param name="height">The target height for naming.</param>
        private async Task CopyFileWithoutChange(StorageFile file, uint width, uint height)
        {
            StorageFolder parent = await file.GetParentAsync();
            StorageFolder dest = await parent.CreateFolderAsync("resized", CreationCollisionOption.OpenIfExists);
            string newFilename = $"{file.DisplayName}-{width}x{height}{file.FileType}";

            await file.CopyAsync(dest, newFilename, NameCollisionOption.GenerateUniqueName);
        }
        private void ColorPickerButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void CropRectangleColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            // Update the crop rectangle stroke color
            if (CropRectangle != null)
            {
                CropRectangle.Stroke = new SolidColorBrush(args.NewColor);

            }
        }
    }

    public class ImageFileWrapper : INotifyPropertyChanged
    {
        private StorageFile _file;
        private bool _isProcessed;

        public StorageFile File => _file;

        public bool IsProcessed
        {
            get => _isProcessed;
            set
            {
                _isProcessed = value;
                OnPropertyChanged(nameof(IsProcessed));
            }
        }

        public string DisplayName => _file.DisplayName;

        public ImageFileWrapper(StorageFile file)
        {
            _file = file;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
