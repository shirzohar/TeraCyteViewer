using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Views
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow(IEnumerable<HistoryItem> historyItems)
        {
            InitializeComponent();
            LoadHistory(historyItems);
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