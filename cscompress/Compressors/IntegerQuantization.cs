using FloatingPointCompressor.Models;
using System.Numerics;

namespace FloatingPointCompressor.Compressors
{
    /// <summary>
    /// Defines a quantization strategy for compressing and decompressing floating-point values.
    /// </summary>
    /// <typeparam name="T">The floating-point type (float, double, etc.).</typeparam>
    public interface IQuantizationStrategy<T> where T : IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Compresses an array of floating-point values using the specified precision.
        /// </summary>
        /// <param name="values">The values to compress.</param>
        /// <param name="precision">The quantization precision.</param>
        /// <returns>The compressed data as a byte array.</returns>
        byte[] Compress(T[] values, Precision precision);

        /// <summary>
        /// Decompresses a byte array back into floating-point values using the specified precision.
        /// </summary>
        /// <param name="compressedData">The compressed data to decompress.</param>
        /// <param name="valueCount">The number of values to decompress.</param>
        /// <param name="precision">The quantization precision.</param>
        /// <returns>The decompressed array of values.</returns>
        T[] Decompress(byte[] compressedData, int valueCount, Precision precision);
    }

    public class IntegerQuantization<T> : IQuantizationStrategy<T> where T : IFloatingPointIeee754<T>
    {
        public byte[] Compress(T[] values, Precision precision)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0) return Array.Empty<byte>();
            double scaleDouble = 1.0 / precision.Value;
            if (scaleDouble > long.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(precision), $"Precision value too small, scale overflows long: {scaleDouble}");
            long scale = (long)Math.Round(scaleDouble);
            int bitsPerValue = CalculateBitsPerValue(values, scale);
            int totalBits = values.Length * bitsPerValue;
            int totalBytes = (totalBits + 7) / 8;
            var compressedData = new byte[1 + totalBytes]; // 1 extra byte for bitsPerValue
            compressedData[0] = (byte)bitsPerValue;
            int bitPosition = 0;
            var scaleT = T.CreateChecked(scale);
            var scaleVector = Vector<T>.One * scaleT;
            for (int i = 0; i + Vector<T>.Count <= values.Length; i += Vector<T>.Count)
            {
                var vector = new Vector<T>(values, i);
                var scaledVector = vector * scaleVector;
                var scaledArray = new T[Vector<T>.Count];
                scaledVector.CopyTo(scaledArray);
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    double scaled = double.CreateChecked(T.Round(scaledArray[j]));
                    double clamped = Math.Max(Math.Min(scaled, long.MaxValue), long.MinValue);
                    long scaledValue = (long)clamped;
                    PackBits(scaledValue, ref bitPosition, compressedData, bitsPerValue, 1);
                }
            }
            for (int i = values.Length - values.Length % Vector<T>.Count; i < values.Length; i++)
            {
                double scaled = double.CreateChecked(T.Round(values[i] * scaleT));
                double clamped = Math.Max(Math.Min(scaled, long.MaxValue), long.MinValue);
                long scaledValue = (long)clamped;
                PackBits(scaledValue, ref bitPosition, compressedData, bitsPerValue, 1);
            }
            return compressedData;
        }

        public T[] Decompress(byte[] compressedData, int valueCount, Precision precision)
        {
            if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
            if (compressedData.Length == 0) return Array.Empty<T>();
            double scaleDouble = 1.0 / precision.Value;
            long scale = (long)Math.Round(scaleDouble);
            int bitsPerValue = compressedData[0];
            var decompressedValues = new T[valueCount];
            int bitPosition = 0;
            var scaleT = T.CreateChecked(scale);
            for (int i = 0; i + Vector<T>.Count <= valueCount; i += Vector<T>.Count)
            {
                var tmpArr = new T[Vector<T>.Count];
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    long scaledValue = UnpackBits(ref bitPosition, compressedData, bitsPerValue, 1);
                    tmpArr[j] = T.CreateChecked(scaledValue) / scaleT;
                }
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    decompressedValues[i + j] = tmpArr[j];
                }
            }
            for (int i = valueCount - valueCount % Vector<T>.Count; i < valueCount; i++)
            {
                long scaledValue = UnpackBits(ref bitPosition, compressedData, bitsPerValue, 1);
                decompressedValues[i] = T.CreateChecked(scaledValue) / scaleT;
            }
            return decompressedValues;
        }

        private int CalculateBitsPerValue(T[] values, long scale)
        {
            T maxAbsValue = values.Length == 0 ? T.Zero : values.Max(v => T.Abs(v));
            double scaled = double.CreateChecked(maxAbsValue) * scale;
            double clamped = Math.Min(Math.Abs(scaled), long.MaxValue);
            long maxScaledValue = (long)Math.Ceiling(clamped);
            return (int)Math.Ceiling(Math.Log2(maxScaledValue + 1)) + 1;
        }

        private int CalculateBitsPerValueLength(int compressedLength, int valueCount)
        {
            int totalBits = compressedLength * 8;
            return (int)Math.Ceiling((double)totalBits / valueCount);
        }

        private void PackBits(long value, ref int bitPosition, byte[] buffer, int bitsPerValue, int offset)
        {
            bool isNegative = value < 0;
            long absoluteValue = isNegative ? ~value : value;
            for (int i = 0; i < bitsPerValue - 1; i++)
            {
                WriteBit(buffer, bitPosition++, (absoluteValue & 1L << i) != 0, offset);
            }
            WriteBit(buffer, bitPosition++, isNegative, offset);
        }

        private long UnpackBits(ref int bitPosition, byte[] buffer, int bitsPerValue, int offset)
        {
            long value = 0;
            for (int i = 0; i < bitsPerValue - 1; i++)
            {
                if (ReadBit(buffer, bitPosition++, offset))
                {
                    value |= 1L << i;
                }
            }
            if (ReadBit(buffer, bitPosition++, offset))
            {
                value = ~value;
            }
            return value;
        }

        private void WriteBit(byte[] buffer, int bitIndex, bool bitValue, int offset)
        {
            int byteIndex = offset + bitIndex / 8;
            int bitOffset = bitIndex % 8;
            if (bitValue)
            {
                buffer[byteIndex] |= (byte)(1 << bitOffset);
            }
        }

        private bool ReadBit(byte[] buffer, int bitIndex, int offset)
        {
            int byteIndex = offset + bitIndex / 8;
            int bitOffset = bitIndex % 8;
            return (buffer[byteIndex] & 1 << bitOffset) != 0;
        }
    }
}
