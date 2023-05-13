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
    public class MandelbrotSetNPow : AlgebraicFractal
    {
        private int _power = 2;
        public int Power
        {
            get => _power;
            set
            {
                if (value > 0) _power = value;
            }
        }
        public MandelbrotSetNPow(int pow = 3) : base(new Coord<double>(-2, -1), new Coord<double>(1, 1))
        {
            Power = pow;
            Caption = "Фрактал Мандельброта N cтупіння";
        }

        public override int FractalEquasion(double x, double y, double MaxIterations)
        {
            Complex C = new Complex(x, y);
            Complex Z = new Complex(x, y);
            int n = 0;
            while (Z.Magnitude <= 2.0 && n < MaxIterations)
            {
                Z = Complex.Pow(Z, Power) + C;
                n++;
            }
            return n;
        }

        public override Vector256<long> FractalInstrictEquasion(Vector256<double> x_pos, Vector256<double> y_pos, Vector256<long> maxIter)
        {
            var _cr = x_pos;
            var _ci = y_pos;
            var _zr = Vector256.Create(0d);
            var _zi = Vector256.Create(0d);
            var _n = Vector256.Create(0l);
            Vector256<double> _mask1, _a, _b, _zr2, _zi2, tmp;
            Vector256<long> _c, _mask2, ONE;
            Vector256<double> TWO, FOUR;
            TWO = Vector256.Create(2d);
            FOUR = Vector256.Create(4d);
            ONE = Vector256.Create(1l);

            do
            {
                _zr2 = Avx.Multiply(_zr, _zr);
                _zi2 = Avx.Multiply(_zi, _zi);
                _a = Avx.Add(_zr2, _zi2);
                (_zr, _zi) = IntrinsicsComplexMath.Pow((_zr, _zi), Power);
                (_zr, _zi) = IntrinsicsComplexMath.Add((_zr, _zi), (_cr, _ci));
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
