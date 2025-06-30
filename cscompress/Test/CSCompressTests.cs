using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Models;
using Xunit;
namespace FloatingPointCompressor.Test
{



    public class CSCompressTests
    {


        public static readonly TheoryData<float[], Precision> samples = new TheoryData<float[], Precision>
        {
            { new float[] { 3.14159265f }, Precision.Millionths },
            { new float[] {0f, 0.0f, 0.00f, 0.000f, 0.0000f,0.000000f}, Precision.Hundredths },
            { new float[] { -0.0001f, -6.100f, -9.9999f}, Precision.Thousandsth },
            { new float[] { 0.2345f, 0.1000f, 1.24145f}, Precision.Tenths },
            { new float[] { 4.45473145f, 2.45444125f, 8.45478115f, 5.35473615f, 7.25247615f, 1.41547365f }, Precision.Tenths },
            { new float[] { 2.4345f, 3.5331f, 8.2222f, 4.1234f }, Precision.Hundredths },
            { new float[] { 5.1431f, 2.2222f, 9.4444f, 5.1111f, 7.3244f, 5.5555f }, Precision.Thousandsth },
            { new float[] { 6.454731f, 4.454441f, 6.454781f, 5.354761f, 7.254761f, 2.454736f }, Precision.TenThousandths },
            { new float[] { 1.123f, 2.222f, 9.444f, 5.111f, 6.134f, 6.555f, 5.111f, 9.211f }, Precision.Millionths },
            { new float[] { 0.123f, 1.234f, 2.345f, 3.456f, 4.567f, 5.678f, 6.789f, 7.890f, 8.901f, 9.012f,
                            10.123f, 11.2364f, 12.345f, 13.456f, 14.567f, 115.678f, 16.789f, 17.890f, -18.901f, 19.012f,
                            20.123f, 21.2634f, 22.3445f, 23.456f, 24.567f, 625.678f, 26.789f, 27.890f, 28.901f, 29.012f,
                            230.123f, 31.2364f, 32.345f, 33.4456f, 34.567f, -35.678f, 36.789f, 37.890f, 38.901f, 39.012f,
                            40.123f, 41.234f, 42.345f, 43.456f, -44.567f, 45.678f, 46.789f, 47.890f, 48.901f, 49.012f,
                            50.123f, 51.234f, 52.345f, 53.456f, -54.567f, 55.678f, 56.789f, 57.890f, 58.901f, 59.012f,
                            60.123f, 61.234f, 62.345f, 63.456f, 64.567f, 65.678f, 66.789f, 67.890f, 68.901f, 69.012f,
                            -70.123f, 71.234f, 72.345f, 73.456f, 74.567f, 75.678f, 76.789f, 77.890f, 784.9501f, 79.012f,
                            80.123f, 81.234f, 82.345f, 83.456f, 84.567f, 85.678f, 86.789f, 87.890f, 88.901f, 89.012f,
                            90.123f, 91.234f, 92.345f, 93.456f, 94.567f, 95.678f, 96.789f, 97.890f, 98.901f,-99.012f
                        }, Precision.Thousandsth },
            { new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f }, Precision.Thousandsth },
            { new float[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f }, Precision.Thousandsth },
            { new float[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f, 16f, 17f, 18f, 19f, 20f, 21f, 22f, 23f, 24f, 25f, 26f, 27f, 28f, 29f, 30f, 31f, 32f, 33f, 34f, 35f, 36f, 37f, 38f, 39f, 40f, 41f, 42f, 43f, 44f, 45f, 46f, 47f, 48f, 49f }, Precision.Thousandsth },
        };

        [Theory]
        [MemberData(nameof(samples))]
        public void Test_Precision_With_Different_Values(float[] values, Precision precision)
        {
            CSCompress<float> fc = new CSCompress<float>(values, precision, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            float[] decompressed = fc.Decompress(compressedData);
            for (int i = 0; i < values.Length; i++)
            {
                float expected = values[i];
                float actual = decompressed[i];
                float tolerance = (float)precision.GetTolerance();
                Assert.InRange(actual, expected - tolerance, expected + tolerance);
            }
        }

        [Fact]
        public void Test_Large_Input_Size()
        {
            float[] values = new float[1000000];
            Random rand = new Random();
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (float)(rand.NextDouble() * 100);
            }
            CSCompress<float> fc = new CSCompress<float>(values, Precision.Thousandsth, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);

            Assert.True(compressedData.Length < values.Length * sizeof(float));
            Assert.Equal(values.Length, decompressed.Length);
        }

        [Fact]
        public void Test_Compression_Reduction()
        {
            float[] values = { 1.23f, 4.56f, 7.89f, 10.12f, 15.34f };
            CSCompress<float> fc = new CSCompress<float>(values, Precision.Thousandsth, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            var originalSize = values.Length * sizeof(float);
            float compressionRatio = (float)originalSize / compressedData.Length;
            Assert.True(compressionRatio > 1, $"Compression ratio is too low: {compressionRatio}");
        }

        [Fact]
        public void Test_Compression_Ratio_Varying_Precisions()
        {
            float[] values = { 1.23f, 4.56f, 7.89f, 10.12f, 15.34f };
            // Only test compression for practical precisions (>= 1e-7)
            foreach (var precision in PrecisionExtensions.AllPrecisions)
            {
                if (precision.Value < 1e-7) continue;
                CSCompress<float> fc = new CSCompress<float>(values, precision, new IntegerQuantization<float>());
                var compressedData = fc.Compress();
                var originalSize = values.Length * sizeof(float);
                var compressedSize = compressedData.Length;
                float compressionRatio = (float)originalSize / compressedSize;
                Assert.True(compressionRatio >= 1, $"Compression ratio for {precision} precision is too low: {compressionRatio}");
            }
        }

        [Fact]
        public void Test_Error_Distributions_For_Different_Precisions()
        {
            float[] values = { 1.123456f, 2.654321f, 3.987654f, 4.000001f };
            CSCompress<float> fcHigh = new CSCompress<float>(values, Precision.Millionths, new IntegerQuantization<float>());
            var compressedHigh = fcHigh.Compress();
            float[] decompressedHigh = fcHigh.Decompress(compressedHigh);
            CSCompress<float> fcLow = new CSCompress<float>(values, Precision.Tenths, new IntegerQuantization<float>());
            var compressedLow = fcLow.Compress();
            float[] decompressedLow = fcLow.Decompress(compressedLow);
            for (int i = 0; i < values.Length; i++)
            {
                float original = values[i];
                float highPrecisionError = Math.Abs(original - decompressedHigh[i]);
                float lowPrecisionError = Math.Abs(original - decompressedLow[i]);

                Assert.True(highPrecisionError < lowPrecisionError,
                    $"Error with higher precision should be less than error with lower precision for index {i}. " +
                    $"High precision error: {highPrecisionError}, Low precision error: {lowPrecisionError}");
            }
            Assert.True(compressedLow.Length < compressedHigh.Length,
                "Compressed data with lower precision should be smaller in size compared to higher precision.");
        }

        [Fact]
        public void Test_Varying_Precision_Compresses()
        {
            var values = new float[] { 123.456f, 1.9f };
            CSCompress<float> fc10s = new CSCompress<float>(values, Precision.Tenths, new IntegerQuantization<float>());
            CSCompress<float> fc100s = new CSCompress<float>(values, Precision.Hundredths, new IntegerQuantization<float>());
            var compressedData10s = fc10s.Compress();
            var compressedData100s = fc100s.Compress();
            Assert.NotEmpty(compressedData10s);
            Assert.NotEmpty(compressedData100s);
            Assert.NotEqual(compressedData10s.Length, compressedData100s.Length);
        }

        [Fact]
        public void Test_Edge_Case_Precision()
        {
            float[] values = { 1.00000049f, 1.00000051f };
            CSCompress<float> fc = new CSCompress<float>(values, Precision.Millionths, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);
            Assert.Equal(1.0000005f, decompressed[0], precision: 6);
            Assert.Equal(1.0000005f, decompressed[1], precision: 6);
        }

        [Fact]
        public void Test_Empty_Compress_Decompress()
        {
            float[] values = { };
            CSCompress<float> fc = new CSCompress<float>(values, Precision.Millionths, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);
            Assert.Empty(compressedData);
            Assert.Empty(decompressed);
        }
    }

    public class DoubleCompressorTests
    {
        public static readonly TheoryData<double[], Precision> doubleSamples = new TheoryData<double[], Precision>
        {
            { new double[] { 3.141592653589793 }, Precision.Millionths },
            { new double[] {0d, 0.0d, 0.00d, 0.000d, 0.0000d,0.000000d}, Precision.Hundredths },
            { new double[] { -0.0001d, -6.100d, -9.9999d}, Precision.Thousandsth },
            { new double[] { 0.2345d, 0.1000d, 1.24145d}, Precision.Tenths },
            { new double[] { 4.45473145d, 2.45444125d, 8.45478115d, 5.35473615d, 7.25247615d, 1.41547365d }, Precision.Tenths },
            { new double[] { 2.4345d, 3.5331d, 8.2222d, 4.1234d }, Precision.Hundredths },
            { new double[] { 5.1431d, 2.2222d, 9.4444d, 5.1111d, 7.3244d, 5.5555d }, Precision.Thousandsth },
            { new double[] { 6.454731d, 4.454441d, 6.454781d, 5.354761d, 7.254761d, 2.454736d }, Precision.TenThousandths },
            { new double[] { 1.123d, 2.222d, 9.444d, 5.111d, 6.134d, 6.555d, 5.111d, 9.211d }, Precision.Millionths },
            { new double[] { 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d, 1d }, Precision.Thousandsth },
            { new double[] { 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d }, Precision.Thousandsth },
            { new double[] { 0d, 1d, 2d, 3d, 4d, 5d, 6d, 7d, 8d, 9d, 10d, 11d, 12d, 13d, 14d, 15d, 16d, 17d, 18d, 19d, 20d, 21d, 22d, 23d, 24d, 25d, 26d, 27d, 28d, 29d, 30d, 31d, 32d, 33d, 34d, 35d, 36d, 37d, 38d, 39d, 40d, 41d, 42d, 43d, 44d, 45d, 46d, 47d, 48d, 49d }, Precision.Thousandsth },
        };

        [Theory]
        [MemberData(nameof(doubleSamples))]
        public void Test_Precision_With_Different_Values_Double(double[] values, Precision precision)
        {
            var fc = new CSCompress<double>(values, precision, new IntegerQuantization<double>());
            var compressedData = fc.Compress();
            double[] decompressed = fc.Decompress(compressedData);
            for (int i = 0; i < values.Length; i++)
            {
                double expected = values[i];
                double actual = decompressed[i];
                double tolerance = precision.GetTolerance();
                Assert.InRange(actual, expected - tolerance, expected + tolerance);
            }
        }

        [Fact]
        public void Test_Large_Input_Size_Double()
        {
            double[] values = new double[1000000];
            Random rand = new Random();
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = rand.NextDouble() * 100;
            }
            var fc = new CSCompress<double>(values, Precision.Thousandsth, new IntegerQuantization<double>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);
            Assert.True(compressedData.Length < values.Length * sizeof(double));
            Assert.Equal(values.Length, decompressed.Length);
        }

        [Fact]
        public void Test_Compression_Reduction_Double()
        {
            double[] values = { 1.23d, 4.56d, 7.89d, 10.12d, 15.34d };
            var fc = new CSCompress<double>(values, Precision.Thousandsth, new IntegerQuantization<double>());
            var compressedData = fc.Compress();
            var originalSize = values.Length * sizeof(double);
            double compressionRatio = (double)originalSize / compressedData.Length;
            Assert.True(compressionRatio > 1, $"Compression ratio is too low: {compressionRatio}");
        }

        [Fact]
        public void Test_Compression_Ratio_Varying_Precisions_Double()
        {
            double[] values = { 1.23d, 4.56d, 7.89d, 10.12d, 15.34d };
            foreach (var precision in PrecisionExtensions.AllPrecisions)
            {
                var fc = new CSCompress<double>(values, precision, new IntegerQuantization<double>());
                var compressedData = fc.Compress();
                var originalSize = values.Length * sizeof(double);
                var compressedSize = compressedData.Length;
                double compressionRatio = (double)originalSize / compressedSize;
                Assert.True(compressionRatio > 1, $"Compression ratio for {precision} precision is too low: {compressionRatio}");
            }
        }

        [Fact]
        public void Test_Error_Distributions_For_Different_Precisions_Double()
        {
            double[] values = { 1.123456789012, 2.654321987654, 3.987654321098, 4.000001234567 };
            var fcHigh = new CSCompress<double>(values, Precision.Millionths, new IntegerQuantization<double>());
            var compressedHigh = fcHigh.Compress();
            double[] decompressedHigh = fcHigh.Decompress(compressedHigh);
            var fcLow = new CSCompress<double>(values, Precision.Tenths, new IntegerQuantization<double>());
            var compressedLow = fcLow.Compress();
            double[] decompressedLow = fcLow.Decompress(compressedLow);
            for (int i = 0; i < values.Length; i++)
            {
                double original = values[i];
                double highPrecisionError = Math.Abs(original - decompressedHigh[i]);
                double lowPrecisionError = Math.Abs(original - decompressedLow[i]);
                Assert.True(highPrecisionError < lowPrecisionError,
                    $"Error with higher precision should be less than error with lower precision for index {i}. " +
                    $"High precision error: {highPrecisionError}, Low precision error: {lowPrecisionError}");
            }
            Assert.True(compressedLow.Length < compressedHigh.Length,
                "Compressed data with lower precision should be smaller in size compared to higher precision.");
        }

        [Fact]
        public void Test_Varying_Precision_Compresses_Double()
        {
            var values = new double[] { 123.456d, 1.9d };
            var fc10s = new CSCompress<double>(values, Precision.Tenths, new IntegerQuantization<double>());
            var fc100s = new CSCompress<double>(values, Precision.Hundredths, new IntegerQuantization<double>());
            var compressedData10s = fc10s.Compress();
            var compressedData100s = fc100s.Compress();
            Assert.NotEmpty(compressedData10s);
            Assert.NotEmpty(compressedData100s);
            Assert.NotEqual(compressedData10s.Length, compressedData100s.Length);
        }

        [Fact]
        public void Test_Edge_Case_Precision_Double()
        {
            double[] values = { 1.000000499999, 1.000000500001 };
            var fc = new CSCompress<double>(values, Precision.Millionths, new IntegerQuantization<double>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);
            Assert.InRange(decompressed[0], 1.0000005d - 1e-6, 1.0000005d + 1e-6);
            Assert.InRange(decompressed[1], 1.0000005d - 1e-6, 1.0000005d + 1e-6);
        }

        [Fact]
        public void Test_Empty_Compress_Decompress_Double()
        {
            double[] values = { };
            var fc = new CSCompress<double>(values, Precision.Millionths, new IntegerQuantization<double>());
            var compressedData = fc.Compress();
            var decompressed = fc.Decompress(compressedData);
            Assert.Empty(compressedData);
            Assert.Empty(decompressed);
        }

        [Fact]
        public void Test_Double_Has_More_Precision_Than_Float()
        {
            // value that can be represented by double but not by float
            double preciseDouble = 1.1234567898765432d;
            float impreciseFloat = (float)preciseDouble;
            var fcDouble = new CSCompress<double>(new[] { preciseDouble }, Precision.TenTrillionths, new IntegerQuantization<double>());
            var fcFloat = new CSCompress<float>(new[] { impreciseFloat }, Precision.TenTrillionths, new IntegerQuantization<float>());
            var compressedDouble = fcDouble.Compress();
            var compressedFloat = fcFloat.Compress();
            double[] decompressedDouble = fcDouble.Decompress(compressedDouble);
            float[] decompressedFloat = fcFloat.Decompress(compressedFloat);
            Assert.Equal(preciseDouble, decompressedDouble[0], 13);
            Assert.NotEqual(decompressedFloat[0], preciseDouble, 13);
        }

        [Fact]
        public void Test_Extreme_Values_Double()
        {
            double[] values = { double.Epsilon, -double.Epsilon, 0.0, -0.0 };
            var fc = new CSCompress<double>(values, Precision.TenTrillionths, new IntegerQuantization<double>());
            var compressed = fc.Compress();
            var decompressed = fc.Decompress(compressed);
            for (int i = 0; i < values.Length; i++)
            {
                Assert.InRange(decompressed[i], values[i] - Precision.TenTrillionths.Value, values[i] + Precision.TenTrillionths.Value);
            }
        }

        [Fact]
        public void Test_Order_Is_Preserved_Double()
        {
            double[] values = { -100.0, -1.0, 0.0, 0.0000001, 1.0, 1.0, 2.0, 10.0, 100.0 };
            var fc = new CSCompress<double>(values, Precision.TenMillionths, new IntegerQuantization<double>());
            var compressed = fc.Compress();
            var decompressed = fc.Decompress(compressed);
            for (int i = 1; i < values.Length; i++)
            {
                Assert.True(decompressed[i - 1] <= decompressed[i],
                    $"Order not preserved at index {i}: {decompressed[i - 1]} > {decompressed[i]}");
            }
        }

    }

    public static class PrecisionExtensions
    {
        public static double GetTolerance(this Precision precision)
        {
            return precision.Value;
        }

        public static readonly Precision[] AllPrecisions = new[]
{
            Precision.TenTrillionths,
            Precision.Trillionths,
            Precision.HundredMillionths,
            Precision.TenMillionths,
            Precision.Millionths,
            Precision.HundredThousandths,
            Precision.TenThousandths,
            Precision.Thousandsth,
            Precision.Hundredths,
            Precision.Tenths
        };

    }


}
