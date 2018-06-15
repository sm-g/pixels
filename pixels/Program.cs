using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace pixels
{
    class Program
    {
        static string inputFolder = "in";
        static string outFolder = "out";
        static string valuesFile = String.Format("{0}\\values {1} {2}.txt", outFolder, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString().Replace(':', '-'));
        static string diffsFile = String.Format("{0}\\differences {1} {2}.txt", outFolder, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString().Replace(':', '-'));
        static string pointsFile = String.Format("{0}\\points {1} {2}.txt", outFolder, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString().Replace(':', '-'));
        static string[] pictureExtensions = new string[] { ".png", ".bmp", ".jpg", ".jpeg" };

        static void Main(string[] args)
        {
            var files = GetInputFiles();
            Console.WriteLine("Файлов для обработки: " + files.Count());
            if (files.Count() == 0)
            {
                Console.WriteLine("Ни одного файла-изображения (bmp, png, jpg, jpeg) не найдено в «" + Path.GetFullPath(inputFolder) + "».");
                Console.Read();
                return;
            }

            int selectedModeNumber = -1;
            Console.WriteLine("Выберите модель расчёта величины пикселя:");
            int curNumber = 0;
            Console.WriteLine("0. По всем моделям");
            foreach (ImageProcessor.ResultingValue mode in Enum.GetValues(typeof(ImageProcessor.ResultingValue)))
            {
                Console.Write(++curNumber + ". ");
                Console.WriteLine(mode.ToString());
            }
            var key = Console.ReadKey(true).KeyChar.ToString();

            if (Int32.TryParse(key, out selectedModeNumber) && selectedModeNumber > 0 && selectedModeNumber <= curNumber)
            {
                selectedModeNumber--;
                var selectedMode = (ImageProcessor.ResultingValue)Enum.GetValues(typeof(ImageProcessor.ResultingValue)).GetValue(selectedModeNumber);
                Console.WriteLine("Расчёт по модели " + selectedMode);
            }
            else
            {
                selectedModeNumber = -1;
                Console.WriteLine("Расчёт по всем моделям");
            }

            bool applyFilters = AskUserAction(" для фильтрации", " для подсчёта разностей", 2);
            if (applyFilters)
                Console.WriteLine("Применение фильтров");
            else
                Console.WriteLine("Подсчёт разностей");

            Console.WriteLine();
            int donePictures = 0;
            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);
            foreach (var f in files)
            {
                try
                {
                    ProcessPicture(f, applyFilters, selectedModeNumber);
                    donePictures++;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
            Console.WriteLine(String.Format("Готово. {0} из {1} файл{2} успешно обработан{3}.", donePictures, files.Count(), (files.Count() % 10 != 1 ? "ов" : "а"), (donePictures % 10 != 1 ? "о" : "")));
            Console.WriteLine("См. вывод в «" + Path.GetFullPath(outFolder) + "».");
            Console.Read();
        }

        static IEnumerable<string> GetInputFiles()
        {
            if (!Directory.Exists(inputFolder))
                Directory.CreateDirectory(inputFolder);
            return Directory.GetFiles(inputFolder).Where(file => pictureExtensions.Any(ext => ext == Path.GetExtension(file).ToLower()));
        }

        static bool AskUserAction(string agreeSentence, string noActionSentence, int secondsToWait = 1)
        {
            Console.Write("Нажмите любую клавишу" + agreeSentence + " или подождите " + secondsToWait + " сек." + noActionSentence);
            Console.WriteLine();

            return WaitUserInput(secondsToWait * 1000, 10);
        }

        static bool WaitUserInput(int msToWait, int readAttempts)
        {
            if (readAttempts < 1)
                readAttempts = 1;
            int iterationDuration = msToWait / readAttempts;
            if (iterationDuration < 50)
                iterationDuration = 50;

            for (; !Console.KeyAvailable && readAttempts > 0; readAttempts--)
            {
                System.Threading.Thread.Sleep(iterationDuration);
                Console.Write(".");
            }
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                Console.WriteLine();
                return true;
            }
            Console.WriteLine();
            return false;
        }

        static void ProcessPicture(string filename, bool applyFilters, int selectedModeNumber)
        {
            Bitmap picture = GetImageFromFile(filename);
            if (picture == null)
                return;
            string name = Path.GetFileNameWithoutExtension(filename);
            string extension = Path.GetExtension(filename);
            string fileInfoString = name + extension + " " + new FileInfo(filename).Length / 1024 + "kB";
            Console.Write(fileInfoString + " ");
            string fileFolder = outFolder + "\\" + name;
            if (!Directory.Exists(fileFolder))
                Directory.CreateDirectory(fileFolder);

            var valueBytes = new Dictionary<ImageProcessor.ResultingValue, byte[,]>();
            if (selectedModeNumber != -1)
            {
                var mode = (ImageProcessor.ResultingValue)Enum.GetValues(typeof(ImageProcessor.ResultingValue)).GetValue(selectedModeNumber);
                valueBytes.Add(mode, ImageProcessor.GetValues(picture, mode));
            }
            else
            {
                foreach (ImageProcessor.ResultingValue mode in Enum.GetValues(typeof(ImageProcessor.ResultingValue)))
                {
                    valueBytes.Add(mode, ImageProcessor.GetValues(picture, mode));
                }
            }

            if (applyFilters)
            {
                FilterValueBytesToFile(valueBytes, fileFolder, name);
            }
            else
            {
                PrintValueTablesToFile(picture, valueBytes, fileInfoString);
                OutputBorder(picture, valueBytes, fileFolder, name, fileInfoString);
            }
            Console.WriteLine(" ОК");
        }

        private static void OutputBorder(Bitmap picture, Dictionary<ImageProcessor.ResultingValue, byte[,]> valueBytes, string fileFolder, string name, string fileInfoString)
        {
            byte[,] bytes;
            int[][] diffs;
            Point[] points;

            StreamWriter swPoints = new StreamWriter(pointsFile, true);
            swPoints.WriteLine(DateTime.Now + " " + fileInfoString);
            swPoints.Close();

            foreach (ImageProcessor.ResultingValue mode in valueBytes.Keys)
            {
                if (valueBytes.TryGetValue(mode, out bytes))
                {
                    diffs = ImageProcessor.GetDifferenceOnDirection(bytes, true);
                    PrintDifferencesTableToFile(diffs, true, mode.ToString(), "vertical");
                    points = ImageProcessor.GetBorderCoordinates(diffs, true);
                    ShowBorderOnImage(picture, points, fileFolder, name, "vertical " + mode);
                    PrintBorderPointsToFile(points, mode.ToString(), "vertical");

                    //diffs = ImageProcessor.GetDifferenceBeforeOnDirection(bytes, true);
                    //PrintDifferencesTableToFile(diffs, true, mode.ToString(), "vertical before");
                    //points = ImageProcessor.GetBorderCoordinates(diffs, true);
                    //ShowBorderOnImage(picture, points, fileFolder, name, "vertical before" + mode);
                    //PrintBorderPointsToFile(points, mode.ToString(), "vertical before");

                    //diffs = ImageProcessor.GetDifferenceAfterOnDirection(bytes, true);
                    //PrintDifferencesTableToFile(diffs, true, mode.ToString(), "vertical after");
                    //points = ImageProcessor.GetBorderCoordinates(diffs, true);
                    //ShowBorderOnImage(picture, points, fileFolder, name, "vertical after" + mode);
                    //PrintBorderPointsToFile(points, mode.ToString(), "vertical after");

                    diffs = ImageProcessor.GetDifferenceOnDirection(bytes, false);
                    PrintDifferencesTableToFile(diffs, false, mode.ToString(), "horizontal");
                    points = ImageProcessor.GetBorderCoordinates(diffs, false);
                    ShowBorderOnImage(picture, points, fileFolder, name, "horizontal " + mode);
                    PrintBorderPointsToFile(points, mode.ToString(), "horizontal");

                    //diffs = ImageProcessor.GetDifferenceBeforeOnDirection(bytes, false);
                    //PrintDifferencesTableToFile(diffs, false, mode.ToString(), "horizontal before");
                    //points = ImageProcessor.GetBorderCoordinates(diffs, false);
                    //ShowBorderOnImage(picture, points, fileFolder, name, "horizontal before" + mode);
                    //PrintBorderPointsToFile(points, mode.ToString(), "horizontal before");

                    //diffs = ImageProcessor.GetDifferenceAfterOnDirection(bytes, false);
                    //PrintDifferencesTableToFile(diffs, false, mode.ToString(), "horizontal after");
                    //points = ImageProcessor.GetBorderCoordinates(diffs, false);
                    //ShowBorderOnImage(picture, points, fileFolder, name, "horizontal after" + mode);
                    //PrintBorderPointsToFile(points, mode.ToString(), "horizontal after");
                    Console.Write(".");
                }
            }
        }

        static void PrintValueTablesToFile(Bitmap picture, Dictionary<ImageProcessor.ResultingValue, byte[,]> valueBytes, string fileInfoString)
        {
            byte[,] bytes;
            StreamWriter swValues = new StreamWriter(valuesFile, true);
            swValues.WriteLine(DateTime.Now + " " + fileInfoString + " " + picture.Width + "*" + picture.Height);

            swValues.WriteLine(GetTablesOfRgbBytes(ImageProcessor.GetRgbValues(picture)));
            foreach (ImageProcessor.ResultingValue mode in valueBytes.Keys)
            {
                if (valueBytes.TryGetValue(mode, out bytes))
                {
                    swValues.WriteLine(mode);
                    swValues.WriteLine(GetTableOfBytes(bytes));

                    Console.Write(".");
                }
            }
            swValues.Close();
        }

        static void PrintDifferencesTableToFile(int[][] diffs, bool firstIsHeight, string resultingValue, string borderType)
        {
            StreamWriter swDiffs = new StreamWriter(diffsFile, true);
            swDiffs.WriteLine(resultingValue + ", " + borderType + " border");
            swDiffs.WriteLine(GetTableOfDifferences(diffs, firstIsHeight));
            swDiffs.Close();
        }

        static void PrintBorderPointsToFile(Point[] points, string resultingValue, string borderType)
        {
            StreamWriter swPoints = new StreamWriter(pointsFile, true);
            swPoints.WriteLine(resultingValue + ", " + borderType + " border");
            swPoints.WriteLine(GetTableOfPoints(points, true));
            swPoints.Close();
        }

        static void ShowBorderOnImage(Bitmap picture, Point[] points, string fileFolder, string name, string comment)
        {
            SaveBitmap(ImageProcessor.GetBorderMask(points, picture.Width, picture.Height), fileFolder, name, "border mask " + comment);
            SaveBitmap(ImageProcessor.HighlightBorder(picture, points, true), fileFolder, name, "border " + comment);
        }

        static void FilterValueBytesToFile(Dictionary<ImageProcessor.ResultingValue, byte[,]> valueBytes, string fileFolder, string fileName)
        {
            byte[,] bytes;
            foreach (Kernel.DefinedKernels kernel in Enum.GetValues(typeof(Kernel.DefinedKernels)))
            {
                Console.Write(kernel + " ");
                foreach (ImageProcessor.ResultingValue mode in valueBytes.Keys)
                {
                    if (valueBytes.TryGetValue(mode, out bytes))
                    {
                        SaveBitmap(ImageProcessor.Filter(bytes, Kernel.FromDefined(kernel)), fileFolder, fileName, kernel + " " + mode);
                        Console.Write(".");
                    }
                }
            }
        }

        static void TestResultingValues(Bitmap image)
        {
            byte[,] table;
            foreach (ImageProcessor.ResultingValue v in Enum.GetValues(typeof(ImageProcessor.ResultingValue)))
            {
                table = ImageProcessor.GetValues(image, v);
                Console.Write(GetTableOfBytes(table));
                Console.ReadLine();
            }
        }

        static Bitmap GetImageFromFile(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("Не могу найти " + filename);
                return null;
            }
            return new Bitmap(Image.FromFile(filename));
        }

        static void SaveBitmap(Bitmap image, string folder, string name, string comment)
        {
            string outFilename = folder + "\\" + Directory.GetFiles(folder).Length + " " + name + " " + comment;
            while (File.Exists(outFilename))
            {
                outFilename += "_";
            }

            if (image.Width * image.Height < 2500) // 50*50
            {
                int zoom = 10;
                string tempFile = System.IO.Path.GetTempFileName();
                Bitmap tempImage = new Bitmap(image.Width * zoom, image.Height * zoom);
                tempImage.Save(tempFile, ImageFormat.Bmp);

                using (var g = Graphics.FromImage(tempImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.ScaleTransform(zoom, zoom);
                    g.DrawImage(image, 0, 0);
                }
                tempImage.Save(outFilename + " x" + zoom + "zoom.png", ImageFormat.Png);
                System.IO.File.Delete(tempFile);
            }
            else
                image.Save(outFilename + ".png", ImageFormat.Png);
        }

        static string GetTableOfBytes(byte[,] values, char delimeter = '\t')
        {
            StringBuilder sb = new StringBuilder();
            int width = values.GetLength(0);
            int height = values.GetLength(1);

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    sb.Append(values[j, i]);
                    sb.Append(delimeter);
                }
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        static string GetTableOfDifferences(int[][] diffs, bool firstIsHeight, char delimeter = '\t')
        {
            StringBuilder sb = new StringBuilder();
            int width = firstIsHeight ? diffs[0].GetLength(0) : diffs.GetLength(0);
            int height = firstIsHeight ? diffs.GetLength(0) : diffs[0].GetLength(0);

            if (firstIsHeight)
            {
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        sb.Append(diffs[i][j]);
                        sb.Append(delimeter);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.AppendLine();
                }
            }
            else
            {
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        sb.Append(diffs[j][i]);
                        sb.Append(delimeter);
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        static string GetTablesOfRgbBytes(byte[][,] rgbBytes, char delimeter = '\t')
        {
            StringBuilder sb = new StringBuilder();
            int width = rgbBytes[0].GetLength(0);
            int height = rgbBytes[0].GetLength(1);

            sb.AppendLine("Red values");
            sb.Append(GetTableOfBytes(rgbBytes[0], delimeter));
            sb.AppendLine("Green values");
            sb.Append(GetTableOfBytes(rgbBytes[1], delimeter));
            sb.AppendLine("Blue values");
            sb.Append(GetTableOfBytes(rgbBytes[2], delimeter));

            return sb.ToString();
        }

        static string GetTableOfPoints(Point[] points, bool borderIsVertical, char delimeter = '\t')
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < points.Length; i++)
            {
                sb.Append(points[i].X);
                sb.Append(delimeter);
                sb.Append(points.Length - points[i].Y - 1);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
