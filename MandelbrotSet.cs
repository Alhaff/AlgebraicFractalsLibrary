using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals
{
    public class MandelbrotSet : AlgebraicFractalBase
    {
        public MandelbrotSet() : base(new Coord<double>(-2,-1), new Coord<double>(1,1))
        {
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
