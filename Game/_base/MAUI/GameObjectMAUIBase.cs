using SkiaSharp;
using System.Reflection;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace NSS.GameObjects
{
    public abstract class GameObjectMAUIBase
    {
        public IImage LoadImageFromResource(string name)
        {
            using (var stream = Assembly.GetCallingAssembly().GetManifestResourceStream($"Grid.Resources.Images.{name}"))
            {
                return (IImage)SKImage.FromEncodedData(stream); 
                // return PlatformImage.FromStream(stream);
            }
        }
    }
}
