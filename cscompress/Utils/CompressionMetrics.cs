using System;
using System.Numerics;

namespace FloatingPointCompressor.Utils
{
    public static class CompressionMetrics
    {
        public static double MeanError<T>(T[] original, T[] decompressed) where T : INumber<T>
        {
            double sum = 0;
            for (int i = 0; i < original.Length; i++)
                sum += Convert.ToDouble(decompressed[i]) - Convert.ToDouble(original[i]);
            return sum / original.Length;
        }

        public static double CompressionRatio(int originalSize, int compressedSize)
            => compressedSize == 0 ? 0 : (double)originalSize / compressedSize;
    }
}
