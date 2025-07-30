using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TeraCyteViewer.Models;
using System;

namespace TeraCyteViewer.Views
{
    public partial class HistoryWindow : Window
    {
        private ObservableCollection<HistoryItem> _historyCollection;

        public HistoryWindow(ObservableCollection<HistoryItem> historyCollection)
        {
            InitializeComponent();
            _historyCollection = historyCollection;
            HistoryItemsControl.ItemsSource = _historyCollection;
            UpdateHistoryDisplay();
        }

        private void UpdateHistoryDisplay()
        {
            var items = _historyCollection?.ToList() ?? new List<HistoryItem>();
            
            if (items.Any())
            {
                HistoryCountText.Text = $"{items.Count} image(s) in history";
            }
            else
            {
                HistoryCountText.Text = "No history available";
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string imageId)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete image '{imageId}' from history?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var itemToRemove = _historyCollection.FirstOrDefault(x => x.ImageId == imageId);
                    if (itemToRemove != null)
                    {
                        _historyCollection.Remove(itemToRemove);
                        SaveHistory();
                        UpdateHistoryDisplay();
                    }
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all history?",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _historyCollection.Clear();
                SaveHistory();
                UpdateHistoryDisplay();
            }
        }

        private void SaveHistory()
        {
            try
            {
                var authService = new Services.AuthService();
                var historyList = _historyCollection.ToList();
                authService.SaveHistory(historyList);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ClassificationColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string classificationLabel)
            {
                return classificationLabel?.ToLower() switch
                {
                    "healthy" or "health" => Brushes.Green,
                    "anomaly" => Brushes.Red,
                    _ => Brushes.Black
                };
            }
            
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}