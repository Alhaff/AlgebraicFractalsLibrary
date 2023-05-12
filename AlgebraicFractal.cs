using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace AlgebraicFractals
{
    public abstract class AlgebraicFractal
    {
        protected Coord<double> _topLeft;
        public Coord<double> TopLeft 
        { 
            get => _topLeft; 
            set
            {
                _topLeft = value + _center;
            }
        }

        protected Coord<double> _bottomRight;
        public Coord<double> BottomRight 
        {
            get => _bottomRight;
            set
            {
                _bottomRight = value + _center;
            }
        }

        protected Coord<double> _center = new Coord<double>(0,0);

        public Coord<double> Center
        {
            get { return _center; }
            set { _center = value; }
        }

        public AlgebraicFractal(Coord<double> topLeftCoord, Coord<double> bottomRightCoord)
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

        public static void CreateMultiFractalSimple(AlgebraicFractal[] algebraicFractals, int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            if (algebraicFractals == null) return;
            var xScale = algebraicFractals.Select(fractal => 
                                        (fractal.BottomRight.X - fractal.TopLeft.X) / 
                                        (imageBottomRight.X - imageTopLeft.X)
                                                 ).ToArray();
            var yScale = algebraicFractals.Select(fractal =>
                                      (fractal.BottomRight.Y - fractal.TopLeft.Y) /
                                      (imageBottomRight.Y - imageTopLeft.Y)
                                               ).ToArray();
            var n = new int[algebraicFractals.Length];
            for (int y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
            {
                for (int x = imageTopLeft.X; x < imageBottomRight.X; x++)
                {
                    for(int i = 0; i < algebraicFractals.Length;i++)
                    {
                        n[i] = algebraicFractals[i].FractalEquasion(x * xScale[i] + algebraicFractals[i].TopLeft.X,
                                                                    y * yScale[i] + algebraicFractals[i].TopLeft.Y, MaxIterations);
                    }
                    image[y * imageWidth + x] = ColorINTFromIterationsAmount(n.Max());
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
        public static void CreateMultiFractalIntrinsics(AlgebraicFractal[] algebraicFractals, int[] image, int imageWidth,
           Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            if (algebraicFractals == null) return;
            var xScale = algebraicFractals.Select(fractal =>
                                       (fractal.BottomRight.X - fractal.TopLeft.X) /
                                        (imageBottomRight.X - imageTopLeft.X)
                                                 ).ToArray();
            var yScale = algebraicFractals.Select(fractal =>
                                      (fractal.BottomRight.Y - fractal.TopLeft.Y) /
                                      (imageBottomRight.Y - imageTopLeft.Y)
                                               ).ToArray();
            var yPos = algebraicFractals.Select(fractal => fractal.TopLeft.Y).ToArray();
            int yOffset = 0;
            int x, y;
            Vector256<double> _x_pos_offsets;
            Vector256<long> _iterations;
            _iterations = Vector256.Create((long)MaxIterations);
            var _x_scale = xScale.Select(x => Vector256.Create(x)).ToArray();
            var _x_jump = xScale.Select(x => Vector256.Create(x * 4)).ToArray();
            _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
            var _x_pos_offsetsArr = _x_scale.Select(x => Avx2.Multiply(_x_pos_offsets, x)).ToArray();
            var n = new Vector256<long>[algebraicFractals.Length];
            var _a = algebraicFractals.Select(f => Vector256.Create(f.TopLeft.X)).ToArray();
            var _x_pos = new Vector256<double>[algebraicFractals.Length];
            var _y_pos = new Vector256<double>[algebraicFractals.Length];
            for (y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
            {
                for(int i =0; i < _x_pos.Length; i++) _x_pos[i] = Avx2.Add(_a[i], _x_pos_offsetsArr[i]); 
                _y_pos = yPos.Select(y => Vector256.Create(y)).ToArray();
                for (x = imageTopLeft.X; x < imageBottomRight.X; x += 4)
                {
                    for (int i = 0; i < _x_pos.Length; i++) n[i] = algebraicFractals[i].FractalInstrictEquasion(_x_pos[i], _y_pos[i], _iterations);
                    for(int i =0; i < 4; i++)
                    {
                        if (yOffset + x + i< image.Length)
                            image[yOffset + x + i] = ColorINTFromIterationsAmount((int)(n.Select(_n => _n.AsInt64()[i]).Max()));
                    }
                    for (int i = 0; i < _x_pos.Length; i++) _x_pos[i] = Avx2.Add(_x_pos[i], _x_jump[i]);
                }
                for (int i = 0; i < _x_pos.Length; i++) yPos[i] += yScale[i];
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
        public class FractalMultiTreadContext
        {
            public AlgebraicFractal[] Fractals { get; init; }
            public int[] Image { get; init; }
            public int ImageWidth { get; init; }
            public double[] XScales { get; init; }
            public double[] YScales { get; init; }
            public Coord<int> ImageTL { get; init; }
            public Coord<int> ImageBR { get; init; }
            public int MaxIter { get; init; }
            public ManualResetEvent DoneEvent { get; init; }
            public FractalMultiTreadContext(
                AlgebraicFractal[] fractals,
                int[] image,
                int imageWidth,
                double[] xScales,
                double[] yScales,
                Coord<int> imageTL,
                Coord<int> imageBR,
                int maxIter,
                ManualResetEvent done)
            {
                Fractals = fractals; 
                Image = image;
                ImageWidth = imageWidth;
                XScales = xScales;
                YScales = yScales;
                ImageTL = imageTL;
                ImageBR = imageBR;
                MaxIter = maxIter;
                DoneEvent = done;
            }
        }

        public class FractalIntrinsincsMultiTreadContext
        {
            public FractalIntrinsincsMultiTreadContext(
                AlgebraicFractal[] fractals,
                int[] image,
                int imageWidth,
                Coord<double>[] fractalsTL,
                Coord<double>[] fractalsBR,
                Coord<int> imageTL,
                Coord<int> imageBR,
                int maxIter,
                ManualResetEvent doneEvent)
            {
                Fractals = fractals;
                Image = image;
                ImageWidth = imageWidth;
                FractalsTL = fractalsTL;
                FractalsBR = fractalsBR;
                ImageTL = imageTL;
                ImageBR = imageBR;
                MaxIter = maxIter;
                DoneEvent = doneEvent;
            }

            public AlgebraicFractal[] Fractals { get; init; }
            public int[] Image { get; init; }
            public int ImageWidth { get; init; }
            public Coord<double>[] FractalsTL { get; init; }
            public Coord<double>[] FractalsBR { get; init; }
            public Coord<int> ImageTL { get; init; }
            public Coord<int> ImageBR { get; init; }
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

        private static void CreateMultiFractalSimpleInThread(object context)
        {
            var ctx = context as FractalMultiTreadContext;
            var xScale = ctx.XScales;
            var yScale = ctx.YScales;
            var n = new int[ctx.Fractals.Length];
            for (int y = ctx.ImageTL.Y; y < ctx.ImageBR.Y; y++)
            {
                for (int x = ctx.ImageTL.X; x < ctx.ImageBR.X; x++)
                {
                    for (int i = 0; i < ctx.Fractals.Length; i++)
                    {
                        n[i] = ctx.Fractals[i].FractalEquasion(x * xScale[i] + ctx.Fractals[i].TopLeft.X,
                                                                    y * yScale[i] + ctx.Fractals[i].TopLeft.Y, ctx.MaxIter);
                    }
                    if (y + ctx.ImageWidth + x < ctx.Image.Length)
                        ctx.Image[y * ctx.ImageWidth + x] = ColorINTFromIterationsAmount(n.Max());
                }
            }
            ctx.DoneEvent.Set();
        }

        private void CreateFractalIntrinsicsInTread(object context)
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
                        lock (ctx.Image)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (yOffset + x + i < image.Length)
                                    image[yOffset + x + i] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[i]));
                            }
                        }
                    _x_pos = Avx2.Add(_x_pos, _x_jump);
                    }
                
                yPos += yScale;
                yOffset += imageWidth;
            }
            ctx.DoneEvent.Set();
        }

        private static void CreateMultiFractalIntrinsicsInTread(object context)
        {
            
            var ctx = context as FractalIntrinsincsMultiTreadContext;
            var yPos = ctx.FractalsTL.Select(pos => pos.Y).ToArray();
            int yOffset = 0;
            var imageTL = ctx.ImageTL;
            var imageBR = ctx.ImageBR;
            var fractalTL = ctx.FractalsTL;
            var fractalBR = ctx.FractalsBR;
            var image = ctx.Image;
            var imageWidth = ctx.ImageWidth;
            var xScale = new double[fractalBR.Length];
            var yScale = new double[fractalBR.Length];
            for (var i = 0; i < fractalBR.Length; i++)
            {
                xScale[i] = (fractalBR[i].X - fractalTL[i].X) / (imageBR.X - imageTL.X);
                yScale[i] = (fractalBR[i].Y - fractalTL[i].Y) / (imageBR.Y - imageTL.Y);
            }
            int x, y;
            Vector256<double> _x_pos_offsets;
            Vector256<long> _n, _iterations;
            Vector256<double>[] _a, _x_pos, _y_pos;
           _iterations = Vector256.Create((long)ctx.MaxIter);
            var _x_scale = xScale.Select(x => Vector256.Create(x)).ToArray();
            var _x_jump = xScale.Select(x => Vector256.Create(x * 4)).ToArray();
            _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
            var _x_pos_offsetsArr = _x_scale.Select(scale => Avx2.Multiply(_x_pos_offsets, scale)).ToArray();
            _x_pos = new Vector256<double>[_x_pos_offsetsArr.Length];
            Vector256<long>[] n = new Vector256<long>[ctx.Fractals.Length];
            for (y = imageTL.Y; y < imageBR.Y; y++)
            {
                _a = fractalTL.Select(tl => Vector256.Create(tl.X)).ToArray();
                for(int j =0; j < _x_pos.Length;j++)
                {
                    _x_pos[j] = Avx2.Add(_a[j], _x_pos_offsetsArr[j]);
                }
                _y_pos = yPos.Select( pos => Vector256.Create(pos)).ToArray();

                for (x = imageTL.X; x < imageBR.X; x += 4)
                {
                    for (int i = 0; i < _x_pos.Length; i++) n[i] = ctx.Fractals[i].FractalInstrictEquasion(_x_pos[i], _y_pos[i], _iterations);
                    lock (ctx.Image)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            if (yOffset + x + i < image.Length)
                                image[yOffset + x + i] = ColorINTFromIterationsAmount((int)(n.Select(_n => _n.AsInt64()[i]).Max()));
                        }
                    }
                    for (int i = 0; i < _x_pos.Length; i++) _x_pos[i] = Avx2.Add(_x_pos[i], _x_jump[i]);
                }
                for (int j = 0; j < yPos.Length; j++) yPos[j] += yScale[j];
                yOffset += imageWidth;
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
        public void CreateFractalIntrinsicsInThreadPool(int[] image, int imageWidth,
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
        public static void CreateMultiFractalSimpleInThreadPool(AlgebraicFractal[] fractals, int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            if (fractals == null) return;
            int ThreadCount = fractals.Select(fr => fr.ThreadCount).Max();
            int nSectionWidth = (int)((imageBottomRight.X - imageTopLeft.X) / (double)ThreadCount);
            var dFractalWidth = fractals.Select(fr => (fr.BottomRight.X - fr.TopLeft.X) / (double)ThreadCount).ToArray();
            ManualResetEvent[] dones = new ManualResetEvent[ThreadCount];
            var xScales = fractals.Select(fr => (fr.BottomRight.X - fr.TopLeft.X) / (double)(imageBottomRight.X - imageTopLeft.X)).ToArray();
            var yScales = fractals.Select(fr => (fr.BottomRight.Y - fr.TopLeft.Y) / (double)(imageBottomRight.Y - imageTopLeft.Y)).ToArray();
            for (int i = 0; i < ThreadCount; i++)
            {
                dones[i] = new ManualResetEvent(false);
                var ImBr = i == ThreadCount - 1 ? imageBottomRight.X : imageTopLeft.X + nSectionWidth * (i + 1);
                var context = new FractalMultiTreadContext(
                           fractals,
                           image,
                           imageWidth,
                           xScales,
                           yScales,
                           new Coord<int>(imageTopLeft.X + nSectionWidth * i, imageTopLeft.Y),
                           new Coord<int>(ImBr, imageBottomRight.Y),
                           MaxIterations,
                           dones[i]);
                    ThreadPool.QueueUserWorkItem(CreateMultiFractalSimpleInThread, context);
                
            }

            foreach (var e in dones)
                e.WaitOne();
        }

        public static void CreateMultiFractalIntrinsicsInThreadPool(AlgebraicFractal[] fractals, int[] image, int imageWidth,
          Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            if (fractals == null) return;
            int ThreadCount = fractals.Select(fr => fr.ThreadCount).Max();
            int nSectionWidth = (int)((imageBottomRight.X - imageTopLeft.X) / (double)ThreadCount);
            var dFractalWidth = fractals.Select(fr => (fr.BottomRight.X - fr.TopLeft.X) / (double)ThreadCount).ToArray();
            ManualResetEvent[] dones = new ManualResetEvent[ThreadCount];
            
            for (int i = 0; i < ThreadCount; i++)
            {
                dones[i] = new ManualResetEvent(false);
                var ImBrX = i == ThreadCount - 1 ? imageBottomRight.X : imageTopLeft.X + nSectionWidth * (i + 1);
                var ImTlX = imageTopLeft.X + nSectionWidth * i;
                var fractalsTL = new Coord<double>[fractals.Length];
                var fractalsBR = new Coord<double>[fractals.Length];
                for (int j = 0; j < fractals.Length;j++ )
                {
                    
                    var FrBrX = i == ThreadCount - 1 ? fractals[j].BottomRight.X : fractals[j].TopLeft.X + dFractalWidth[j] * (i + 1);
                    var FrTlX = fractals[j].TopLeft.X + dFractalWidth[j] * i;
                    fractalsTL[j] = new Coord<double>(FrTlX, fractals[j].TopLeft.Y);
                    fractalsBR[j] = new Coord<double>(FrBrX, fractals[j].BottomRight.Y);
                }
               
                var context = new FractalIntrinsincsMultiTreadContext(
                           fractals,
                           image,
                           imageWidth,
                           fractalsTL,
                           fractalsBR,
                           new Coord<int>(ImTlX, imageTopLeft.Y),
                           new Coord<int>(ImBrX, imageBottomRight.Y),
                           MaxIterations,
                           dones[i]);
                ThreadPool.QueueUserWorkItem(CreateMultiFractalIntrinsicsInTread, context);

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
