using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Sbz.Application.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Sbz.Application.Common.Interfaces
{
    public interface IFileService
    {
        Task<string> AddFileToServer(IFormFile files, UploadType fileType, string fileNameWithoutExt = null);
        Task<string> AddImageToServer(IFormFile files, UploadType fileType, string fileNameWithoutExt, params ImageSize[] thumbSize);
        Task<string> AddImageToServer(string base64Image, UploadType fileType, ImageSize[] thumbSize);
        Task ImageResizerAsync(string inputImagePath, string outputImagePath, ImageSize size);
        void ImageResizer(string inputImagePath, string outputImagePath, ImageSize size);
        void ImageResizer(string inputImagePath, string outputImagePath, ImageSize[] size);
        void ImageOptimizer(string imagePath);
        void DeleteImage(UploadType fileType, string fileName);
        void DeleteFile(UploadType fileType, string fileName);        
        Task ProcessImageAsync(string filePath, string base64Image);        
        void ProcessImage(string filePath, string base64Image);
        void ProcessImage(string filePath, string base64Image, params ImageSize[] sizes);
        void ProcessImage(string filePath, string base64Image, string[] sizes);
        string GenerateImageName(string uniqName, string fileName, string[] thumbnailSizes = null);
        string GenerateSliderImageName(string imageName, bool isMobile = false);
        string GenerateImageThumbName(string imageName, ImageSize size);
        ImageSize[] ImageSizeConvertor(string[] sizes);
        string[] ImageSizeConvertor(ImageSize[] sizes);
        bool CheckCorrectUploadType(IFormFile file, UploadFileType type);
    }

    public enum UploadType
    {
        UserAvatar,        
        CkEditor,
        Blog
    }
    public enum UploadFileType
    {        
        None,        
        Image,        
        Video,        
        Document,        
        Compress,        
        Audio
    }
}
