using System.Threading.Tasks;
using RLHub2.Models;

namespace RLHub2.Services
{
    public interface IProfileService
    {
        Task<Profile> GetProfileAsync(string nick);
    }
}
