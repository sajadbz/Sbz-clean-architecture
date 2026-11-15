using ImageMagick;
using Sbz.Application.Common.Extensions;
using Sbz.Application.Common.Interfaces;
using Sbz.Application.Common.Models;
using Sbz.Application.Statics;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sbz.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly ILoggerManager<FileService> _logger;
        private readonly IDateTime _dateTime;
        public FileService(ILoggerManager<FileService> logger, IDateTime dateTime)
        {
            _logger = logger;
            _dateTime = dateTime;
        }

        public async Task<string> AddFileToServer(IFormFile file, UploadType fileType, string fileNameWithoutExt = null)
        {
            var destinationPath = UploadPathFinder(fileType);
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            if (file?.Length > 0)
            {
                string fileName;
                if (!string.IsNullOrEmpty(fileNameWithoutExt))
                {
                    fileNameWithoutExt += $"_{_dateTime.NowName}";
                    fileName = GenerateImageName(fileNameWithoutExt, file.FileName, new ImageSize[0]);
                }
                else
                {
                    var ext = Path.GetExtension(file.FileName);
                    fileName = $"{Application.Common.Generator.CodeGenerator.NewGuidCode()}{ext}";
                }

                var filePath = Path.Combine(destinationPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return fileName;
            }

            throw new Exception("خطا در ذخیره سازی فایل!!!");
        }

        public async Task<string> AddImageToServer(IFormFile file, UploadType fileType, string fileNameWithoutExt, params ImageSize[] thumbSize)
        {
            var destinationPath = UploadPathFinder(fileType);
            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);
            if (file.Length > 0)
            {
                var fileName = GenerateImageName(fileNameWithoutExt, file.FileName, thumbSize);
                var filePath = Path.Combine(destinationPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                foreach (var s in thumbSize)
                {
                    var fileThumbName = GenerateImageThumbName(fileName, s);
                    var fileThumbPath = Path.Combine(destinationPath, fileThumbName);
                    ImageResizer(filePath, fileThumbPath, s);
                }

                return fileName;
            }

            throw new Exception("خطا در ذخیره سازی فایل!!!");
        }

        public async Task<string> AddImageToServer(string base64Image, UploadType fileType, ImageSize[] thumbSize)
        {
            if (!base64Image.IsNullEmptyOrWhitespace())
            {
                var destinationPath = UploadPathFinder(fileType);
                var fileName = Application.Common.Generator.CodeGenerator.NewGuidCode()+".jpg";
                var filePath = Path.Combine(destinationPath, fileName);

                await ProcessImageAsync(filePath, base64Image);

                foreach (var size in thumbSize)
                {
                    var fileThumbName = GenerateImageThumbName(fileName, size);
                    var fileThumbPath = Path.Combine(destinationPath, fileThumbName);
                    await ImageResizerAsync(filePath, fileThumbPath, size);
                }

                return fileName;
            }

            throw new Exception("خطا در ذخیره سازی فایل!!!");
        }

        public async Task ProcessImageAsync(string filePath, string base64Image)
        {
            try
            {
                string[] base64 = base64Image.Split(',');
                var b = base64[1];
                byte[] bytes = Convert.FromBase64String(b);
                var pathDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(pathDirectory))
                    Directory.CreateDirectory(pathDirectory);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.WriteAsync(bytes, 0, bytes.Length);
                    await stream.FlushAsync();
                }

                ImageOptimizer(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError("ProcessImage: " + ex.Message);
            }
        }

        public void ProcessImage(string filePath, string base64Image)
        {
            try
            {
                string[] base64 = base64Image.Split(',');
                var b = base64[1];
                byte[] bytes = Convert.FromBase64String(b);
                var pathDirectory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(pathDirectory))
                    Directory.CreateDirectory(pathDirectory);

                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                ImageOptimizer(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError("ProcessImage: " + ex.Message);
            }
        }

        public void ProcessImage(string filePath, string base64Image, params ImageSize[] sizes)
        {
            ProcessImage(filePath, base64Image);
            if (sizes == null) return;
            foreach (var s in sizes)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var ext = Path.GetExtension(filePath);
                var desPath = filePath.Replace(fileName + ext, "");


                var thumbFileName = $"{fileName.Remove(fileName.LastIndexOf('('))}_{s}{ext}";

                var fullPathThumb = Path.Combine(desPath, thumbFileName);
                ImageResizer(filePath, fullPathThumb, s);
            }
        }

        public void ProcessImage(string filePath, string base64Image, string[] sizes)
        {
            ProcessImage(filePath, base64Image, ImageSizeConvertor(sizes));
        }

        public async Task ImageResizerAsync(string inputImagePath, string outputImagePath, ImageSize size)
        {
            var outputFormat = GetOutputImageFormat(inputImagePath);
            if (outputFormat == MagickFormat.Unknown)
                return;

            using var image = new MagickImage();
            using (var source = File.OpenRead(inputImagePath))
                await image.ReadAsync(source, GetInputImageFormat(source));

            ResizeAndCrop(image, size);
            ConfigureImageOutput(image, outputFormat);
            await using var destination = File.Create(outputImagePath);
            await image.WriteAsync(destination, outputFormat);
        }

        public void ImageResizer(string inputImagePath, string outputImagePath, ImageSize size)
        {
            var outputFormat = GetOutputImageFormat(inputImagePath);
            if (outputFormat == MagickFormat.Unknown)
                return;

            using var image = new MagickImage();
            using (var source = File.OpenRead(inputImagePath))
                image.Read(source, GetInputImageFormat(source));

            ResizeAndCrop(image, size);
            ConfigureImageOutput(image, outputFormat);
            using var destination = File.Create(outputImagePath);
            image.Write(destination, outputFormat);
        }

        public void ImageResizer(string inputImagePath, string outputImagePath, ImageSize[] sizes)
        {
            foreach (var size in sizes)
                ImageResizer(inputImagePath, outputImagePath, size);

        }

        public void ImageOptimizer(string imagePath)
        {
            var outputFormat = GetOutputImageFormat(imagePath);
            if (outputFormat == MagickFormat.Unknown)
                return;

            using var image = new MagickImage();
            using (var source = File.OpenRead(imagePath))
                image.Read(source, GetInputImageFormat(source));

            ConfigureImageOutput(image, outputFormat);
            using var destination = File.Create(imagePath);
            image.Write(destination, outputFormat);
        }

        public void DeleteImage(UploadType fileType, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var path = UploadPathFinder(fileType);
            var fileExt = Path.GetExtension(fileName);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var index = fileNameNoExt.LastIndexOf('(');
            if (index > 0)
            {
                var sizesStr = fileNameNoExt.Substring(index);
                string[] sizes = sizesStr.Replace("(", string.Empty).Replace(")", string.Empty).Split(',');
                for (int i = 0; i < sizes.Length; i++)
                {
                    var thumbName = GenerateImageThumbName(fileName, sizes[i]);
                    var thumbFullPath = Path.Combine(path, thumbName);
                    if (File.Exists(thumbFullPath))
                        File.Delete(thumbFullPath);
                }
            }

            var mainImagePath = Path.Combine(path, fileName);
            if (File.Exists(mainImagePath))
                File.Delete(mainImagePath);
        }
         public void DeleteFile(UploadType fileType, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var path = UploadPathFinder(fileType);                      

            var mainFilePath = Path.Combine(path, fileName);
            if (File.Exists(mainFilePath))
                File.Delete(mainFilePath);
        }

        public string GenerateImageName(string uniqName, string fileName, string[] thumbnailSizes)
        {
            var ext = Path.GetExtension(fileName);
            string tn = "";
            if (thumbnailSizes != null)
                tn = $"({string.Join(",", thumbnailSizes)})";
            return $"{uniqName}{tn}{ext}";
        }

        public string GenerateImageName(string uniqName, string fileName, ImageSize[] thumbnailSizes)
        {
            uniqName = uniqName.FixTitleForUrl();
            var sizeStr = ImageSizeConvertor(thumbnailSizes);
            return GenerateImageName(uniqName, fileName, sizeStr);
        }

        public string GenerateSliderImageName(string imageName, bool isMobile = false)
        {
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return isMobile ? $"{Path.GetFileNameWithoutExtension(imageName)}_mobile_{dateTime}{Path.GetExtension(imageName)}"
                : $"{Path.GetFileNameWithoutExtension(imageName)}_{dateTime}{Path.GetExtension(imageName)}";
        }

        public string GenerateImageThumbName(string imageName, ImageSize size)
        {
            return Paths.GenerateImageThumbName(imageName, size);
            //var fileNameNoExt = Path.GetFileNameWithoutExtension(imageName);
            //var fileExt = Path.GetExtension(imageName);
            //var index = fileNameNoExt.LastIndexOf('(');
            //var sizesStr = fileNameNoExt.Substring(index);

            //return $"{fileNameNoExt.Replace(sizesStr, string.Empty)}_{size}{fileExt}";
        }

        public ImageSize[] ImageSizeConvertor(string[] sizes)
        {
            if (sizes == null) return null;
            var length = sizes.Length;
            ImageSize[] imgSizes = new ImageSize[length];
            for (int i = 0; i < length; i++)
            {
                var sizeStr = sizes[i].Split('x', 'X');
                if (int.TryParse(sizeStr[0], out int width) &&
                    int.TryParse(sizeStr[1], out int height))
                {
                    imgSizes[i] = new ImageSize(width, height);
                }
            }
            return imgSizes;
        }

        public string[] ImageSizeConvertor(ImageSize[] sizes)
        {
            if (sizes == null) return null;
            var length = sizes.Length;
            string[] sizeStr = new string[length];
            for (int i = 0; i < length; i++)
            {
                sizeStr[i] = sizes[i].ToString();
            }
            return sizeStr;
        }

        public bool CheckCorrectUploadType(IFormFile file, UploadFileType type)
        {
            switch (type)
            {
                case UploadFileType.Image:
                    if (file.ContentType == "image/jpeg" || file.ContentType == "image/png")
                    {
                        try
                        {
                            using var stream = file.OpenReadStream();
                            using var image = new MagickImage();
                            image.Read(stream, GetInputImageFormat(stream));
                            return true;
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("جلوگیری از بارگزاری یک فایل به جای عکس" + e);
                            return false;
                        }
                    }
                    break;
                case UploadFileType.Video:
                    if (file.ContentType == "video/mp4" || file.ContentType == "video/3gpp")
                    {
                        return true;
                    }
                    break;
                case UploadFileType.Audio:
                    if (file.ContentType == "audio/mpeg" || file.ContentType == "audio/x-m4a")
                    {
                        return true;
                    }
                    break;
                case UploadFileType.Document:
                    if (file.ContentType == "application/pdf" || file.ContentType == "application/msword" || file.ContentType == "text/plain" || file.ContentType == "application/vnd.ms-powerpoint")
                    {
                        return true;
                    }
                    break;
                case UploadFileType.Compress:
                    if (file.ContentType == "application/zip" || file.ContentType == "application/x-7z-compressed" || file.ContentType == "application/octet-stream")
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        #region Private Methods
        private static MagickFormat GetOutputImageFormat(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                ".png" => MagickFormat.Png,
                _ => MagickFormat.Unknown
            };
        }

        private static MagickFormat GetInputImageFormat(Stream source)
        {
            Span<byte> header = stackalloc byte[8];
            var position = source.Position;
            source.ReadExactly(header);
            source.Position = position;

            if (header.StartsWith(new byte[] { 0xff, 0xd8, 0xff }))
                return MagickFormat.Jpeg;
            if (header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
                return MagickFormat.Png;

            throw new InvalidDataException("Only JPEG and PNG images are supported.");
        }

        private static void ResizeAndCrop(MagickImage image, ImageSize size)
        {
            var width = checked((uint)size.Width);
            var height = checked((uint)size.Height);
            image.Resize(new MagickGeometry(width, height) { FillArea = true });
            if (width == 0 || height == 0)
                return;

            image.Crop(width, height, Gravity.Center);
            image.ResetPage();
        }

        private static void ConfigureImageOutput(MagickImage image, MagickFormat format)
        {
            if (format == MagickFormat.Jpeg)
                image.Quality = 90;
        }

        private string GenerateImageThumbName(string imageName, string size)
        {
            var s = ImageSizeConvertor(new[] { size });
            return GenerateImageThumbName(imageName, s[0]);
        }

        private string UploadPathFinder(UploadType fileType)
        {
            switch (fileType)
            {
                case UploadType.UserAvatar:
                    return Paths.User.ImagePathServer;
                //case UploadType.UserIdentity:
                //    return Paths.User.IdentityImagePathServer;
                case UploadType.CkEditor:
                    return Paths.UploadEditorPathServer;
                //case UploadType.ProductGallery:
                //    return Paths.Product.ImagePathServer;
                //case UploadType.ProductCatalog:
                //    return Paths.Product.CatalogPathServer;
                //case UploadType.BranchLogo:
                //    return Paths.Branch.ImagePathServer;
            }

            return Paths.UploadStaticFilePath;
        }
        #endregion
    }
}
