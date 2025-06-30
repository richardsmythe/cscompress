using BenchmarkDotNet.Running;
using FloatingPointCompressor;
using System.Globalization;
using FloatingPointCompressor.Test;
using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Utils;
using System.IO;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Uncomment to run benchmark.
        // var summary = BenchmarkRunner.Run<FloatCompressorBenchmark>();

        float[] scientificFloatValues = new float[] {
            5.54500008f,
            -7.55112505f,
            123456.789f,
            -98765.4297f,
            3.1415925f,
            -2.71828175f,
            1.61803389f,
            -0.577215672f,
            299792.469f
        };

        double[] scientificDoubleValues = new double[] {
            5.54500008,
            -7.55112505,
            123456.789,
            -98765.4297,
            3.1415925,
            -2.71828175,
            1.61803389,
            -0.577215672,
            299792.469
        };

        var precisions = PrecisionExtensions.AllPrecisions;

        var outputPath = "compressed_results.txt";
        using (var writer = new StreamWriter(outputPath, false))
        {
            // FLOAT TESTS
            writer.WriteLine("[float] Compressed Results:");
            Console.WriteLine("\n[float] Original values:");
            foreach (var v in scientificFloatValues)
                Console.WriteLine(v.ToString("G9", CultureInfo.InvariantCulture));

            foreach (var precision in precisions)
            {
                var name = $"{precision.Value:G}";
                byte[] compressed = scientificFloatValues.CompressWithPrecision(precision);
                float[] decompressed = Builder.DecompressWithPrecision(
                    compressed, scientificFloatValues.Length, precision, (IQuantizationStrategy<float>?)null);

                writer.WriteLine($"Float Precision: {name}");
                writer.WriteLine(Convert.ToBase64String(compressed));
                writer.WriteLine();

                float meanError = (float)CompressionMetrics.MeanError(scientificFloatValues, decompressed);
                float ratio = (float)CompressionMetrics.CompressionRatio(
                    scientificFloatValues.Length * sizeof(float),
                    compressed.Length
                );

                Console.WriteLine($"\n--- Float Precision: {name} ---");
                Console.WriteLine($"Compression Ratio: {ratio:F2}");
                Console.WriteLine($"Mean Error: {meanError:E}");

                Console.WriteLine("Decompressed values:");
                for (int i = 0; i < scientificFloatValues.Length; i++)
                {
                    float error = decompressed[i] - scientificFloatValues[i];
                    Console.WriteLine($"Original: {scientificFloatValues[i],20:G9} | Decompressed: {decompressed[i],20:G9} | Error: {error,12:E}");
                }
            }

            // DOUBLE TESTS
            writer.WriteLine("[double] Compressed Results:");
            Console.WriteLine("\n Double Original values:");
            foreach (var v in scientificDoubleValues)
                Console.WriteLine(v.ToString("G17", CultureInfo.InvariantCulture));

            foreach (var precision in precisions)
            {
                var name = $"{precision.Value:G}";
                byte[] compressed = scientificDoubleValues.CompressWithPrecision(precision);
                double[] decompressed = Builder.DecompressWithPrecision(
                    compressed, scientificDoubleValues.Length, precision, (IQuantizationStrategy<double>?)null);

                // Write compressed result to file
                writer.WriteLine($"Double Precision: {name}");
                writer.WriteLine(Convert.ToBase64String(compressed));
                writer.WriteLine();

                double meanError = CompressionMetrics.MeanError(scientificDoubleValues, decompressed);
                double ratio = CompressionMetrics.CompressionRatio(
                    scientificDoubleValues.Length * sizeof(double),
                    compressed.Length
                );

                Console.WriteLine($"\n--- [double] Precision: {name} ---");
                Console.WriteLine($"Compression Ratio: {ratio:F2}");
                Console.WriteLine($"Mean Error: {meanError:E}");

                Console.WriteLine("Decompressed values:");
                for (int i = 0; i < scientificDoubleValues.Length; i++)
                {
                    double error = decompressed[i] - scientificDoubleValues[i];
                    Console.WriteLine($"Original: {scientificDoubleValues[i],20:G17} | Decompressed: {decompressed[i],20:G17} | Error: {error,22:E}");
                }
            }
        }
    }
}
