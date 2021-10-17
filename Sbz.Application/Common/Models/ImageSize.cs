using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sbz.Application.Common.Models
{
    public class ImageSize
    {
        public ImageSize()
        {
        }

        public ImageSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsEmpty => Width == 0 || Height == 0;

        public string FileExtensionNameWithSize(string fileName)
        {
            return $"{fileName}_{ToString()}";
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}
