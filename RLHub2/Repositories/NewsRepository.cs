using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RLHub2.Interfaces;
using RLHub2.Models;

namespace RLHub2.Repositories
{
    public class NewsRepository : INewsRepository
    {
        public async Task<List<NewsItem>> GetNewsAsync()
        {
            await Task.Delay(50);

            var list = new List<NewsItem>();

            list.Add(new NewsItem
            {
                Title = "RLCS News (Repository OK)",
                Description = "",
                Category = "Esports",
                PublishedDate = DateTime.UtcNow
            });

            list.Add(new NewsItem
            {
                Title = "Rocket League Updates",
                Description = "",
                Category = "Updates",
                PublishedDate = DateTime.UtcNow
            });

            return list;
        }
    }
}