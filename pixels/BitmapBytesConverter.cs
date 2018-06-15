using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;

namespace pixels
{
    public static class BitmapBytesConverter
    {
        [DllImport("user32.dll")]
        static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr hObject);

        private const int LR_LOADFROMFILE = 0x0010;
        private const int LR_MONOCHROME = 0x0001;

        public static byte[] Get24BppBgrBytes(Bitmap input)
        {
            int width = input.Width;
            BitmapData inputData = input.LockBits(
                new Rectangle(0, 0, width, input.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            int stride = Math.Abs(inputData.Stride);
            int bytesCount = stride * input.Height;
            byte[] bytes = new byte[bytesCount];
            System.Runtime.InteropServices.Marshal.Copy(inputData.Scan0, bytes, 0, bytesCount);
            input.UnlockBits(inputData);

            int outputRowLenght = width * 3;
            byte[] output = bytes.Where((value, index) => index % stride < outputRowLenght).ToArray();
            return output;
        }

        public static Bitmap Get24BppBitmap(byte[] bgrBytes, int width, int height)
        {
            byte bytesPerPixel = 3;
            if (bgrBytes.Length % bytesPerPixel != 0) return null;
            Bitmap output = new Bitmap(width, height);
            BitmapData outputData = output.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);

            int stride = Math.Abs(outputData.Stride);
            int outputBytes = stride * height;
            byte[] bitmapBytes = new byte[outputBytes];
            int row, column, span, bitmapIndex, inputIndex;
            for (row = 0; row < height; row++)
                for (column = 0; column < width; column++)
                {
                    span = column * bytesPerPixel;
                    bitmapIndex = row * stride + span;
                    inputIndex = row * bytesPerPixel * width + span;
                    bitmapBytes[bitmapIndex] = bgrBytes[inputIndex];
                    bitmapBytes[bitmapIndex + 1] = bgrBytes[inputIndex + 1];
                    bitmapBytes[bitmapIndex + 2] = bgrBytes[inputIndex + 2];
                }

            System.Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, outputData.Scan0, bitmapBytes.Length);
            output.UnlockBits(outputData);
            return output;
        }

        public static Bitmap GetGrayscale24BppBitmap(byte[,] valueBytes)
        {
            int bytesPerPixel = 3;
            int width = valueBytes.GetLength(0);
            int height = valueBytes.GetLength(1);
            Bitmap output = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            BitmapData outputData = output.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format24bppRgb);

            int stride = Math.Abs(outputData.Stride);
            int outputBytes = stride * height;
            byte[] bitmapBytes = new byte[outputBytes];
            int row, column, bitmapIndex;
            byte value;
            for (row = 0; row < height; row++)
                for (column = 0; column < width; column++)
                {
                    bitmapIndex = row * stride + column * bytesPerPixel;
                    value = valueBytes[column, row];
                    bitmapBytes[bitmapIndex] = value;
                    bitmapBytes[bitmapIndex + 1] = value;
                    bitmapBytes[bitmapIndex + 2] = value;
                }
            System.Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, outputData.Scan0, bitmapBytes.Length);
            output.UnlockBits(outputData);
            return output;
        }

        public static Bitmap GetGrayscale8BppBitmap(byte[,] valueBytes)
        {
            int width = valueBytes.GetLength(0);
            int height = valueBytes.GetLength(1);
            Bitmap output = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            BitmapData outputData = output.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);

            output.Palette = Get8bppGrayscalePalette();

            int stride = Math.Abs(outputData.Stride);
            int outputBytes = stride * height;
            byte[] bitmapBytes = new byte[outputBytes];
            int row, column;
            for (row = 0; row < height; row++)
                for (column = 0; column < width; column++)
                {
                    bitmapBytes[row * stride + column] = valueBytes[column, row];
                }
            System.Runtime.InteropServices.Marshal.Copy(bitmapBytes, 0, outputData.Scan0, bitmapBytes.Length);
            output.UnlockBits(outputData);
            return output;
        }

        public static Bitmap Get1BppBitmap(byte[,] valueBytes)
        {
            Bitmap temp = GetGrayscale24BppBitmap(valueBytes);

            string tempFile = System.IO.Path.GetTempFileName();
            temp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Bmp);
            IntPtr hBitmap = LoadImage(IntPtr.Zero, tempFile, 0, 0, 0, (LR_LOADFROMFILE | LR_MONOCHROME));
            Bitmap output = Image.FromHbitmap(hBitmap);

            DeleteObject(hBitmap);
            System.IO.File.Delete(tempFile);
            return output;
        }

        static ColorPalette Get8bppGrayscalePalette()
        {
            ColorPalette palette = new Bitmap(1, 1, PixelFormat.Format8bppIndexed).Palette;
            Color[] e = palette.Entries;
            for (int i = 0; i < 256; i++)
            {
                e[i] = Color.FromArgb(i, i, i);
            }
            return palette;
        }
    }
}
