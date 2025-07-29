using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
}