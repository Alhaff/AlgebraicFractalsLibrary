using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals.Fractals
{
    public class JuliaSet : AlgebraicFractal
    {
        public JuliaSet()
            : base(new Coord<double>(-2, -1.5), new Coord<double>(1, 1.5))
        {
        }

        public override int FractalEquasion(double x, double y, double MaxIterations)
        {
            Complex C = new Complex(0.36, 0.36);
            Complex Z = new Complex(x, y);
            int n = 0;
            while (Z.Magnitude < 2.0 && n < MaxIterations)
            {
                Z = Z * Z + C;
                n++;
            }
            return n;
        }

        public override Vector256<long> FractalInstrictEquasion(Vector256<double> x_pos, Vector256<double> y_pos, Vector256<long> maxIter)
        {
            var _cr = Vector256.Create(0.36d);
            var _ci = Vector256.Create(0.36d);
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
                _zr2 = Avx.Multiply(_zr, _zr);
                _zi2 = Avx.Multiply(_zi, _zi);
                _a = Avx.Subtract(_zr2, _zi2);
                _a = Avx.Add(_a, _cr);
                _b = Avx.Multiply(_zr, _zi);
                _b = Avx.Multiply(_b, TWO);
                _b = Avx.Add(_b, _ci);
                _zr = _a;
                _zi = _b;
                _a = Avx.Add(_zr2, _zi2);
                _mask1 = Avx.CompareLessThan(_a, FOUR);
                _mask2 = Avx2.CompareGreaterThan(maxIter, _n);
                _mask2 = Avx2.And(_mask2, _mask1.AsInt64());
                _c = Avx2.And(ONE, _mask2); // Zero out ones where n < iterations													
                _n = Avx2.Add(_n, _c); // n++ Increase all n
            } while (Avx.MoveMask(Vector256.ConvertToDouble(_mask2)) > 0);
            return _n;
        }

    }
}
