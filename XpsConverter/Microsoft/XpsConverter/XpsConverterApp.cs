using System;
using System.IO;
using System.IO.Packaging;

namespace Microsoft.XpsConverter
{
    internal class XpsConverterApp
	{

        private static XpsType _convertFrom = XpsType.OpenXPS;
        private static XpsType _convertTo = XpsType.MSXPS;

        private enum ExitCode
        {
            MissingDll = -4,
            Unexpected,
            Block,
            Fail,
            Succeed,
            Warn
        }

        private static int Main(string[] args)
		{
            if(args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (File.Exists(args[i]))
                    {
                        string filename = Path.GetFileNameWithoutExtension(args[i]);
                        string extension = Path.GetExtension(args[i]);
                        if (extension == ".oxps")
                        {
                            filename = $"{filename}.xps";
                            ConvertFile(args[i], filename);
                        }
                    }
                    else if (Directory.Exists(args[i]))
                    {
                        ConvertFolder(args[i], $"{args[i]}_OUT");
                    }
                }
            }
			return 0;
		}

		private static void ConvertFile(string inputFile, string outputFile)
		{
			try
			{
				File.Copy(inputFile, outputFile, true);
				File.SetAttributes(outputFile, FileAttributes.Normal);
			}
			catch (Exception ex)
			{
                Console.WriteLine($"Exception occured while processing files {ex}");
                return;
			}
			try
			{
				using (Package package = Package.Open(outputFile, FileMode.Open, FileAccess.ReadWrite))
				{
					new XpsConverter(package, _convertFrom, _convertTo).Process();
					package.Close();
				}
            }
			catch (Exception ex2)
			{
                Console.WriteLine($"Exception occured while processing file. \nThe file might not be fully converted.\n {ex2}");
			}
		}

		private static void ConvertFolder(string inputFolder, string outputFolder)
		{
			try
			{
				DirectoryInfo directoryInfo = new DirectoryInfo(inputFolder);
				if (!directoryInfo.Exists)
				{
                    Console.WriteLine("Folder does not exist.");
				}
				else
				{
					DirectoryInfo directoryInfo2 = new DirectoryInfo(outputFolder);
					if (!directoryInfo2.Exists)
					{
						directoryInfo2.Create();
					}
					inputFolder = directoryInfo.FullName;
					outputFolder = directoryInfo2.FullName;
					string str = (_convertFrom == XpsType.MSXPS) ? "xps" : "oxps";
					string extension = (_convertTo == XpsType.MSXPS) ? "xps" : "oxps";
					FileInfo[] files = directoryInfo.GetFiles("*." + str, SearchOption.AllDirectories);
					for (int i = 0; i < files.Length; i++)
					{
						FileInfo fileInfo = files[i];
						string text = outputFolder + fileInfo.DirectoryName.Substring(inputFolder.Length);
						if (!text.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
						{
							text += "\\";
						}
						if (!Directory.Exists(text))
						{
							Directory.CreateDirectory(text);
						}
						string outputFile = text + Path.ChangeExtension(fileInfo.Name, extension);
						ConvertFile(fileInfo.FullName, outputFile);
					}
				}
			}
			catch (Exception ex)
			{
                Console.WriteLine($"Exception occured while processing folders {ex}");

            }
		}
	}
}
