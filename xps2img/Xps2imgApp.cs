using System.Drawing.Imaging;
using System.IO;

namespace xps2img
{
    class Xps2imgApp
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (File.Exists(args[i]))
                    {
                        string filename = Path.GetFileNameWithoutExtension(args[i]);
                        string extension = Path.GetExtension(args[i]);
                        if (extension == ".xps")
                        {
                            using (var xpsConverter = new Xps2Image(args[i]))
                            {
                                var images = xpsConverter.ToBitmap(new Parameters
                                {
                                    ImageType = ImageType.Png,
                                    Dpi = 300
                                });

                                int index = 1;

                                foreach (var image in images)
                                {
                                    image.Save(filename + index.ToString() + ".jpg", ImageFormat.Jpeg);
                                    index++;
                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }
    }
}
