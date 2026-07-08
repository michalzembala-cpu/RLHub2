using System.Collections.Generic;

namespace RLHub2.Models
{
    public class Season
    {
        public string Name { get; set; } = "";
        public List<string> Changes { get; set; } = new();
        public List<string> NewFeatures { get; set; } = new();
        public List<string> Rewards { get; set; } = new();
    }
}
