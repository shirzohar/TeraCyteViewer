using System.Threading.Tasks;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public interface IImageService
    {
        Task<ImageData?> GetLatestImageAsync();
    }
} 