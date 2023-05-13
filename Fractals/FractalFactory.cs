using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgebraicFractals.Fractals
{
    public static class FractalFactory
    {
        public static AlgebraicFractal CreateFractal(ExistingFractals existingFractals, params object?[]? arguments)
        {
            var str = "AlgebraicFractals.Fractals." + existingFractals.ToString();
            Type type = Type.GetType(str);
            if (type != null) return (AlgebraicFractal)Activator.CreateInstance(type, arguments);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(str);
                if (type != null) return (AlgebraicFractal)Activator.CreateInstance(type, arguments);
            }
            throw new TypeUnloadedException($"Cannot find type {str}");

        }
    }
}
