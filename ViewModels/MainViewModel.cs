using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TeraCyteViewer.Models;
using TeraCyteViewer.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;

namespace TeraCyteViewer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Services
        private readonly AuthService _authService;
        private readonly ImageService _imageService;
        private readonly ResultService _resultService;

        // Status properties
        private string _statusMessage = "Initializing...";
        private StatusType _statusType = StatusType.Info;
        private bool _isLoading = true;
        private bool _isAuthenticated = false;
        private bool _isRunning = false;
        private string? _lastImageId = null;

        // Data properties
        private BitmapImage? _currentImage;
        private string _imageInfo = "";
        private float _intensityAverage;
        private float _focusScore;
        private string _classificationLabel = "";
        private Brush _classificationColor = Brushes.Black;
        private ISeries[] _histogramSeries = Array.Empty<ISeries>();
        private string _totalValues = "0";
        private string _maxValue = "0";
        private string _averageValue = "0.0";
        private string _nonZeroCount = "0";

        // Collections
        public ObservableCollection<HistoryItem> History { get; } = new ObservableCollection<HistoryItem>();

        // Commands
        public RelayCommand StartMonitoringCommand { get; }
        public RelayCommand StopMonitoringCommand { get; }
        public RelayCommand ShowHistoryCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public MainViewModel()
        {
            // Initialize services
            _authService = new AuthService();
            _imageService = new ImageService(_authService);
            _resultService = new ResultService(_authService);

            // Initialize commands
            StartMonitoringCommand = new RelayCommand(StartMonitoring, () => !IsRunning);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsRunning);
            ShowHistoryCommand = new RelayCommand(ShowHistory);
            RefreshCommand = new RelayCommand(RefreshData);

            // Start authentication
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "🔐 Authenticating with TeraCyte server...";
                StatusType = StatusType.Info;
                
                var response = await _authService.LoginAsync("shir.zohar", "biotech456");
                if (response == null)
                {
                    StatusMessage = "❌ Authentication failed - no response received";
                    StatusType = StatusType.Error;
                    return;
                }

                IsAuthenticated = true;
                IsLoading = false;
                StatusMessage = "✅ Authentication successful! Ready to start monitoring.";
                StatusType = StatusType.Success;
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Authentication error: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

        private async Task PollLoop()
        {
            while (IsRunning)
            {
                try
                {
                    // Check if token needs refresh
                    if (_authService.IsTokenExpired())
                    {
                        StatusMessage = "🔄 Refreshing authentication token...";
                        StatusType = StatusType.Warning;
                        var refreshed = await _authService.RefreshTokenAsync();
                        if (!refreshed)
                        {
                            StatusMessage = "❌ Token refresh failed. Attempting re-login...";
                            StatusType = StatusType.Error;
                            await Reauthenticate();
                            continue;
                        }
                        StatusMessage = "✅ Token refreshed successfully";
                        StatusType = StatusType.Success;
                    }

                    // Get latest image
                    var imageData = await _imageService.GetLatestImageAsync();
                    if (imageData == null)
                    {
                        StatusMessage = "⚠️ No image data received";
                        StatusType = StatusType.Warning;
                        await Task.Delay(5000);
                        continue;
                    }

                    // Check if this is a new image
                    if (_lastImageId != imageData.ImageId)
                    {
                        _lastImageId = imageData.ImageId;
                        
                        // Get results for this image
                        var resultData = await _resultService.GetLatestResultsAsync();
                        if (resultData != null && resultData.ImageId == imageData.ImageId)
                        {
                            UpdateUI(imageData, resultData);
                            AddToHistory(imageData, resultData);
                            StatusMessage = $"✅ New data received: {imageData.ImageId}";
                            StatusType = StatusType.Success;
                        }
                    }

                    await Task.Delay(5000); // Poll every 5 seconds
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Error during polling: {ex.Message}";
                    StatusType = StatusType.Error;
                    await Task.Delay(10000); // Wait longer on error
                }
            }
        }

        private async Task Reauthenticate()
        {
            try
            {
                var response = await _authService.LoginAsync("shir.zohar", "biotech456");
                if (response != null)
                {
                    IsAuthenticated = true;
                    StatusMessage = "✅ Re-authentication successful";
                    StatusType = StatusType.Success;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Re-authentication failed: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

        private void UpdateUI(ImageData imageData, ResultData result)
        {
            CurrentImage = imageData.Image;
            ImageInfo = $"Image ID: {imageData.ImageId}\nTimestamp: {imageData.Timestamp:yyyy-MM-dd HH:mm:ss}";
            
            IntensityAverage = result.IntensityAverage;
            FocusScore = result.FocusScore;
            ClassificationLabel = result.ClassificationLabel;
            
            // Set classification color
            ClassificationColor = result.ClassificationLabel?.ToLower() switch
            {
                "healthy" => Brushes.Green,
                "anomaly" => Brushes.Red,
                _ => Brushes.Black
            };

            // Update histogram
            if (result.Histogram != null && result.Histogram.Count > 0)
            {
                UpdateHistogram(result.Histogram.ToArray());
            }
        }

        private void UpdateHistogram(int[] histogram)
        {
            if (histogram == null || histogram.Length == 0) 
            {
                StatusMessage = "⚠️ No histogram data available";
                return;
            }

            var values = new double[histogram.Length];
            for (int i = 0; i < histogram.Length; i++)
            {
                values[i] = histogram[i];
            }

            // Generate random colors for visual feedback
            var random = new Random();
            var colors = new[]
            {
                SKColors.Blue,
                SKColors.Green,
                SKColors.Red,
                SKColors.Orange,
                SKColors.Purple,
                SKColors.Teal,
                SKColors.Brown,
                SKColors.Pink
            };
            
            var fillColor = colors[random.Next(colors.Length)];
            var strokeColor = new SKColor(
                (byte)Math.Max(0, fillColor.Red - 50),
                (byte)Math.Max(0, fillColor.Green - 50),
                (byte)Math.Max(0, fillColor.Blue - 50)
            );

            // Clear the series first to force a refresh
            HistogramSeries = Array.Empty<ISeries>();
            OnPropertyChanged(nameof(HistogramSeries));
            
            // Set the new series
            HistogramSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(fillColor),
                    Stroke = new SolidColorPaint(strokeColor, 1)
                }
            };
            OnPropertyChanged(nameof(HistogramSeries));

            // Update statistics
            TotalValues = histogram.Length.ToString();
            MaxValue = histogram.Max().ToString();
            AverageValue = histogram.Average().ToString("F1");
            NonZeroCount = histogram.Count(x => x > 0).ToString();
            
            StatusMessage = $"📊 Histogram updated: {histogram.Length} values, max: {histogram.Max()}, color: {fillColor}";
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

            History.Insert(0, historyItem);

            // Keep only the last 50 items
            while (History.Count > 50)
            {
                History.RemoveAt(History.Count - 1);
            }
        }

        // Status properties
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public StatusType StatusType
        {
            get => _statusType;
            set => SetProperty(ref _statusType, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set => SetProperty(ref _isAuthenticated, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        // Data properties
        public BitmapImage? CurrentImage
        {
            get => _currentImage;
            set => SetProperty(ref _currentImage, value);
        }

        public string ImageInfo
        {
            get => _imageInfo;
            set => SetProperty(ref _imageInfo, value);
        }

        public float IntensityAverage
        {
            get => _intensityAverage;
            set => SetProperty(ref _intensityAverage, value);
        }

        public float FocusScore
        {
            get => _focusScore;
            set => SetProperty(ref _focusScore, value);
        }

        public string ClassificationLabel
        {
            get => _classificationLabel;
            set => SetProperty(ref _classificationLabel, value);
        }

        public Brush ClassificationColor
        {
            get => _classificationColor;
            set => SetProperty(ref _classificationColor, value);
        }

        public ISeries[] HistogramSeries
        {
            get => _histogramSeries;
            set => SetProperty(ref _histogramSeries, value);
        }

        public string TotalValues
        {
            get => _totalValues;
            set => SetProperty(ref _totalValues, value);
        }

        public string MaxValue
        {
            get => _maxValue;
            set => SetProperty(ref _maxValue, value);
        }

        public string AverageValue
        {
            get => _averageValue;
            set => SetProperty(ref _averageValue, value);
        }

        public string NonZeroCount
        {
            get => _nonZeroCount;
            set => SetProperty(ref _nonZeroCount, value);
        }

        // Command methods
        private void StartMonitoring()
        {
            IsRunning = true;
            StatusMessage = "Starting monitoring...";
            StatusType = StatusType.Info;
            _ = PollLoop();
        }

        public void StopMonitoring()
        {
            IsRunning = false;
            StatusMessage = "Monitoring stopped";
            StatusType = StatusType.Warning;
        }

        private void ShowHistory()
        {
            StatusMessage = "Opening history...";
            StatusType = StatusType.Info;
            
            var historyWindow = new Views.HistoryWindow(History);
            historyWindow.Show();
        }

        private async void RefreshData()
        {
            try
            {
                StatusMessage = "🔄 Refreshing data...";
                StatusType = StatusType.Info;

                // Check if token needs refresh
                if (_authService.IsTokenExpired())
                {
                    var refreshed = await _authService.RefreshTokenAsync();
                    if (!refreshed)
                    {
                        await Reauthenticate();
                        return;
                    }
                }

                // Get latest image
                var imageData = await _imageService.GetLatestImageAsync();
                if (imageData == null)
                {
                    StatusMessage = "⚠️ No image data received";
                    StatusType = StatusType.Warning;
                    return;
                }

                // Get results for this image
                var resultData = await _resultService.GetLatestResultsAsync();
                if (resultData != null && resultData.ImageId == imageData.ImageId)
                {
                    UpdateUI(imageData, resultData);
                    AddToHistory(imageData, resultData);
                    StatusMessage = $"✅ Data refreshed: {imageData.ImageId}";
                    StatusType = StatusType.Success;
                }
                else
                {
                    StatusMessage = "⚠️ No matching results found";
                    StatusType = StatusType.Warning;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Refresh error: {ex.Message}";
                StatusType = StatusType.Error;
            }
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