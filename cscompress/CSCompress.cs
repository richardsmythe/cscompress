using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Models;
using System.Numerics;

namespace FloatingPointCompressor
{
    /// <summary>
    /// Provides compression and decompression for floating-point arrays using a quantization strategy.
    /// </summary>
    /// <typeparam name="T">The floating-point type (float, double, etc.).</typeparam>
    public class CSCompress<T> where T : IFloatingPointIeee754<T>
    {
        private readonly IQuantizationStrategy<T> _strategy;
        private readonly T[] _values;
        private readonly Precision _precision;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSCompress{T}"/> class.
        /// </summary>
        /// <param name="values">The array of values to compress.</param>
        /// <param name="precision">The quantization precision.</param>
        /// <param name="strategy">The quantization strategy to use (optional).</param>
        public CSCompress(T[] values, Precision precision, IQuantizationStrategy<T>? strategy = null)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            _values = values;
            _precision = precision;
            _strategy = strategy ?? new IntegerQuantization<T>();
        }

        /// <summary>
        /// Compresses the input values using the selected quantization strategy.
        /// </summary>
        /// <returns>The compressed data as a byte array.</returns>
        public byte[] Compress()
        {
            return _strategy.Compress(_values, _precision);
        }

        /// <summary>
        /// Decompresses the provided byte array back into the original values.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <returns>The decompressed array of values.</returns>
        public T[] Decompress(byte[] compressedData)
        {
            return _strategy.Decompress(compressedData, _values.Length, _precision);
        }
    }
}
