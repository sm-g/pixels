using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pixels
{
    class Kernel
    {
        static double[,] prewittMatrix = { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };
        static double[,] sobelMatrix = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        static double[,] laplacianMatrix = { { 1, 0, 1 }, { 0, -4, 0 }, { 1, 0, 1 } };
        static double[,] scharrMatrix = { { -3, -10, -3 }, { 0, 0, 0 }, { 3, 10, 3 } };

        double[,] matrix;

        public double[,] Matrix { get { return matrix; } }
        public double this[int x, int y] { get { return matrix[x, y]; } }
        public int Width { get { return matrix.GetLength(0); } }
        public int Height { get { return matrix.GetLength(1); } }
        public string Name { get; private set; }

        public static Kernel Prewitt { get { return new Kernel(prewittMatrix, "Prewitt"); } }
        public static Kernel Sobel { get { return new Kernel(sobelMatrix, "Sobel"); } }
        public static Kernel Scharr { get { return new Kernel(scharrMatrix, "Scharr"); } }
        public static Kernel Laplacian { get { return new Kernel(laplacianMatrix, "Laplacian"); } }

        public static Kernel FromDefined(DefinedKernels kernel)
        {
            switch (kernel)
            {
                case DefinedKernels.Prewitt:
                    return Prewitt;
                case DefinedKernels.Sobel:
                    return Sobel;
                case DefinedKernels.Scharr:
                    return Scharr;
                case DefinedKernels.Laplacian:
                    return Laplacian;
            }
            throw new Exception("No such defined kernel.");
        }

        public Kernel(double[,] kernelMatrix, string name = "")
        {
            if (kernelMatrix.Length > 1)
                matrix = kernelMatrix;
            else
                throw new ArgumentException();

            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public enum DefinedKernels
        {
            Prewitt,
            Sobel,
            Scharr,
            Laplacian
        }
    }
}
