using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Views
{
    public partial class HistoryWindow : Window
    {
        private ObservableCollection<HistoryItem> _historyCollection;

        public HistoryWindow(ObservableCollection<HistoryItem> historyCollection)
        {
            InitializeComponent();
            _historyCollection = historyCollection;
            LoadHistory(historyCollection);
        }

        private void LoadHistory(IEnumerable<HistoryItem> historyItems)
        {
            var items = historyItems?.ToList() ?? new List<HistoryItem>();
            
            HistoryItemsControl.ItemsSource = items;
            
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
                    var itemToRemove = _historyCollection.FirstOrDefault(item => item.ImageId == imageId);
                    if (itemToRemove != null)
                    {
                        _historyCollection.Remove(itemToRemove);
                        LoadHistory(_historyCollection);
                    }
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_historyCollection.Count == 0)
            {
                MessageBox.Show("History is already empty.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete all {_historyCollection.Count} images from history?",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _historyCollection.Clear();
                LoadHistory(_historyCollection);
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