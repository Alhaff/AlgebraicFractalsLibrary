using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals
{
    public struct Coord<T> where T : INumber<T>
    {
        public Coord(T x, T y)
        {
            X = x;
            Y = y;
        }
        public T X { get; init; }
        public T Y { get; init; }
    }
}
