using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;

// везде первый индекс двумерного массива — координата x соответствующего изображения (второй индекс — y)
namespace pixels
{
    internal static class ImageProcessor
    {
        public static Bitmap Filter(byte[,] input, Kernel kernel, bool monochrome = false)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);
            int kWidth = kernel.Width;
            int kHeight = kernel.Height;
            int kWidthHalf = kWidth / 2;
            int kHeightHalf = kHeight / 2;

            byte[,] inputBytes = AddOutline(input, kWidthHalf, kHeightHalf);
            byte[,] outputBytes = new byte[width, height];

            double sumPixel, sumKernel;
            double kernelValue;
            int x, y, i, j;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    sumPixel = sumKernel = 0;
                    for (i = 0; i < kWidth; i++)
                    {
                        for (j = 0; j < kHeight; j++)
                        {
                            kernelValue = kernel[i, j];
                            if (kernelValue != 0)
                            {
                                sumPixel += inputBytes[x + i, y + j] * kernelValue;
                                sumKernel += kernelValue;
                            }
                        }
                    }
                    if (sumKernel <= 0)
                        sumKernel = 1;

                    sumPixel /= sumKernel;
                    if (sumPixel < 0)
                        sumPixel = 0;
                    else if (sumPixel > 255)
                        sumPixel = 255;

                    outputBytes[x, y] = (byte)sumPixel;
                }
            }
            if (monochrome)
                return BitmapBytesConverter.Get1BppBitmap(outputBytes);
            return BitmapBytesConverter.GetGrayscale8BppBitmap(outputBytes);
        }

        public static Bitmap GetBorderMask(Point[] points, int width, int height)
        {
            byte[,] outputBytes = new byte[width, height];

            int length = points.Length;
            for (int line = 0; line < length; line++)
            {
                outputBytes[points[line].X, points[line].Y] = 255;
            }

            return BitmapBytesConverter.GetGrayscale8BppBitmap(outputBytes);
        }

        public static Bitmap HighlightBorder(Bitmap image, Point[] points, bool borderIsVertical)
        {
            int width = image.Width;
            int height = image.Height;

            int scanLines = borderIsVertical ? height : width;
            int scanLineLength = borderIsVertical ? width : height;

            byte[] outputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);

            int index;
            for (int line = 0; line < scanLines; line++)
            {
                index = (points[line].Y * scanLineLength + points[line].X) * 3;
                outputBytes[index] = (byte)(Byte.MaxValue - outputBytes[index]);
                outputBytes[index + 1] = (byte)(Byte.MaxValue - outputBytes[index + 1]);
                outputBytes[index + 2] = (byte)(Byte.MaxValue - outputBytes[index + 2]);
            }

            return BitmapBytesConverter.Get24BppBitmap(outputBytes, width, height);
        }

        public static int[][] GetDifferenceBeforeOnDirection(byte[,] input, bool horizontalScan)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);

            int scanLines = horizontalScan ? height : width;
            int scanLineLength = horizontalScan ? width : height;
            int[][] diffSummed = new int[scanLines][];

            for (int i = 0; i < scanLines; i++)
            {
                diffSummed[i] = new int[scanLineLength];
            }

            if (horizontalScan)
            {
                for (int line = 0; line < scanLines; line++)
                {
                    //diffSummed[line][0] = (byte)Math.Abs(input[1, line] - input[0, line]); // first
                    for (int posInLine = 0; posInLine < scanLineLength - 1; posInLine++)
                    {
                        diffSummed[line][posInLine] = (byte)Math.Abs(input[posInLine + 1, line] - input[posInLine, line]);
                    }
                    diffSummed[line][scanLineLength - 1] = diffSummed[line][scanLineLength - 2]; // last
                }
            }
            else
            {
                for (int line = 0; line < scanLines; line++)
                {
                    //diffSummed[line][0] = (byte)Math.Abs(input[line, 1] - input[line, 0]); // first
                    for (int posInLine = 0; posInLine < scanLineLength - 1; posInLine++)
                    {
                        diffSummed[line][posInLine] = (byte)Math.Abs(input[line, posInLine + 1] - input[line, posInLine]);
                    }
                    diffSummed[line][scanLineLength - 1] = diffSummed[line][scanLineLength - 2]; // last
                }
            }
            return diffSummed;
        }

        public static int[][] GetDifferenceAfterOnDirection(byte[,] input, bool horizontalScan)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);

            int scanLines = horizontalScan ? height : width;
            int scanLineLength = horizontalScan ? width : height;
            int[][] diffSummed = new int[scanLines][];

            for (int i = 0; i < scanLines; i++)
            {
                diffSummed[i] = new int[scanLineLength];
            }

            if (horizontalScan)
            {
                for (int line = 0; line < scanLines; line++)
                {
                    diffSummed[line][0] = (byte)Math.Abs(input[1, line] - input[0, line]); // first
                    for (int posInLine = 1; posInLine < scanLineLength; posInLine++)
                    {
                        diffSummed[line][posInLine] = (byte)Math.Abs(input[posInLine - 1, line] - input[posInLine, line]);
                    }
                    //diffSummed[line][scanLineLength - 1] = diffSummed[line][scanLineLength - 2]; // last
                }
            }
            else
            {
                for (int line = 0; line < scanLines; line++)
                {
                    diffSummed[line][0] = (byte)Math.Abs(input[line, 1] - input[line, 0]); // first
                    for (int posInLine = 1; posInLine < scanLineLength; posInLine++)
                    {
                        diffSummed[line][posInLine] = (byte)Math.Abs(input[line, posInLine - 1] - input[line, posInLine]);
                    }
                    //diffSummed[line][scanLineLength - 1] = diffSummed[line][scanLineLength - 2]; // last
                }
            }
            return diffSummed;
        }

        public static int[][] GetDifferenceOnDirection(byte[,] input, bool horizontalScan)
        {
            int width = input.GetLength(0);
            int height = input.GetLength(1);

            int scanLines = horizontalScan ? height : width;
            int scanLineLength = horizontalScan ? width : height;
            byte[,] diffOneDirection = new byte[scanLines, scanLineLength - 1];
            int[][] diffSummed = new int[scanLines][];

            for (int i = 0; i < scanLines; i++)
            {
                diffSummed[i] = new int[scanLineLength];
            }

            if (horizontalScan)
            {
                for (int line = 0; line < scanLines; line++)
                {
                    diffOneDirection[line, 0] = (byte)Math.Abs(input[1, line] - input[0, line]);
                    diffSummed[line][0] = diffOneDirection[line, 0]; // first
                    for (int posInLine = 1; posInLine < scanLineLength - 1; posInLine++)
                    {
                        diffOneDirection[line, posInLine] = (byte)Math.Abs(input[posInLine + 1, line] - input[posInLine, line]);
                        diffSummed[line][posInLine] = diffOneDirection[line, posInLine - 1] + diffOneDirection[line, posInLine];
                    }
                    diffSummed[line][scanLineLength - 1] = diffOneDirection[line, scanLineLength - 2]; // last
                }
            }
            else
            {
                for (int line = 0; line < scanLines; line++)
                {
                    diffOneDirection[line, 0] = (byte)Math.Abs(input[line, 1] - input[line, 0]);
                    diffSummed[line][0] = diffOneDirection[line, 0]; // first
                    for (int posInLine = 1; posInLine < scanLineLength - 1; posInLine++)
                    {
                        diffOneDirection[line, posInLine] = (byte)Math.Abs(input[line, posInLine + 1] - input[line, posInLine]);
                        diffSummed[line][posInLine] = diffOneDirection[line, posInLine - 1] + diffOneDirection[line, posInLine];
                    }
                    diffSummed[line][scanLineLength - 1] = diffOneDirection[line, scanLineLength - 2]; // last
                }
            }
            return diffSummed;
        }

        public static Point[] GetBorderCoordinates(int[][] diffSummed, bool verticalBorder)
        {
            int scanLines = diffSummed.GetLength(0);
            Point[] output = new Point[scanLines];

            if (verticalBorder)
            {
                for (int line = 0; line < scanLines; line++)
                {
                    output[line] = new Point(diffSummed[line].MaxIndex(), line);
                }
            }
            else
            {
                for (int line = 0; line < scanLines; line++)
                {
                    output[line] = new Point(line, diffSummed[line].MaxIndex());
                }
            }

            return output;
        }

        public static int MaxIndex<T>(this IEnumerable<T> sequence)
            where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T);

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = value;
                }
                index++;
            }
            return maxIndex;
        }

        public static byte[,] AddOutline(byte[,] original, int dX, int dY)
        {
            int w = original.GetLength(0) + dX * 2;
            int h = original.GetLength(1) + dY * 2;
            byte[,] newArray = new byte[w, h];
            int i, j;
            int stopI = w - dX;
            int stopJ = h - dY;
            for (i = dX; i < stopI; i++)
                for (j = dY; j < stopJ; j++)
                    newArray[i, j] = original[i - dX, j - dY];
            return newArray;
        }

        public static byte[,] GetValues(Bitmap image, ResultingValue value)
        {
            switch (value)
            {
                case ResultingValue.HsbBrightness:
                    return GetHsbBrightness(image);

                case ResultingValue.RgbAverage:
                    return GetRgbAverage(image);

                case ResultingValue.PhotometricLuminance:
                    return GetPhotometricLuminance(image);

                case ResultingValue.CCIR601Luminance:
                    return GetCCIR601Luminance(image);
            }
            throw new Exception("Unknown resulting value.");
        }

        public static byte[,] GetHsbBrightness(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[] inputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);
            byte[,] result = new byte[width, height];
            int index, x, y;
            for (x = 0; x < width; x++)
                for (y = 0; y < height; y++)
                {
                    index = 3 * (width * y + x);
                    result[x, y] = (byte)(Byte.MaxValue * Color.FromArgb(inputBytes[index + 2], inputBytes[index + 1], inputBytes[index]).GetBrightness());
                }
            return result;
        }

        public static byte[,] GetRgbAverage(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[] inputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);
            byte[,] result = new byte[width, height];
            int index, x, y;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    index = 3 * (width * y + x);
                    result[x, y] = (byte)((inputBytes[index] + inputBytes[index + 1] + inputBytes[index + 2]) / 3);
                }
            }
            return result;
        }

        /// <summary>
        /// 0.2126 * R + 0.7152 * G + 0.0722 * B
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[,] GetPhotometricLuminance(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[] inputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);
            byte[,] result = new byte[width, height];
            int index, x, y;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    index = 3 * (width * y + x);
                    result[x, y] = (byte)(0.2126 * inputBytes[index + 2] + 0.7152 * inputBytes[index + 1] + 0.0722 * inputBytes[index]);
                }
            }
            return result;
        }

        /// <summary>
        /// 0.299 * R + 0.587 * G + 0.114 * B
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static byte[,] GetCCIR601Luminance(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[] inputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);
            byte[,] result = new byte[width, height];
            int index, x, y;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    index = 3 * (width * y + x);
                    result[x, y] = (byte)(0.299 * inputBytes[index + 2] + 0.587 * inputBytes[index + 1] + 0.114 * inputBytes[index]);
                }
            }
            return result;
        }

        public static byte[][,] GetRgbValues(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;

            byte[] inputBytes = BitmapBytesConverter.Get24BppBgrBytes(image);
            byte[][,] result = new byte[3][,];
            for (int i = 0; i < 3; i++)
            {
                result[i] = new byte[width, height];
            }
            int index, x, y;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    index = 3 * (width * y + x);
                    result[0][x, y] = inputBytes[index + 2];
                    result[1][x, y] = inputBytes[index + 1];
                    result[2][x, y] = inputBytes[index];
                }
            }
            return result;
        }

        public enum ResultingValue
        {
            HsbBrightness,
            RgbAverage,
            PhotometricLuminance,
            CCIR601Luminance
        }

        private static Bitmap FilterColored(Bitmap input, Kernel kernel)
        {
            if (input == null)
                return input;
            int width = input.Width;
            int height = input.Height;
            int kWidth = kernel.Width;
            int kHeight = kernel.Height;
            int kWidthHalf = kWidth / 2;
            int kHeightHalf = kHeight / 2;

            int outlinedWidth = width + kWidth - 1;
            Bitmap outlinedInput = new Bitmap(outlinedWidth, height + kHeight - 1);
            using (Graphics g = Graphics.FromImage(outlinedInput))
            {
                g.DrawImage(input, 1, 1);
            }
            byte[] inputRgbBytes = BitmapBytesConverter.Get24BppBgrBytes(outlinedInput);

            int borderBytes = ((kWidth - 1) * height + (kHeight - 1) * width + 4 * kWidthHalf * kHeightHalf) * 3;
            byte[] outputRgbBytes = new byte[inputRgbBytes.Length - borderBytes];

            double[] sum = { 0, 0, 0, 0 };
            double kernelValue;
            int index;
            int x, y, i, j;
            for (x = 0; x < width; x++)
            {
                for (y = 0; y < height; y++)
                {
                    sum[0] = sum[1] = sum[2] = sum[3] = 0;
                    for (i = 0; i < kWidth; i++)
                    {
                        for (j = 0; j < kHeight; j++)
                        {
                            kernelValue = kernel[i, j];
                            if (kernelValue != 0)
                            {
                                // обрабатываемый пиксель
                                index = 3 * (outlinedWidth * (y + j) + x + i);
                                sum[0] += inputRgbBytes[index] * kernelValue;
                                sum[1] += inputRgbBytes[index + 1] * kernelValue;
                                sum[2] += inputRgbBytes[index + 2] * kernelValue;
                                sum[3] += kernelValue;
                            }
                        }
                    }
                    // сохраняемый пиксель
                    index = 3 * (width * y + x);
                    if (sum[3] <= 0)
                        sum[3] = 1;
                    for (byte t = 0; t < 3; t++)
                    {
                        sum[t] /= sum[3];
                        if (sum[t] < 0)
                            sum[t] = 0;
                        else if (sum[t] > 255)
                            sum[t] = 255;

                        outputRgbBytes[index + t] = (byte)sum[t];
                    }
                }
            }
            return BitmapBytesConverter.Get24BppBitmap(outputRgbBytes, width, height);
        }
    }
}