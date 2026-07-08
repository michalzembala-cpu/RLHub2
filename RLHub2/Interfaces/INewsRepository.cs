using System.Collections.Generic;
using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Interfaces
{
    public interface INewsRepository
    {
        Task<List<NewsItem>> GetNewsAsync();
    }
}