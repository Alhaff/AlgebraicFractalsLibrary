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
    public class Newton : AlgebraicFractal
    {
        public double Eps { get; set; } = 0.00001; 
        public Newton(double eps = 0.00001) : base(new Coord<double>(-1,-1), new Coord<double>(1,1))
        {
            Eps = eps;
            Caption = "Фрактал Ньютона";
        }

        public override int FractalEquasion(double x, double y, double MaxIterations)
        {
            Complex Z = new Complex(x,y);
            Complex z3 = Complex.Pow(Z, 3);
            Complex tmp;
            int n = 0;
            do
            {
                tmp = z3 * Z;
                Z = (3 * tmp + Complex.One) / (4 * z3);
                z3 = Complex.Pow(Z, 3);
                tmp = tmp - Complex.One;
                n++;
            } while (Math.Abs(tmp.Real) > Eps && Math.Abs(tmp.Imaginary) > Eps &&  n < MaxIterations);
            return n;   
        }

        public override Vector256<long> FractalInstrictEquasion(Vector256<double> x_pos, Vector256<double> y_pos, Vector256<long> maxIter)
        {
            var _zr = x_pos;
            var _zi = y_pos;
            var _n = Vector256.Create(0l);
            Vector256<double> _mask1, _a, _zr2, _zi2,_zr3,_zi3, _one;
            Vector256<long> _c, _mask2, ONE;
            Vector256<double> EPS, THREE,FOUR;
            _one = Vector256.Create(1d);
            ONE = Vector256.Create(1l);
            THREE = Vector256.Create(3d);
            FOUR = Vector256.Create(4d);
            EPS = Vector256.Create(Eps);
            do
            {
                (_zr3,_zi3) = IntrinsicsComplexMath.Pow((_zr, _zi), 3);
                (_zr2, _zi2) = IntrinsicsComplexMath.Multiply((_zr3, _zi3), (_zr, _zi));
                _zr2 = Avx2.Subtract(_zr2, _one);
                _zr2 = Vector256.Abs<double>(_zr2);
                _zi2 = Vector256.Abs<double>(_zi2);
                (_zr, _zi) = IntrinsicsComplexMath.Multiply((_zr3, _zi3), (_zr, _zi));
                _zr = Avx2.Multiply(_zr, THREE);
                _zi = Avx2.Multiply(_zi, THREE);
                _zr = Avx2.Add(_zr, _one);
                _zr3 = Avx2.Multiply(_zr3, FOUR);
                _zi3 = Avx2.Multiply(_zi3, FOUR);
                (_zr, _zi) = IntrinsicsComplexMath.Divide((_zr, _zi), (_zr3, _zi3));
                _mask1 = Avx.CompareGreaterThan(_zr2, EPS);
                _a = Avx2.CompareGreaterThan(_zi2, EPS);
                _mask1 = Avx2.And(_mask1, _a);
                _mask2 = Avx2.CompareGreaterThan(maxIter, _n);
                _mask2 = Avx2.And(_mask2, _mask1.AsInt64());
                _c = Avx2.And(ONE, _mask2); // Zero out ones where n < iterations													
                _n = Avx2.Add(_n, _c); // n++ Increase all n
            } while (Avx.MoveMask(Vector256.ConvertToDouble(_mask2)) > 0);
            return _n;
        }
    }
}
