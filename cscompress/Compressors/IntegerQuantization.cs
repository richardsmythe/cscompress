using FloatingPointCompressor.Models;
using System.Numerics;
using System.Linq;
using System.Buffers.Binary;

namespace FloatingPointCompressor.Compressors
{
    /// <summary>
    /// Defines a quantization strategy for compressing and decompressing floating-point values.
    /// Implementations quantize values at a chosen precision, pack them to bits, and reverse the process.
    /// </summary>
    /// <typeparam name="T">The IEEE-754 floating-point type (e.g., float, double).</typeparam>
    public interface IQuantizationStrategy<T> where T : IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Compresses an array of values using the provided precision into a compact byte[] payload.
        /// </summary>
        byte[] Compress(T[] values, Precision precision);

        /// <summary>
        /// Decompresses a self-describing payload by reading valueCount and scale from the header.
        /// </summary>
        T[] Decompress(byte[] compressedData);

        /// <summary>
        /// Decompresses a previously produced payload back into values using the provided precision. Contains valueCount for callers who want to validate correctness.
        /// </summary>
        T[] Decompress(byte[] compressedData, int valueCount, Precision precision);

        /// <summary>
        /// Decompresses a self-describing payload, reading valueCount from the header.
        /// </summary>
        T[] Decompress(byte[] compressedData, Precision precision);
    }

    /// <summary>
    /// Binary header that prefixes every compressed payload.
    /// Layout (little-endian for multibyte fields):
    /// Version(1) | Flags(1) | BitsPerValue(1) | ValueCount(4) | Scale(8)
    /// - Version: payload format version for forward/backward compatibility.
    /// - Flags: reserved field for toggling features eg: rounding mode.
    /// - BitsPerValue: fixed bitwidth for every quantized value in the payload.
    /// - ValueCount: number of values encoded in the payload. Determines how large output array should be.
    /// - Scale: integer scaling factor = round(1.0 / Precision.Value).
    /// </summary>
    public readonly record struct PayloadHeader(byte Version, byte Flags, byte BitsPerValue, int ValueCount, long Scale)
    {
        /// <summary>Total header byte size.</summary>
        public const int Size = 1 + 1 + 1 + 4 + 8; // a 15byte overhead. each size maps to .net primitive types

        /// <summary>Factory for typical header creation.</summary>
        public static PayloadHeader Create(byte bitsPerValue, int valueCount, long scale, byte version = 1, byte flags = 0)
            => new PayloadHeader(version, flags, bitsPerValue, valueCount, scale);

        /// <summary>
        /// Writes the header into a span. Returns false if the span is too small.
        /// Ensures lowest byte first.
        /// </summary>
        public bool TryWrite(Span<byte> dest)
        {
            if (dest.Length < Size) return false;
            int i = 0;
            dest[i++] = Version;
            dest[i++] = Flags;
            dest[i++] = BitsPerValue;
            BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(i, 4), ValueCount); // 
            i += 4;
            BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(i, 8), Scale);
            return true;
        }

        /// <summary>
        /// Reads a header from the source span. Returns false if the span is too small.
        /// </summary>
        public static bool TryRead(ReadOnlySpan<byte> src, out PayloadHeader header)
        {
            header = default;
            if (src.Length < Size) return false;
            int i = 0;
            byte version = src[i++];
            byte flags = src[i++];
            byte bitsPerValue = src[i++];
            int valueCount = BinaryPrimitives.ReadInt32LittleEndian(src.Slice(i, 4));
            i += 4;
            long scale = BinaryPrimitives.ReadInt64LittleEndian(src.Slice(i, 8));
            header = new PayloadHeader(version, flags, bitsPerValue, valueCount, scale);
            return true;
        }
    }

    /// <summary>
    /// Quantizes values to integers using a precision-derived scale, then packs them into a bitstream.
    /// The payload starts with a compact self-describing header followed by the bit-packed values.
    /// </summary>
    public class IntegerQuantization<T> : IQuantizationStrategy<T> where T : IFloatingPointIeee754<T>
    {
        private const byte CurrentVersion = 1; 
        private const byte DefaultFlags = 0;

        public byte[] Compress(T[] values, Precision precision)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (values.Length == 0) return Array.Empty<byte>();
            if (precision.Value <= 0) throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be positive.");

            // Compute integer scaling factor: scale = round(1.0 / precision)
            double scaleDouble = 1.0 / precision.Value;
            if (scaleDouble > long.MaxValue) throw new ArgumentOutOfRangeException(nameof(precision), $"Precision value too small, scale overflows long: {scaleDouble}");
            long scale = (long)Math.Round(scaleDouble);

            // Determine how many bits we need per value after scaling (magnitude + sign bit)
            int bitsPerValue = CalculateBitsPerValue(values, scale);
            if (bitsPerValue <= 0 || bitsPerValue > 64) throw new ArgumentOutOfRangeException(nameof(bitsPerValue), $"bitsPerValue out of range: {bitsPerValue}");

            // Allocate buffer: header + bitpacked payload
            int totalBits = values.Length * bitsPerValue;
            int totalBytes = (totalBits + 7) / 8; // 7 ensures round up to next byte. 
            var compressedData = new byte[PayloadHeader.Size + totalBytes];

            // Serialize header at the start of the buffer
            var header = PayloadHeader.Create((byte)bitsPerValue, values.Length, scale, CurrentVersion, DefaultFlags);
            if (!header.TryWrite(compressedData.AsSpan(0, PayloadHeader.Size))) throw new InvalidOperationException("Failed to write payload header.");

            // Pack the values: scale,rounded integer, clamped to long, bitpacked
            int bitPosition = 0; // bit offset within the payload (after header)
            var scaleT = T.CreateChecked(scale);
            var scaleVector = Vector<T>.One * scaleT;

            // SIMD-chunk processing
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
                    PackBits(scaledValue, ref bitPosition, compressedData, bitsPerValue, PayloadHeader.Size);
                }
            }

            // Deal with remainder elements that dont fit into the complete SIMD vector separately
            for (int i = values.Length - values.Length % Vector<T>.Count; i < values.Length; i++)
            {
                double scaled = double.CreateChecked(T.Round(values[i] * scaleT));
                double clamped = Math.Max(Math.Min(scaled, long.MaxValue), long.MinValue);
                long scaledValue = (long)clamped;
                PackBits(scaledValue, ref bitPosition, compressedData, bitsPerValue, PayloadHeader.Size);
            }

            return compressedData;
        }

        /// <summary>
        /// Decompresses a self-describing payload by reading valueCount from the header.
        /// </summary>
        public T[] Decompress(byte[] compressedData, Precision precision)
        {
            if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
            if (compressedData.Length == 0) return Array.Empty<T>();
            if (!PayloadHeader.TryRead(compressedData.AsSpan(0, PayloadHeader.Size), out var header)) throw new ArgumentException("Compressed payload too small to contain header.", nameof(compressedData));

            return Decompress(compressedData, header.ValueCount, precision);
        }

        /// <summary>
        /// Decompresses a self-describing payload by reading valueCount and scale from the header only.
        /// No external Precision parameter needed.
        /// </summary>
        public T[] Decompress(byte[] compressedData)
        {
            if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
            if (compressedData.Length == 0) return Array.Empty<T>();
            if (!PayloadHeader.TryRead(compressedData.AsSpan(0, PayloadHeader.Size), out var header)) throw new ArgumentException("Compressed payload too small to contain header.", nameof(compressedData));
            if (header.Version != CurrentVersion)throw new NotSupportedException($"Unsupported payload version {header.Version}.");
            if (header.BitsPerValue <= 0 || header.BitsPerValue > 64)throw new ArgumentOutOfRangeException(nameof(header.BitsPerValue), $"bitsPerValue out of range: {header.BitsPerValue}");
            
            // check payload is declared length
            int totalBits = header.ValueCount * header.BitsPerValue;
            int totalBytes = (totalBits + 7) / 8;
            int expectedLength = PayloadHeader.Size + totalBytes;
            
            if (compressedData.Length < expectedLength)throw new ArgumentException($"Compressed payload truncated. Expected at least {expectedLength} bytes, got {compressedData.Length}.");

            // unpack values so read bits, signed integer, then divide by scale
            var decompressedValues = new T[header.ValueCount];
            int bitPosition = 0;
            var scaleT = T.CreateChecked(header.Scale);

            for (int i = 0; i + Vector<T>.Count <= header.ValueCount; i += Vector<T>.Count)
            {
                var tmpArr = new T[Vector<T>.Count];
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    long scaledValue = UnpackBits(ref bitPosition, compressedData, header.BitsPerValue, PayloadHeader.Size);
                    tmpArr[j] = T.CreateChecked(scaledValue) / scaleT;
                }
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    decompressedValues[i + j] = tmpArr[j];
                }
            }

            // deal with remainders elements separately
            for (int i = header.ValueCount - header.ValueCount % Vector<T>.Count; i < header.ValueCount; i++)
            {
                long scaledValue = UnpackBits(ref bitPosition, compressedData, header.BitsPerValue, PayloadHeader.Size);
                decompressedValues[i] = T.CreateChecked(scaledValue) / scaleT;
            }

            return decompressedValues;
        }

        public T[] Decompress(byte[] compressedData, int valueCount, Precision precision)
        {
            if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));
            if (compressedData.Length == 0) return Array.Empty<T>();
            if (precision.Value <= 0) throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be positive.");
            if (!PayloadHeader.TryRead(compressedData.AsSpan(0, PayloadHeader.Size), out var header)) throw new ArgumentException("Compressed payload too small to contain header.", nameof(compressedData));
            if (header.Version != CurrentVersion)throw new NotSupportedException($"Unsupported payload version {header.Version}.");
            if (header.BitsPerValue <= 0 || header.BitsPerValue > 64)throw new ArgumentOutOfRangeException(nameof(header.BitsPerValue), $"bitsPerValue out of range: {header.BitsPerValue}");
            if (header.ValueCount != valueCount)throw new ArgumentException($"ValueCount mismatch: header={header.ValueCount}, argument={valueCount}.");

            // check payload is declared length
            int totalBits = header.ValueCount * header.BitsPerValue;
            int totalBytes = (totalBits + 7) / 8;
            int expectedLength = PayloadHeader.Size + totalBytes;
            
            if (compressedData.Length < expectedLength)throw new ArgumentException($"Compressed payload truncated. Expected at least {expectedLength} bytes, got {compressedData.Length}.");

            // unpack values so read bits, signed integer, then divide by scale
            var decompressedValues = new T[valueCount];
            int bitPosition = 0;
            var scaleT = T.CreateChecked(header.Scale);

            for (int i = 0; i + Vector<T>.Count <= valueCount; i += Vector<T>.Count)
            {
                var tmpArr = new T[Vector<T>.Count];
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    long scaledValue = UnpackBits(ref bitPosition, compressedData, header.BitsPerValue, PayloadHeader.Size);
                    tmpArr[j] = T.CreateChecked(scaledValue) / scaleT;
                }
                for (int j = 0; j < Vector<T>.Count; j++)
                {
                    decompressedValues[i + j] = tmpArr[j];
                }
            }

            // deal with remainders elements separately
            for (int i = valueCount - valueCount % Vector<T>.Count; i < valueCount; i++)
            {
                long scaledValue = UnpackBits(ref bitPosition, compressedData, header.BitsPerValue, PayloadHeader.Size);
                decompressedValues[i] = T.CreateChecked(scaledValue) / scaleT;
            }

            return decompressedValues;
        }

        /// <summary>
        /// Computes how many bits are needed to represent the scaled magnitude of the largest value,
        /// plus one bit for the sign. Ensures a uniform width for all packed values.
        /// </summary>
        private int CalculateBitsPerValue(T[] values, long scale)
        {
            T maxAbsValue = values.Length == 0 ? T.Zero : values.Max(v => T.Abs(v));
            double scaled = double.CreateChecked(maxAbsValue) * scale;
            double clamped = Math.Min(Math.Abs(scaled), long.MaxValue);
            long maxScaledValue = (long)Math.Ceiling(clamped);
            int magnitudeBits = (maxScaledValue <= 0) ? 1 : (int)Math.Ceiling(Math.Log2(maxScaledValue + 1));
            return magnitudeBits + 1; // +1 for sign bit
        }

        /// <summary>
        /// Packs a signed integer into the buffer using <paramref name="bitsPerValue"/> bits at the current bit position.
        /// Encoding: write (bitsPerValue-1) magnitude bits LSB-first, then a sign bit (true for negative).
        /// </summary>
        private void PackBits(long value, ref int bitPosition, byte[] buffer, int bitsPerValue, int offset)
        {
            bool isNegative = value < 0;
            // convert magnitude if in the negative case (sign-magnitude-like scheme)
            ulong magnitude = isNegative ? (ulong)(-(value + 1L)) + 1UL : (ulong)value;
            for (int i = 0; i < bitsPerValue - 1; i++)
            {
                bool bit = ((magnitude >> i) & 1UL) != 0UL;
                WriteBit(buffer, bitPosition++, bit, offset);
            }
            WriteBit(buffer, bitPosition++, isNegative, offset);
        }

        /// <summary>
        /// Unpacks a signed integer from the buffer using <paramref name="bitsPerValue"/> bits at the current bit position.
        /// Decoding mirrors <see cref="PackBits"/>: read LSB first magnitude, then sign.
        /// </summary>
        private long UnpackBits(ref int bitPosition, byte[] buffer, int bitsPerValue, int offset)
        {
            ulong magnitude = 0UL;
            for (int i = 0; i < bitsPerValue - 1; i++)
            {
                if (ReadBit(buffer, bitPosition++, offset))
                {
                    magnitude |= 1UL << i;
                }
            }
            bool isNegative = ReadBit(buffer, bitPosition++, offset);
            if (!isNegative)
            {
                return (long)magnitude;
            }
            if (magnitude <= (ulong)long.MaxValue)
            {
                return -(long)magnitude;
            }
            else
            {
                return long.MinValue;
            }
        }

        /// <summary>
        /// Sets a single bit at the given absolute bit index (relative to <paramref name="offset"/> bytes into <paramref name="buffer"/>).
        /// Only sets bits when true; leaving false preserves zero.
        /// </summary>
        private void WriteBit(byte[] buffer, int bitIndex, bool bitValue, int offset)
        {
            int byteIndex = offset + (bitIndex / 8);
            int bitOffset = bitIndex % 8;
            if (bitValue)
            {
                buffer[byteIndex] |= (byte)(1 << bitOffset);
            }
        }

        /// <summary>
        /// Reads a single bit at the given absolute bit index (relative to <paramref name="offset"/> bytes into <paramref name="buffer"/>).
        /// </summary>
        private bool ReadBit(byte[] buffer, int bitIndex, int offset)
        {
            int byteIndex = offset + (bitIndex / 8);
            int bitOffset = bitIndex % 8;
            return (buffer[byteIndex] & (1 << bitOffset)) != 0;
        }
    }
}
