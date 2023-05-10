using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals
{
    static public class IntrinsicsComplexMath
    {
        #region IntrinsicsCoplexOperation

        public static (Vector256<double>, Vector256<double>) Add((Vector256<double>, Vector256<double>) z1,
                                                                              (Vector256<double>, Vector256<double>) z2)
        {
            var zr = Avx2.Add(z1.Item1, z2.Item1);
            var zi = Avx2.Add(z1.Item2, z2.Item2);
            return (zr, zi);
        }

        public static void Add(ref Vector256<double> z1R, ref Vector256<double> z1I, 
                               ref Vector256<double> z2R, ref Vector256<double> z2I,
                               out Vector256<double> zR, out Vector256<double> zI) 
        { 
           zR = Avx2.Add(z1R, z2R);
           zI = Avx2.Add(z1I, z2I);
        }
        public static (Vector256<double>, Vector256<double>) Subtract((Vector256<double>, Vector256<double>) z1,
                                                                             (Vector256<double>, Vector256<double>) z2)
        {
            var zr = Avx2.Subtract(z1.Item1, z2.Item1);
            var zi = Avx2.Subtract(z1.Item2, z2.Item2);
            return (zr, zi);
        }
        public static void Subtract(ref Vector256<double> z1R, ref Vector256<double> z1I,
                                    ref Vector256<double> z2R, ref Vector256<double> z2I,
                                    out Vector256<double> zR, out Vector256<double> zI)
        {
            zR = Avx2.Subtract(z1R, z2R);
            zI = Avx2.Subtract(z1I, z2I);
        }
        public static (Vector256<double>, Vector256<double>) Multiply((Vector256<double>, Vector256<double>) z1,
                                                                             (Vector256<double>, Vector256<double>) z2)
        {
            var zr = Avx2.Multiply(z1.Item1, z2.Item1);
            var tmp = Avx2.Multiply(z1.Item2, z2.Item2);
            zr = Avx2.Subtract(zr, tmp);
            var zi = Avx2.Multiply(z1.Item2, z2.Item1);
            tmp = Avx2.Multiply(z1.Item1, z2.Item2);
            zi = Avx2.Add(zi, tmp);
            return (zr, zi);
        }
        public static void Multiply(ref Vector256<double> z1R, ref Vector256<double> z1I,
                                    ref Vector256<double> z2R, ref Vector256<double> z2I,
                                    out Vector256<double> zR, out Vector256<double> zI)
        {
            var a = Avx2.Multiply(z1R, z2R);
            var b = Avx2.Multiply(z1I, z2I);
            var c = Avx2.Subtract(a, b);
            a = Avx2.Multiply(z1I, z2R);
            b = Avx2.Multiply(z1R, z2I);
            zR = c;
            zI = Avx2.Add(a, b);
        }

        public static (Vector256<double>, Vector256<double>) Divide((Vector256<double>, Vector256<double>) z1,
                                                                             (Vector256<double>, Vector256<double>) z2)
        {
            var denominator = Avx2.Multiply(z2.Item1, z2.Item1);
            var tmp = Avx2.Multiply(z2.Item2, z2.Item2);
            denominator = Avx2.Add(denominator, tmp);
            var zr = Avx2.Multiply(z1.Item1, z2.Item1);
            tmp = Avx2.Multiply(z1.Item2, z2.Item2);
            zr = Avx2.Add(zr, tmp);
            zr = Avx2.Divide(zr, denominator);
            var zi = Avx2.Multiply(z1.Item2, z2.Item1);
            tmp = Avx2.Multiply(z1.Item1, z2.Item2);
            zi = Avx2.Subtract(zi, tmp);
            zi = Avx2.Divide(zi, denominator);
            return (zr, zi);
        }
        public static (Vector256<double>, Vector256<double>) Pow((Vector256<double>, Vector256<double>) z, int pow)
        {
            if (pow == 0) return (Vector256.Create(1d), Vector256<double>.Zero);
            var mask1 = Avx2.CompareEqual(z.Item1, Vector256<double>.Zero);
            var mask2 = Avx2.CompareEqual(z.Item2, Vector256<double>.Zero);
            mask1 = Avx2.And(mask1, mask2);
            if (Avx2.MoveMask(mask1) > 0) return (Vector256<double>.Zero, Vector256<double>.Zero);
            var t = z;
            for (int i = 1; i < pow; i++)
            {
                z = Multiply(z, t);
            }
            return z;
        }


        #endregion
    }
}
