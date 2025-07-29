using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using TeraCyteViewer.Models;
using TeraCyteViewer.Services;
using TeraCyteViewer.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Net.Http;
using System.Linq; // Added for .Max() and .Average()
using System.Collections.Generic; // Added for .Count()

namespace TeraCyteViewer
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService = new AuthService();
        private ImageService? _imageService;
        private ResultService? _resultService;
        private string? _lastImageId = null;
        private bool _isRunning = true;
        private readonly List<HistoryItem> _history = new List<HistoryItem>();
        private const int MaxHistoryItems = 50; // Limit history to prevent memory issues

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            StartApp();
        }

        private void InitializeServices()
        {
            _imageService = new ImageService(_authService);
            _resultService = new ResultService(_authService);
        }

        private async void StartApp()
        {
            try
            {
                UpdateStatus("🔐 Authenticating with TeraCyte server...", StatusType.Info);
                
                var response = await _authService.LoginAsync("shir.zohar", "biotech456");
                if (response == null)
                {
                    UpdateStatus("❌ Authentication failed - no response received", StatusType.Error);
                    return;
                }

                UpdateStatus("✅ Authentication successful! Starting live monitoring...", StatusType.Success);
                
                // Hide loading overlay after successful authentication
                Dispatcher.Invoke(() => ImageLoadingOverlay.Visibility = Visibility.Collapsed);
                
                // Start the polling loop
                _ = Task.Run(() => PollLoop());
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Authentication error: {ex.Message}", StatusType.Error);
            }
        }

        private async Task PollLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // Check if services are initialized
                    if (_imageService == null || _resultService == null)
                    {
                        UpdateStatus("⚠️ Services not initialized", StatusType.Warning);
                        await Task.Delay(5000);
                        continue;
                    }

                    // Check if token needs refresh
                    if (_authService.IsTokenExpired())
                    {
                        UpdateStatus("🔄 Refreshing authentication token...", StatusType.Warning);
                        var refreshed = await _authService.RefreshTokenAsync();
                        if (!refreshed)
                        {
                            UpdateStatus("❌ Token refresh failed. Attempting re-login...", StatusType.Error);
                            await Reauthenticate();
                            continue;
                        }
                        UpdateStatus("✅ Token refreshed successfully", StatusType.Success);
                    }

                    // Get latest image
                    var imageData = await _imageService.GetLatestImageAsync();
                    if (imageData == null)
                    {
                        UpdateStatus("⚠️ No image data received", StatusType.Warning);
                        await Task.Delay(5000);
                        continue;
                    }

                    // Check if this is a new image
                    if (imageData.ImageId != _lastImageId)
                    {
                        UpdateStatus($"🆕 New image detected: {imageData.ImageId}", StatusType.Info);

                        // Get results for this image
                        var result = await _resultService.GetLatestResultsAsync();
                        if (result == null)
                        {
                            UpdateStatus("⚠️ No results data received", StatusType.Warning);
                            await Task.Delay(5000);
                            continue;
                        }

                        // Verify image and result match
                        if (result.ImageId != imageData.ImageId)
                        {
                            UpdateStatus("⚠️ Image and result IDs don't match, waiting for sync...", StatusType.Warning);
                            await Task.Delay(2000);
                            continue;
                        }

                        _lastImageId = imageData.ImageId;
                        
                        // Add to history
                        AddToHistory(imageData, result);
                        
                        // Update UI with new data
                        Dispatcher.Invoke(() => UpdateUI(imageData, result));
                        
                        UpdateStatus($"✅ Updated: {imageData.ImageId} | {result.ClassificationLabel}", StatusType.Success);
                    }
                    else
                    {
                        UpdateStatus($"⏳ Monitoring... Last update: {_lastImageId}", StatusType.Info);
                    }
                }
                catch (TaskCanceledException)
                {
                    UpdateStatus("⏱️ Request timeout, retrying...", StatusType.Warning);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("401"))
                {
                    UpdateStatus("🔐 Authentication expired, refreshing...", StatusType.Warning);
                    await Reauthenticate();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Error: {ex.Message}", StatusType.Error);
                }

                await Task.Delay(5000);
            }
        }

        private void AddToHistory(ImageData imageData, ResultData result)
        {
            var historyItem = new HistoryItem
            {
                ImageId = imageData.ImageId,
                Timestamp = imageData.Timestamp,
                Image = imageData.Image,
                Results = result
            };

            // Add to beginning of list (most recent first)
            _history.Insert(0, historyItem);

            // Limit history size
            if (_history.Count > MaxHistoryItems)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private async Task Reauthenticate()
        {
            try
            {
                var response = await _authService.LoginAsync("shir.zohar", "biotech456");
                if (response != null)
                {
                    UpdateStatus("✅ Re-authentication successful", StatusType.Success);
                }
                else
                {
                    UpdateStatus("❌ Re-authentication failed", StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ Re-authentication error: {ex.Message}", StatusType.Error);
            }
        }

        private void UpdateUI(ImageData imageData, ResultData result)
        {
            // Hide loading overlay
            ImageLoadingOverlay.Visibility = Visibility.Collapsed;
            
            // Show new data indicator
            ShowNewDataIndicator();
            
            // Update image with fade-in effect
            MicroscopeImage.Source = imageData.Image;
            
            // Update image info
            ImageInfoText.Text = $"ID: {imageData.ImageId} | {imageData.Timestamp:yyyy-MM-dd HH:mm:ss}";
            
            // Update results with color coding
            IntensityText.Text = $"{result.IntensityAverage:F2}";
            FocusText.Text = $"{result.FocusScore:F2}";
            ClassificationText.Text = result.ClassificationLabel;
            
            // Color code the classification
            switch (result.ClassificationLabel?.ToLower())
            {
                case "healthy":
                    ClassificationText.Foreground = System.Windows.Media.Brushes.Green;
                    break;
                case "anomaly":
                    ClassificationText.Foreground = System.Windows.Media.Brushes.Red;
                    break;
                default:
                    ClassificationText.Foreground = System.Windows.Media.Brushes.Orange;
                    break;
            }
            
            // Update histogram
            var histogramData = result.Histogram?.ToArray() ?? Array.Empty<int>();
            
            // Simple debug - show histogram count in status
            if (histogramData.Length > 0)
            {
                UpdateStatus($"✅ Updated: {imageData.ImageId} | {result.ClassificationLabel} | Histogram: {histogramData.Length} values", StatusType.Success);
            }
            else
            {
                UpdateStatus($"⚠️ Updated: {imageData.ImageId} | {result.ClassificationLabel} | No histogram data", StatusType.Warning);
            }
            
            UpdateHistogram(histogramData);
        }

        private void ShowNewDataIndicator()
        {
            // Create fade in/out animation
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                BeginTime = TimeSpan.FromMilliseconds(1500)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, NewDataIndicator);
            Storyboard.SetTargetProperty(fadeIn, new System.Windows.PropertyPath(UIElement.OpacityProperty));
            Storyboard.SetTarget(fadeOut, NewDataIndicator);
            Storyboard.SetTargetProperty(fadeOut, new System.Windows.PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(fadeIn);
            storyboard.Children.Add(fadeOut);
            storyboard.Begin();
        }

        private void UpdateStatus(string message, StatusType statusType)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = message;
                
                // Update status text color based on type
                switch (statusType)
                {
                    case StatusType.Success:
                        StatusText.Foreground = System.Windows.Media.Brushes.Green;
                        break;
                    case StatusType.Warning:
                        StatusText.Foreground = System.Windows.Media.Brushes.Orange;
                        break;
                    case StatusType.Error:
                        StatusText.Foreground = System.Windows.Media.Brushes.Red;
                        break;
                    case StatusType.Info:
                    default:
                        StatusText.Foreground = System.Windows.Media.Brushes.Black;
                        break;
                }
            });
        }

        private void UpdateHistogram(int[] histogram)
        {
            if (histogram == null || histogram.Length == 0)
            {
                // Show empty state
                HistogramChart.Series = new ISeries[]
                {
                    new ColumnSeries<int>
                    {
                        Values = new int[256]
                    }
                };
                
                TotalValuesText.Text = "0";
                MaxValueText.Text = "0";
                AverageText.Text = "0.0";
                NonZeroText.Text = "0";
                HistogramText.Visibility = Visibility.Visible;
                return;
            }

            // Update individual text elements
            TotalValuesText.Text = histogram.Length.ToString();
            MaxValueText.Text = histogram.Max().ToString();
            AverageText.Text = histogram.Average().ToString("F1");
            NonZeroText.Text = histogram.Count(x => x > 0).ToString();
            HistogramText.Visibility = Visibility.Visible;

            // Create simple histogram
            var series = new ColumnSeries<int>
            {
                Values = histogram
            };

            HistogramChart.Series = new ISeries[] { series };
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var historyWindow = new HistoryWindow(_history);
                historyWindow.Owner = this;
                historyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _isRunning = false;
            base.OnClosed(e);
        }
    }

    public enum StatusType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
