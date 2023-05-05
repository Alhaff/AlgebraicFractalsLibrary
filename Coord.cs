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
        #region Operators

        #region Operator+
        static public Coord<T> operator+(Coord<T> one, Coord<T> two) 
            => new Coord<T>(one.X+two.X, one.Y+two.Y);
        static public Coord<T> operator +(Coord<T> one, T two)
            => new Coord<T>(one.X + two, one.Y + two);
        static public Coord<T> operator +(T one, Coord<T> two)
            => new Coord<T>(one + two.X, one + two.Y);
        #endregion

        #region Operator-
        static public Coord<T> operator -(Coord<T> one, Coord<T> two)
            => new Coord<T>(one.X - two.X, one.Y - two.Y);
        static public Coord<T> operator -(Coord<T> one, T two)
           => new Coord<T>(one.X - two, one.Y - two);
        static public Coord<T> operator -(T one, Coord<T> two)
            => new Coord<T>(one - two.X, one - two.Y);
        #endregion

        #region Operator*
        static public Coord<T> operator *(Coord<T> one, Coord<T> two)
            => new Coord<T>(one.X * two.X, one.Y * two.Y);
        static public Coord<T> operator *(Coord<T> one, T two)
            => new Coord<T>(one.X * two, one.Y * two);
        static public Coord<T> operator *(T one, Coord<T> two)
            => new Coord<T>(one * two.X, one * two.Y);
        #endregion

        #region Operator/
        static public Coord<T> operator /(Coord<T> one, Coord<T> two)
            => new Coord<T>(one.X / two.X, one.X / two.X);
        static public Coord<T> operator /(Coord<T> one, T two)
           => new Coord<T>(one.X / two, one.X / two);
        #endregion

        #endregion
    }
}
