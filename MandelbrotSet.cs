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
    public class MandelbrotSet : AlgebraicFractal
    {
        public MandelbrotSet() : base(new Coord<double>(-2,-1), new Coord<double>(1,1))
        {
        }
        public override int FractalEquasion(double x, double y, double MaxIterations)
        {
            Complex C = new Complex(x,y);
            Complex Z = new Complex(x,y);
            int n = 0;
            while (Z.Magnitude < 2.0 && n < MaxIterations)
            {
                Z = (Z * Z) + C;
                n++;
            }
            return n;
        }

        public override Vector256<long> FractalInstrictEquasion(Vector256<double> x_pos, Vector256<double> y_pos, Vector256<long> maxIter)
        {
            var _cr = x_pos;
            var _ci = y_pos;
            var _zr = x_pos;
            var _zi = y_pos;
            var _n = Vector256.Create(0l);
            Vector256<double> _mask1, _a, _b, _zr2, _zi2;
            Vector256<long> _c, _mask2, ONE;
            Vector256<double> TWO, FOUR;
            TWO = Vector256.Create(2d);
            FOUR = Vector256.Create(4d);
            ONE = Vector256.Create(1l);
            do
            {
                _zr2 = Avx2.Multiply(_zr, _zr);
                _zi2 = Avx2.Multiply(_zi, _zi);
                _a = Avx2.Subtract(_zr2, _zi2);
                _a = Avx2.Add(_a, _cr);
                _b = Avx2.Multiply(_zr, _zi);
                _b = Avx2.Multiply(_b, TWO);
                _b = Avx2.Add(_b, _ci);
                _zr = _a;
                _zi = _b;
                _a = Avx2.Add(_zr2, _zi2);
                _mask1 = Avx2.CompareLessThan(_a, FOUR);
                _mask2 = Avx2.CompareGreaterThan(maxIter, _n);
                _mask2 = Avx2.And(_mask2, _mask1.AsInt64());
                _c = Avx2.And(ONE, _mask2); // Zero out ones where n < iterations													
                _n = Avx2.Add(_n, _c); // n++ Increase all n
            } while (Avx2.MoveMask(Vector256.ConvertToDouble(_mask2)) > 0);
            return _n;
        }
    }
}
