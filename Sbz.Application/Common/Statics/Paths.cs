using Sbz.Application.Common.Models;
using System.IO;

namespace Sbz.Application.Statics
{
    public static class Paths
    {
        public static string UploadStaticFilePath = "/u";
        public static string UploadEditorPath = $"{UploadStaticFilePath}/editor";
        public static string UploadEditorPathServer = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot{UploadEditorPath}");
        public static string UploadEditorFilePath = $"{UploadEditorPath}/files";
        public static string UploadEditorFilePathServer = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot{UploadEditorFilePath}");
        
        public static DirectoryInfo KeyDirectoryServerPath = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/kd/"));
        public static string GenerateImageThumbName(string imageName, ImageSize size)
        {
            if (string.IsNullOrEmpty(imageName)) return "";
            if (size.IsEmpty) return "";

            var fileNameNoExt = Path.GetFileNameWithoutExtension(imageName);
            var fileExt = Path.GetExtension(imageName);
            try
            {                
                return $"{size}_{fileNameNoExt}{fileExt}";
            }
            catch
            {
                // ignored
            }

            return string.Empty;
        }

        public static class User
        {
            #region Avatar            
            public static ImageSize[] ThumbSizes = new[] { new ImageSize(75,75), new ImageSize(300,300)};
            //Todo: default image name for user
            public static string ImageDefault = $"/?/avatar.png";
            public static string ImagePath = $"{UploadStaticFilePath}/user/avatar";
            public static string ImagePathServer = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot{ImagePath}");
            public static string GeneratePath(string imageName, bool isServer = false)
            {
                if (!isServer)
                    return !string.IsNullOrEmpty(imageName)
                        ? Path.Combine(ImagePath, imageName).Replace("\\", "/")
                        : ImageDefault;

                return !string.IsNullOrEmpty(imageName) ?
                    Path.Combine(ImagePathServer, imageName).Replace("\\", "/") :
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", ImageDefault);
            }

            public static string GenerateThumbPath(string imageName, ImageSize size, bool isServer = false)
            {
                return GeneratePath(GenerateImageThumbName(imageName, size), isServer);
            }
            #endregion
                        

        }
        
    }  
}
