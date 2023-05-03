using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using static System.Net.Mime.MediaTypeNames;

namespace AlgebraicFractals
{
    public class MandelbrotSet : AlgebraicFractalBase
    {
        public MandelbrotSet() : base(new Coord<double>(-2,-1), new Coord<double>(1,1))
        {
        }

        public override void CreateFractalIntrinsics(int[] image, int imageWidth,
            Coord<int> imageTopLeft, Coord<int> imageBottomRight, int MaxIterations)
        {
            double xScale = (BottomRight.X - TopLeft.X) / (imageBottomRight.X - imageTopLeft.X);
            double yScale = (BottomRight.Y - TopLeft.Y) / (imageBottomRight.Y - imageTopLeft.Y);
            double yPos = TopLeft.Y;
            int yOffset = 0;
            int x, y;
            unsafe
            {
                Vector256<double> _a, _b, _two, _four, _mask1;
                Vector256<double> _zr, _zi, _zr2, _zi2, _cr, _ci;
                Vector256<double> _x_pos_offsets, _x_pos, _x_scale, _x_jump;
                Vector256<long> _one, _c, _n, _iterations, _mask2;

                _one = Vector256.Create(1l);
                _two = Vector256.Create(2.0);
                _four = Vector256.Create(4.0);
                _iterations = Vector256.Create((long)MaxIterations);

                _x_scale = Vector256.Create(xScale);
                _x_jump = Vector256.Create(xScale * 4);
                _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
                _x_pos_offsets = Avx2.Multiply(_x_pos_offsets, _x_scale);

                for (y = imageTopLeft.Y; y < imageBottomRight.Y; y++)
                {
                    _a = Vector256.Create(TopLeft.X);
                    _x_pos = Avx2.Add(_a, _x_pos_offsets);
                    _ci = Vector256.Create(yPos);

                    for (x = imageTopLeft.X; x < imageBottomRight.X; x+=4)
                    {
                        _cr = _x_pos;
                        _zr = Vector256.Create(0d);
                        _zi = Vector256.Create(0d);
                        _n = Vector256.Create(0l);
                       
                        do
                        {
                            _zr2 = Avx2.Multiply(_zr, _zr);
                            _zi2 = Avx2.Multiply(_zi, _zi);
                            _a = Avx2.Subtract(_zr2, _zi2);
                            _a = Avx2.Add(_a, _cr);
                            _b = Avx2.Multiply(_zr, _zi);
                            _b = Avx2.Multiply(_b, _two);
                            _b = Avx2.Add(_b, _ci);
                            _zr = _a;
                            _zi = _b;
                            _a = Avx2.Add(_zr2, _zi2);
                            _mask1 = Avx2.CompareLessThan(_a, _four);
                            _mask2 = Avx2.CompareGreaterThan(_iterations, _n);
                            _mask2 = Avx2.And(_mask2, _mask1.AsInt64());
                            _c = Avx2.And(_one, _mask2); // Zero out ones where n < iterations													
                            _n = Avx2.Add(_n, _c); // n++ Increase all n
                        } while (Avx2.MoveMask(Vector256.ConvertToDouble(_mask2)) > 0);

                        if(yOffset + x < image.Length) image[yOffset + x + 0] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[0]));
                        if (yOffset + x +1 < image.Length) image[yOffset + x + 1] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[1]));
                        if (yOffset + x +2< image.Length) image[yOffset + x + 2] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[2]));
                        if (yOffset + x +3< image.Length) image[yOffset + x + 3] = ColorINTFromIterationsAmount((int)(_n.AsInt64()[3]));
                        _x_pos = Avx2.Add(_x_pos, _x_jump);
                    }
                    yPos += yScale;
                    yOffset += imageWidth;
                }
            }

        }

        protected override void CreateFractalIntrinsicsInTread(object context)
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
            unsafe
            {
                Vector256<double> _a, _b, _two, _four, _mask1;
                Vector256<double> _zr, _zi, _zr2, _zi2, _cr, _ci;
                Vector256<double> _x_pos_offsets, _x_pos, _x_scale, _x_jump;
                Vector256<long> _one, _c, _n, _iterations, _mask2;

                _one = Vector256.Create(1l);
                _two = Vector256.Create(2.0);
                _four = Vector256.Create(4.0);
                _iterations = Vector256.Create((long)ctx.MaxIter);

                _x_scale = Vector256.Create(xScale);
                _x_jump = Vector256.Create(xScale * 4);
                _x_pos_offsets = Vector256.Create(0d, 1d, 2d, 3d);
                _x_pos_offsets = Avx2.Multiply(_x_pos_offsets, _x_scale);

                for (y = imageTL.Y; y < imageBR.Y; y++)
                {
                    _a = Vector256.Create(fractalTL.X);
                    _x_pos = Avx2.Add(_a, _x_pos_offsets);
                    _ci = Vector256.Create(yPos);

                    for (x = imageTL.X; x < imageBR.X; x += 4)
                    {
                        _cr = _x_pos;
                        _zr = Vector256.Create(0d);
                        _zi = Vector256.Create(0d);
                        _n = Vector256.Create(0l);

                        do
                        {
                            _zr2 = Avx2.Multiply(_zr, _zr);
                            _zi2 = Avx2.Multiply(_zi, _zi);
                            _a = Avx2.Subtract(_zr2, _zi2);
                            _a = Avx2.Add(_a, _cr);
                            _b = Avx2.Multiply(_zr, _zi);
                            _b = Avx2.Multiply(_b, _two);
                            _b = Avx2.Add(_b, _ci);
                            _zr = _a;
                            _zi = _b;
                            _a = Avx2.Add(_zr2, _zi2);
                            _mask1 = Avx2.CompareLessThan(_a, _four);
                            _mask2 = Avx2.CompareGreaterThan(_iterations, _n);
                            _mask2 = Avx2.And(_mask2, _mask1.AsInt64());
                            _c = Avx2.And(_one, _mask2); // Zero out ones where n < iterations													
                            _n = Avx2.Add(_n, _c); // n++ Increase all n
                        } while (Avx2.MoveMask(Vector256.ConvertToDouble(_mask2)) > 0);

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
            ctx.DoneEvent.Set();
        }

        public override int FractalEquasion(double x, double y, double MaxIterations)
        {
            Complex C = new Complex(x,y);
            Complex Z = new Complex(0,0);
            int n = 0;
            while (Z.Magnitude < 2.0 && n < MaxIterations)
            {
                Z = (Z * Z) + C;
                n++;
            }
            return n;
        }
    }
}
