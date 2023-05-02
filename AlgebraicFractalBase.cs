using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals
{
    public abstract class AlgebraicFractalBase
    {
        public Coord<double> TopLeft { get; set; }

        public Coord<double> BottomRight { get; set; }

        public AlgebraicFractalBase(Coord<double> topLeftCoord, Coord<double> bottomRightCoord)
        {
            TopLeft = topLeftCoord;
            BottomRight = bottomRightCoord;
        }
        /// <summary>
        /// Must return iterations count from input coord x, y
        /// </summary>
        /// <param name="x">X point coord</param>
        /// <param name="y">Y point coord</param>
        /// <param name="MaxIterations"> Max iterations amount on current iterations</param>
        /// <returns> iteration count</returns>
        public abstract int FractalEquasion(double x, double y, double MaxIterations);

        public void CreateFractalSimple(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            double xScale = (BottomRight.X - TopLeft.X) / (imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (imageBottomRight.Y - imageTopLeft.Y);

            for (int y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
            {
                for (int x = imageTopLeft.X; x < imageBottomRight.X; x++)
                {
                    image[y * imageWidth + x] = 
                        FractalEquasion(x * xScale + TopLeft.X, y * yScale + TopLeft.Y, MaxIterations);
                }
            }
        }

        public static int ColorINTFromIterationsAmount(double n, double alpha = 0.1d)
        {
            double red = 0.5d * Math.Sin(alpha * n) + 0.5d;
            double green = 0.5d * Math.Sin(alpha * n + 2.094d) + 0.5d;
            double blue = 0.5d * Math.Sin(alpha * n + 4.188d) + 0.5d;
            return (255 << 24) | ((int)(red * 255) << 16) | ((int)(green * 255) << 8) | ((int)(blue * 255));
        }
        public static (int,int,int) ColorRGBFromIterationsAmount(double n, double alpha = 0.1d)
        {
            double red = 0.5d * Math.Sin(alpha * n) + 0.5d;
            double green = 0.5d * Math.Sin(alpha * n + 2.094d) + 0.5d;
            double blue = 0.5d * Math.Sin(alpha * n + 4.188d) + 0.5d;
            return ((int)(red * 255) , (int)(green * 255), (int)(blue * 255));
        }
    }
}
