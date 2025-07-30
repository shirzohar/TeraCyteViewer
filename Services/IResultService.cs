using System.Threading.Tasks;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public interface IResultService
    {
        Task<ResultData?> GetLatestResultsAsync();
    }
} 