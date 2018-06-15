using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;

namespace pixels
{
    [TestFixture]
    public class ImageProcessorTests
    {
        private static IEnumerable<TestCaseData> AddOutlineCases
        {
            get
            {
                yield return new TestCaseData(
                    new byte[,] { { 1 } },
                    0, 2,
                    new byte[,] { { 0, 0, 1, 0, 0 } }
                    ).SetName("AddOutline_1");

                yield return new TestCaseData(
                    new byte[,] { { 1, 2, 3 }, { 4, 5, 6 } },
                    1, 1,
                    new byte[,] { { 0, 0, 0, 0, 0 }, { 0, 1, 2, 3, 0 }, { 0, 4, 5, 6, 0 }, { 0, 0, 0, 0, 0 } }
                    ).SetName("AddOutline_1");
            }
        }

        [TestCaseSource(nameof(AddOutlineCases))]
        public void AddOutline(byte[,] original, int dX, int dY, byte[,] expected)
        {
            var actual = ImageProcessor.AddOutline(original, dX, dY);

            CollectionAssert.AreEqual(expected, actual);
        }

        private static IEnumerable<TestCaseData> GetBorderCoordinatesCases
        {
            get
            {
                yield return new TestCaseData(
                    new byte[,] { { 0, 0 }, { 10, 0 }, { 0, 10 } },
                    true,
                    new[] { new Point(1, 0), new Point(1, 1) }
                    ).SetName("GetBorderCoordinates_1");

                yield return new TestCaseData(
                    new byte[,] { { 0, 0, 0 }, { 20, 0, 10 }, { 0, 10, 0 }, { 10, 0, 0 } },
                    true,
                    new[] { new Point(1, 0), new Point(2, 1), new Point(1, 2) }
                    ).SetName("GetBorderCoordinates_2");
            }
        }

        [TestCaseSource(nameof(GetBorderCoordinatesCases))]
        public void GetBorderCoordinates(byte[,] input, bool borderIsVertical, Point[] expected)
        {
            int[][] diffs = ImageProcessor.GetDifferenceOnDirection(input, borderIsVertical);
            var result = ImageProcessor.GetBorderCoordinates(diffs, borderIsVertical);

            CollectionAssert.AreEqual(expected, result);
        }
    }
}