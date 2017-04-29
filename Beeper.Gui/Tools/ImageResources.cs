using Beeper.Gui.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Beeper.Gui.Tools
{
    public static class ImageResources
    {
        private static List<ImageResource> imageResources = new List<ImageResource>();

        public static void LoadImageResources(string resourceFolder)
        {
            foreach (string File in Directory.EnumerateFiles(resourceFolder).Where(p => Path.GetExtension(p) == ".png"))
            {
                List<char> keyList =
                    Path.GetFileNameWithoutExtension(File)
                    .ToArray()
                    .ToList();
                keyList.RemoveAll(c => Char.IsDigit(c));
                StringBuilder builder = new StringBuilder();
                builder.Append(keyList.ToArray());
                string key = builder.ToString();
                Image rawImage = Image.FromFile(File);
                int size = (rawImage.Width + rawImage.Height) / 2;
                ImageResource image = new ImageResource()
                {
                    Key = key,
                    Base = new BitmapImage(new Uri(File)),
                    Size = size
                };
                imageResources.Add(image);
            }
        }

        public static BitmapImage GetImageResource(string ImageName, int Size)
        {
            return imageResources.FirstOrDefault(p => p.Key == ImageName && p.Size == Size * 2).Base;
        }

        public static BitmapImage GetImageFromFile(string filePath)
        {
            return new BitmapImage(new Uri(filePath));
        }
    }
}
