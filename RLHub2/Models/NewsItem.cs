using System;

namespace RLHub2.Models
{
    public class NewsItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Link { get; set; } = "";
        public DateTime PublishedDate { get; set; }
    }
}
