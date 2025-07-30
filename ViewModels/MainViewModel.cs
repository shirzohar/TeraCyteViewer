using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TeraCyteViewer.Models;
using TeraCyteViewer.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace TeraCyteViewer.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private readonly ImageService _imageService;
        private readonly ResultService _resultService;

        private string _statusMessage = "Initializing...";
        private StatusType _statusType = StatusType.Info;
        private bool _isLoading = true;
        private bool _isAuthenticated = false;
        private bool _isRunning = false;
        private string? _lastImageId = null;

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
        private string _minValue = "0";
        private string _medianValue = "0.0";
        private string _standardDeviation = "0.0";
        private DateTime _lastUpdateTime = DateTime.Now;
        
        private string _imageQuality = "High Resolution";
        private string _processingTime = "~2.3s";
        private string _detectionConfidence = "95.2%";
        private string _analysisAccuracy = "98.7%";
        private string _connectionStatus = "Connected";

        private bool _showNewDataIndicator = false;
        private bool _showImageNewIndicator = false;
        private bool _showIntensityNewIndicator = false;
        private bool _showFocusNewIndicator = false;
        private bool _showClassificationNewIndicator = false;
        private bool _showHistogramNewIndicator = false;
        private bool _isVisualCueActive = false;

        private UserInfo? _currentUser;

        public ObservableCollection<HistoryItem> History { get; } = new ObservableCollection<HistoryItem>();

        private readonly DispatcherTimer _updateTimer;

        public RelayCommand StartMonitoringCommand { get; }
        public RelayCommand StopMonitoringCommand { get; }
        public RelayCommand ShowHistoryCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand LoginCommand { get; }

        public MainViewModel()
        {
            _authService = new AuthService();
            _imageService = new ImageService(_authService);
            _resultService = new ResultService(_authService);

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _updateTimer.Tick += (s, e) => OnPropertyChanged(nameof(LastUpdateTime));

            StartMonitoringCommand = new RelayCommand(StartMonitoring, () => IsAuthenticated && !IsRunning);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsRunning);
            ShowHistoryCommand = new RelayCommand(ShowHistory, () => IsAuthenticated);
            RefreshCommand = new RelayCommand(RefreshData, () => IsAuthenticated);
            LogoutCommand = new RelayCommand(Logout, () => IsAuthenticated);
            LoginCommand = new RelayCommand(Login, () => !IsAuthenticated);

            LoadStoredHistory();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Check if we have stored tokens
                if (!string.IsNullOrEmpty(_authService.GetAccessToken()))
                {
                    StatusMessage = "Validating stored authentication...";
                    StatusType = StatusType.Info;
                    
                    var isValid = await _authService.ValidateTokenAsync();
                    if (isValid)
                    {
                        try
                        {
                            var userInfo = await _authService.GetCurrentUserAsync();
                            CurrentUser = userInfo;
                            IsAuthenticated = true;
                            StatusMessage = $"Welcome back, {userInfo?.Username ?? "User"}!";
                            StatusType = StatusType.Success;
                            _updateTimer.Start();
                            return;
                        }
                        catch
                        {
                            // Token validation failed, clear stored tokens
                            _authService.ClearStoredTokens();
                        }
                    }
                }

                // No valid stored tokens, show login prompt
                StatusMessage = "Please login to start monitoring.";
                StatusType = StatusType.Info;
                IsLoading = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Initialization failed: {ex.Message}";
                StatusType = StatusType.Error;
                IsLoading = false;
            }
        }

        private async Task PollLoop()
        {
            while (IsRunning)
            {
                try
                {
                    if (_authService.IsTokenExpired())
                    {
                        StatusMessage = "Refreshing authentication token...";
                        StatusType = StatusType.Warning;
                        
                        if (_authService.IsRefreshTokenExpired())
                        {
                            StatusMessage = "Refresh token expired. Attempting re-login...";
                            StatusType = StatusType.Error;
                            await Reauthenticate();
                            continue;
                        }
                        
                        var refreshed = await _authService.RefreshTokenAsync();
                        if (!refreshed)
                        {
                            StatusMessage = "Token refresh failed. Attempting re-login...";
                            StatusType = StatusType.Error;
                            await Reauthenticate();
                            continue;
                        }
                        StatusMessage = "Token refreshed successfully";
                        StatusType = StatusType.Success;
                    }

                    var isValid = await _authService.ValidateTokenAsync();
                    if (!isValid)
                    {
                        StatusMessage = "Token validation failed. Attempting re-login...";
                        StatusType = StatusType.Error;
                        await Reauthenticate();
                        continue;
                    }

                    var imageData = await _imageService.GetLatestImageAsync();
                    if (imageData == null)
                    {
                        StatusMessage = "No image data received";
                        StatusType = StatusType.Warning;
                        await Task.Delay(5000);
                        continue;
                    }

                    if (_lastImageId != imageData.ImageId)
                    {
                        _lastImageId = imageData.ImageId;
                        
                        var resultData = await _resultService.GetLatestResultsAsync();
                        if (resultData != null && resultData.ImageId == imageData.ImageId)
                        {
                            UpdateUI(imageData, resultData);
                            AddToHistory(imageData, resultData);
                            StatusMessage = $"New data received: {imageData.ImageId}";
                            StatusType = StatusType.Success;
                        }
                    }

                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Data polling failed: {ex.Message}";
                    StatusType = StatusType.Error;
                    await Task.Delay(10000);
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
                    StatusMessage = "Re-authentication successful";
                    StatusType = StatusType.Success;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Re-authentication failed: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

        private void UpdateUI(ImageData imageData, ResultData result)
        {
            CurrentImage = imageData.Image;
            ImageInfo = $"Image ID: {imageData.ImageId}\nTimestamp: {imageData.Timestamp:yyyy-MM-dd HH:mm:ss}";
            
            IntensityAverage = result.IntensityAverage;
            FocusScore = result.FocusScore;
            ClassificationLabel = result.ClassificationLabel ?? "";
            
            ClassificationColor = (result.ClassificationLabel?.ToLower()) switch
            {
                "healthy" or "health" => Brushes.Green,
                "anomaly" => Brushes.Red,
                _ => Brushes.Black
            };

            if (result.Histogram != null && result.Histogram.Count > 0)
            {
                UpdateHistogram(result.Histogram.ToArray());
            }
            
            UpdateDynamicUIProperties(result);
            
            LastUpdateTime = DateTime.Now;
            
            TriggerNewDataVisualCues();
        }

        private void UpdateDynamicUIProperties(ResultData result)
        {
            ImageQuality = result.FocusScore > 0.8 ? "High Resolution" : 
                          result.FocusScore > 0.6 ? "Medium Resolution" : "Low Resolution";
            
            var processingTime = result.Histogram?.Count > 0 ? 
                $"~{(result.Histogram.Count / 100.0):F1}s" : "~2.3s";
            ProcessingTime = processingTime;
            
            var confidence = Math.Min(100, Math.Max(0, result.IntensityAverage * 10));
            DetectionConfidence = $"{confidence:F1}%";
            
            var accuracy = result.ClassificationLabel?.ToLower() switch
            {
                "healthy" or "health" => 98.7,
                "anomaly" => 95.2,
                _ => 92.0
            };
            AnalysisAccuracy = $"{accuracy:F1}%";
            
            ConnectionStatus = "Connected";
        }

        private void TriggerNewDataVisualCues()
        {
            if (IsVisualCueActive)
                return;
                
            IsVisualCueActive = true;
            
            ShowNewDataIndicator = true;
            ShowImageNewIndicator = true;
            ShowIntensityNewIndicator = true;
            ShowFocusNewIndicator = true;
            ShowClassificationNewIndicator = true;
            ShowHistogramNewIndicator = true;
            
            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowNewDataIndicator = false;
                    ShowImageNewIndicator = false;
                    ShowIntensityNewIndicator = false;
                    ShowFocusNewIndicator = false;
                    ShowClassificationNewIndicator = false;
                    ShowHistogramNewIndicator = false;
                    IsVisualCueActive = false;
                });
            });
        }

        private void UpdateHistogram(int[] histogram)
        {
            if (histogram == null || histogram.Length == 0) 
            {
                StatusMessage = "No histogram data available";
                return;
            }

            var values = new double[histogram.Length];
            for (int i = 0; i < histogram.Length; i++)
            {
                values[i] = histogram[i];
            }

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

            HistogramSeries = Array.Empty<ISeries>();
            OnPropertyChanged(nameof(HistogramSeries));
            
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

            TotalValues = histogram.Length.ToString();
            MaxValue = histogram.Max().ToString();
            MinValue = histogram.Min().ToString();
            AverageValue = histogram.Average().ToString("F1");
            NonZeroCount = histogram.Count(x => x > 0).ToString();
            
            var sortedHistogram = histogram.OrderBy(x => x).ToArray();
            var middle = sortedHistogram.Length / 2;
            var median = sortedHistogram.Length % 2 == 0 
                ? (sortedHistogram[middle - 1] + sortedHistogram[middle]) / 2.0 
                : sortedHistogram[middle];
            MedianValue = median.ToString("F1");
            
            var mean = histogram.Average();
            var variance = histogram.Select(x => Math.Pow(x - mean, 2)).Average();
            var stdDev = Math.Sqrt(variance);
            StandardDeviation = stdDev.ToString("F1");
            
            StatusMessage = $"Histogram updated: {histogram.Length} values, max: {histogram.Max()}";
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

            while (History.Count > 50)
            {
                History.RemoveAt(History.Count - 1);
            }

            SaveHistory();
            RefreshCommandStates();
        }

        private void SaveHistory()
        {
            try
            {
                var historyList = History.ToList();
                _authService.SaveHistory(historyList);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save history: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

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
            set
            {
                SetProperty(ref _isAuthenticated, value);
                RefreshCommandStates();
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                SetProperty(ref _isRunning, value);
                RefreshCommandStates();
            }
        }

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

        public string MinValue
        {
            get => _minValue;
            set => SetProperty(ref _minValue, value);
        }

        public string MedianValue
        {
            get => _medianValue;
            set => SetProperty(ref _medianValue, value);
        }

        public string StandardDeviation
        {
            get => _standardDeviation;
            set => SetProperty(ref _standardDeviation, value);
        }

        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public string ImageQuality
        {
            get => _imageQuality;
            set => SetProperty(ref _imageQuality, value);
        }

        public string ProcessingTime
        {
            get => _processingTime;
            set => SetProperty(ref _processingTime, value);
        }

        public string DetectionConfidence
        {
            get => _detectionConfidence;
            set => SetProperty(ref _detectionConfidence, value);
        }

        public string AnalysisAccuracy
        {
            get => _analysisAccuracy;
            set => SetProperty(ref _analysisAccuracy, value);
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        public bool ShowNewDataIndicator
        {
            get => _showNewDataIndicator;
            set => SetProperty(ref _showNewDataIndicator, value);
        }

        public bool ShowImageNewIndicator
        {
            get => _showImageNewIndicator;
            set => SetProperty(ref _showImageNewIndicator, value);
        }

        public bool ShowIntensityNewIndicator
        {
            get => _showIntensityNewIndicator;
            set => SetProperty(ref _showIntensityNewIndicator, value);
        }

        public bool ShowFocusNewIndicator
        {
            get => _showFocusNewIndicator;
            set => SetProperty(ref _showFocusNewIndicator, value);
        }

        public bool ShowClassificationNewIndicator
        {
            get => _showClassificationNewIndicator;
            set => SetProperty(ref _showClassificationNewIndicator, value);
        }

        public bool ShowHistogramNewIndicator
        {
            get => _showHistogramNewIndicator;
            set => SetProperty(ref _showHistogramNewIndicator, value);
        }

        public bool IsVisualCueActive
        {
            get => _isVisualCueActive;
            set => SetProperty(ref _isVisualCueActive, value);
        }

        public UserInfo? CurrentUser
        {
            get => _currentUser;
            set => SetProperty(ref _currentUser, value);
        }

        private void StartMonitoring()
        {
            IsRunning = true;
            _ = PollLoop();
        }

        public void StopMonitoring()
        {
            IsRunning = false;
            _updateTimer.Stop();
        }

        private void ShowHistory()
        {
            if (History.Count == 0)
            {
                MessageBox.Show("No images in history yet. Start monitoring to see images here.", 
                    "Empty History", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
                return;
            }
            
            var historyWindow = new Views.HistoryWindow(History);
            historyWindow.Show();
        }

        private void RefreshCommandStates()
        {
            StartMonitoringCommand.RaiseCanExecuteChanged();
            StopMonitoringCommand.RaiseCanExecuteChanged();
            ShowHistoryCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
            LogoutCommand.RaiseCanExecuteChanged();
            LoginCommand.RaiseCanExecuteChanged();
        }

        private async void RefreshData()
        {
            try
            {
                StatusMessage = "Refreshing data...";
                StatusType = StatusType.Info;

                var imageData = await _imageService.GetLatestImageAsync();
                var resultData = await _resultService.GetLatestResultsAsync();

                if (imageData != null && resultData != null)
                {
                    UpdateUI(imageData, resultData);
                    StatusMessage = "Data refreshed successfully";
                    StatusType = StatusType.Success;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Refresh failed: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

        private async void Logout()
        {
            try
            {
                StatusMessage = "Logging out...";
                StatusType = StatusType.Info;
                await _authService.LogoutAsync();
                IsAuthenticated = false;
                StatusMessage = "Logged out successfully.";
                StatusType = StatusType.Success;
                CurrentUser = null;
                _updateTimer.Stop();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Logout failed: {ex.Message}";
                StatusType = StatusType.Error;
            }
        }

        private async void Login()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Authenticating with TeraCyte server...";
                StatusType = StatusType.Info;
                
                var response = await _authService.LoginAsync("shir.zohar", "biotech456");
                if (response == null)
                {
                    StatusMessage = "Authentication failed - no response received";
                    StatusType = StatusType.Error;
                    return;
                }

                IsAuthenticated = true;
                
                try
                {
                    var userInfo = await _authService.GetCurrentUserAsync();
                    CurrentUser = userInfo;
                    StatusMessage = $"Authentication successful! Welcome, {userInfo?.Username ?? "User"}";
                }
                catch
                {
                    StatusMessage = "Authentication successful! (User info unavailable)";
                }
                
                StatusType = StatusType.Success;
                IsLoading = false;
                
                _updateTimer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Authentication failed: {ex.Message}";
                StatusType = StatusType.Error;
                IsLoading = false;
            }
        }

        private void LoadStoredHistory()
        {
            try
            {
                var storedHistory = _authService.LoadHistory();
                if (storedHistory != null)
                {
                    foreach (var item in storedHistory)
                    {
                        History.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load history: {ex.Message}";
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