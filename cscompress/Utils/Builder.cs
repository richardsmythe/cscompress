using System;
using System.IO;
using System.Linq;
using System.Globalization;
using FloatingPointCompressor;
using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Models;

namespace FloatingPointCompressor.Utils
{
    public static class Builder
    {
        /// <summary>
        /// Compresses a float array using the specified precision and optional quantization strategy.
        /// </summary>
        public static byte[] CompressWithPrecision(this float[] values, Precision precision, IQuantizationStrategy<float>? strategy = null)
        {
            var compressor = new CSCompress<float>(values, precision, strategy ?? new IntegerQuantization<float>());
            return compressor.Compress();
        }

        /// <summary>
        /// Compresses a double array using the specified precision and optional quantization strategy.
        /// </summary>
        public static byte[] CompressWithPrecision(this double[] values, Precision precision, IQuantizationStrategy<double>? strategy = null)
        {
            var compressor = new CSCompress<double>(values, precision, strategy ?? new IntegerQuantization<double>());
            return compressor.Compress();
        }

        /// <summary>
        /// Decompresses a byte array to a float array using the original length and precision.
        /// </summary>
        public static float[] DecompressFloatWithPrecision(this byte[] compressed, int originalLength, Precision precision, IQuantizationStrategy<float>? strategy = null)
        {
            var compressor = new CSCompress<float>(new float[originalLength], precision, strategy ?? new IntegerQuantization<float>());
            return compressor.Decompress(compressed);
        }

        /// <summary>
        /// Decompresses a byte array to a double array using the original length and precision.
        /// </summary>
        public static double[] DecompressDoubleWithPrecision(this byte[] compressed, int originalLength, Precision precision, IQuantizationStrategy<double>? strategy = null)
        {
            var compressor = new CSCompress<double>(new double[originalLength], precision, strategy ?? new IntegerQuantization<double>());
            return compressor.Decompress(compressed);
        }

        /// <summary>
        /// Reads a float array from a CSV or text file (comma or newline separated).
        /// </summary>
        public static float[] ReadFloatArrayFromFile(string path)
        {
            var text = File.ReadAllText(path);
            var values = text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            return values;
        }

        /// <summary>
        /// Reads a double array from a CSV or text file (comma or newline separated).
        /// </summary>
        public static double[] ReadDoubleArrayFromFile(string path)
        {
            var text = File.ReadAllText(path);
            var values = text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            return values;
        }

        /// <summary>
        /// Extension method to write a byte array to a file as a base64 string.
        /// </summary>
        public static void SaveToFile(this byte[] data, string path)
        {
            File.WriteAllText(path, Convert.ToBase64String(data));
        }
    }
}
