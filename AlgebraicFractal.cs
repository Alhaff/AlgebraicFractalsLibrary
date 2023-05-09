﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
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

        #region BaseCreateFractal
        /// <summary>
        /// Must return iterations count from input coord x, y
        /// </summary>
        /// <param name="x">X point coord</param>
        /// <param name="y">Y point coord</param>
        /// <param name="MaxIterations"> Max iterations amount on current iterations</param>
        /// <returns> iteration count</returns>
        public abstract int FractalEquasion(double x, double y, double MaxIterations);

        public abstract Vector256<long> FractalInstrictEquasion(Vector256<double> x_pos, Vector256<double> y_pos, Vector256<long> maxIter);

        /// <summary>
        /// Fill Image pixels with color value obtains after FractalEquasion
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageTopLeft"></param>
        /// <param name="imageBottomRight"></param>
        /// <param name="MaxIterations"></param>
        public void CreateFractalSimple(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            double xScale = (BottomRight.X - TopLeft.X) / (imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (imageBottomRight.Y - imageTopLeft.Y);

            for (int y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
            {
                for (int x = imageTopLeft.X; x < imageBottomRight.X; x++)
                {
                    image[y * imageWidth + x] = ColorINTFromIterationsAmount(
                        FractalEquasion(x * xScale + TopLeft.X, y * yScale + TopLeft.Y, MaxIterations));
                }
            }
        }

        /// <summary>
        /// Fill Image pixels with color value used AVX2
        /// </summary>
        /// <param name="image"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageTopLeft"></param>
        /// <param name="imageBottomRight"></param>
        /// <param name="MaxIterations"></param>
        public  void CreateFractalIntrinsics(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            double xScale = (BottomRight.X - TopLeft.X) / (imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (imageBottomRight.Y - imageTopLeft.Y);
            double yPos = TopLeft.Y;
            int yOffset = 0;
            int x, y;
           
                Vector256<double> _a, _y_pos;
                Vector256<double> _x_pos_offsets, _x_pos, _x_scale, _x_jump;
                Vector256<long> _n, _iterations;
                _iterations = Vector256.Create((long)MaxIterations);
                _x_scale = Vector256.Create(xScale);
                _x_jump = Vector256.Create(xScale * 4);
                _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
                _x_pos_offsets = Avx2.Multiply(_x_pos_offsets, _x_scale);

                for (y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
                {
                    _a = Vector256.Create(TopLeft.X);
                    _x_pos = Avx2.Add(_a, _x_pos_offsets);
                    _y_pos = Vector256.Create(yPos);

                    for (x = imageTopLeft.X; x < imageBottomRight.X; x += 4)
                    {
                        _n = FractalInstrictEquasion(_x_pos, _y_pos, _iterations);
                        if (yOffset + x < image.Length) image[yOffset + x + 0] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[0]));
                        if (yOffset + x + 1 < image.Length) image[yOffset + x + 1] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[1]));
                        if (yOffset + x + 2 < image.Length) image[yOffset + x + 2] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[2]));
                        if (yOffset + x + 3 < image.Length) image[yOffset + x + 3] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[3]));
                        _x_pos = Avx2.Add(_x_pos, _x_jump);
                    }
                    yPos += yScale;
                    yOffset += imageWidth;
                }
            
        }
        #endregion


        #region ThreadWork

        public int ThreadCount { get; set; } = 32;
        public class FractalThreadContext
        {
            public FractalThreadContext(int[] image, int imageWidth,
                Coord<double> fractalTL, Coord<double> fractalBR,
                Coord<int> imageTL, Coord<int> imageBR,
                double xScale, double yScale,int maxIter, ManualResetEvent done)
            {
                Image = image;
                ImageWidth = imageWidth;
                FractalTL = fractalTL;
                FractalBR = fractalBR;
                ImageTL = imageTL;
                ImageBR = imageBR;
                XScale = xScale;
                YScale = yScale;
                MaxIter = maxIter;
                DoneEvent = done;
            }

            public int[] Image { get; init; }
            public int ImageWidth { get; init; }
            public Coord<double> FractalTL { get; init; }
            public Coord<double> FractalBR { get; init; }
            public Coord<int> ImageTL { get; init; }
            public Coord<int> ImageBR { get; init; }
            public double XScale { get; init; }
            public double YScale { get; init; }
            public int MaxIter { get; init; }
            public ManualResetEvent DoneEvent { get; init; }
        } 
        private void CreateFractalSimpleInThread(object context)
        {
            var ctx =  context as FractalThreadContext;
            int[] image = ctx.Image;
            int imageWidth = ctx.ImageWidth;
            Coord<double> fractalTopLeft = ctx.FractalTL;
            Coord<double> fractalBottomRight = ctx.FractalBR;
            Coord<int> imageTopLeft = ctx.ImageTL; 
            Coord<int> imageBottomRight = ctx.ImageBR;
            int MaxIterations = ctx.MaxIter;
            double xScale = ctx.XScale;
            double yScale = ctx.YScale;
            for (int y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
            {
                for (int x = imageTopLeft.X; x < imageBottomRight.X; x++)
                {
                    if(y + imageWidth + x < image.Length)
                        image[y * imageWidth + x] = ColorINTFromIterationsAmount(
                        FractalEquasion((x * xScale) + TopLeft.X, (y*yScale) + TopLeft.Y, MaxIterations));
                }
            }
            ctx.DoneEvent.Set();
        }

        public void CreateFractalSimpleInThreadPool(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            int nSectionWidth = (int)((imageBottomRight.X - imageTopLeft.X) / (double)ThreadCount);
            double dFractalWidth = (BottomRight.X - TopLeft.X) / (double)ThreadCount;
            double xScale = (BottomRight.X - TopLeft.X) / (double)(imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (double)(imageBottomRight.Y - imageTopLeft.Y);
            ManualResetEvent[] dones = new ManualResetEvent[ThreadCount];
            for(int i =0; i < ThreadCount;i++)
            {
                dones[i] = new ManualResetEvent(false);
                var FrBr = i == ThreadCount - 1 ? BottomRight.X : TopLeft.X + dFractalWidth * (i + 1);
                var ImBr = i == ThreadCount - 1 ? imageBottomRight.X : imageTopLeft.X + nSectionWidth * (i + 1);
                var context = new FractalThreadContext(
                       image,
                       imageWidth,
                       new Coord<double>(TopLeft.X + dFractalWidth * i, TopLeft.Y),
                       new Coord<double>(FrBr, BottomRight.Y),
                       new Coord<int>(imageTopLeft.X + nSectionWidth * i, imageTopLeft.Y),
                       new Coord<int>(ImBr, imageBottomRight.Y),
                       xScale, yScale,
                       MaxIterations,
                       dones[i]);
                ThreadPool.QueueUserWorkItem(CreateFractalSimpleInThread, context);
            }
          
            foreach (var e in dones)
                e.WaitOne();
        }

        protected void CreateFractalIntrinsicsInTread(object context)
        {
            var ctx = context as FractalThreadContext;
            double yPos = ctx.FractalTL.Y;
            int yOffset = 0;
            var imageTL = ctx.ImageTL;
            var imageBR = ctx.ImageBR;
            var fractalTL = ctx.FractalTL;
            var fractalBR = ctx.FractalBR;
            var image = ctx.Image;
            var imageWidth = ctx.ImageWidth;
            double xScale = (fractalBR.X - fractalTL.X) / (imageBR.X - imageTL.X);
            double yScale = (fractalBR.Y - fractalTL.Y) / (imageBR.Y - imageTL.Y);
            int x, y;
            Vector256<double> _a, _b, _y_pos;
            Vector256<double> _x_pos_offsets, _x_pos, _x_scale, _x_jump;
            Vector256<long> _n, _iterations;

            _iterations = Vector256.Create((long)ctx.MaxIter);
            _x_scale = Vector256.Create(xScale);
            _x_jump = Vector256.Create(xScale * 4);
            _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
            _x_pos_offsets = Avx2.Multiply(_x_pos_offsets, _x_scale);

            for (y = imageTL.Y; y < imageBR.Y; y++)
            {
                _a = Vector256.Create(fractalTL.X);
                _x_pos = Avx2.Add(_a, _x_pos_offsets);
                _y_pos = Vector256.Create(yPos);
                
                for (x = imageTL.X; x < imageBR.X; x += 4)
                {
                    _n = FractalInstrictEquasion(_x_pos, _y_pos, _iterations);
                    if (yOffset + x < image.Length) image[yOffset + x + 0] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[0]));
                    if (yOffset + x + 1 < image.Length) image[yOffset + x + 1] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[1]));
                    if (yOffset + x + 2 < image.Length) image[yOffset + x + 2] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[2]));
                    if (yOffset + x + 3 < image.Length) image[yOffset + x + 3] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[3]));
                    _x_pos = Avx2.Add(_x_pos, _x_jump);
                 }
                 yPos += yScale;
                 yOffset += imageWidth;
            }
            ctx.DoneEvent.Set();
        }

        public void CreateFractalIntrinsicsThreadPool(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            int nSectionWidth = (int)((imageBottomRight.X - imageTopLeft.X) / (double)ThreadCount);
            double dFractalWidth = (BottomRight.X - TopLeft.X) / (double)ThreadCount;
            double xScale = (BottomRight.X - TopLeft.X) / (double)(imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (double)(imageBottomRight.Y - imageTopLeft.Y);
            ManualResetEvent[] dones = new ManualResetEvent[ThreadCount];
          
            for (int i = 0; i < ThreadCount; i++)
            {
                dones[i] = new ManualResetEvent(false);
                var FrBr = i == ThreadCount - 1 ? BottomRight.X : TopLeft.X + dFractalWidth * (i + 1);
                var ImBr = i == ThreadCount - 1 ? imageBottomRight.X : imageTopLeft.X + nSectionWidth * (i + 1);
                var context = new FractalThreadContext(
                       image,
                       imageWidth,
                       new Coord<double>(TopLeft.X + dFractalWidth * i, TopLeft.Y),
                       new Coord<double>(FrBr, BottomRight.Y),
                       new Coord<int>(imageTopLeft.X + nSectionWidth * i, imageTopLeft.Y),
                       new Coord<int>(ImBr, imageBottomRight.Y),
                       xScale, yScale,
                       MaxIterations,
                       dones[i]);
                ThreadPool.QueueUserWorkItem(CreateFractalIntrinsicsInTread, context);
            }
            foreach (var e in dones)
                e.WaitOne();
        }
        #endregion
        #region Color
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
        #endregion
    }
}