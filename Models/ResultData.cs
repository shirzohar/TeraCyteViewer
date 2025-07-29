using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeraCyteViewer.Models
{
    public class ResultData
    {
        [JsonProperty("image_id")]
        public string? ImageId { get; set; }

        [JsonProperty("intensity_average")]
        public float IntensityAverage { get; set; }

        [JsonProperty("focus_score")]
        public float FocusScore { get; set; }

        [JsonProperty("classification_label")]
        public string? ClassificationLabel { get; set; }

        [JsonProperty("histogram")]
        public List<int>? Histogram { get; set; }
    }
}
