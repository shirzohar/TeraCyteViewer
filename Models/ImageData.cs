using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TeraCyteViewer.Models
{
    public class ImageData
    {
        public string? ImageId { get; set; }
        public DateTime Timestamp { get; set; }
        public BitmapImage? Image { get; set; }
    }

    public class HistoryItem
    {
        public string? ImageId { get; set; }
        public DateTime Timestamp { get; set; }
        public BitmapImage? Image { get; set; }
        public ResultData? Results { get; set; }
    }
}
