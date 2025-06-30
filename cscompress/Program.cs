using BenchmarkDotNet.Running;
using FloatingPointCompressor;
using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Models;
using FloatingPointCompressor.Test;
using FloatingPointCompressor.Utils;
using System.Globalization;
using System.IO;

internal class Program
{
    private static async Task Main(string[] args)
    {


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

        double[] values = { 1.23, 4.56, 7.89 };
        Precision precision = Precision.TenThousandths;
        var compressed = values.CompressWithPrecision(precision);
        compressed.SaveToFile("compressed_doubles.txt");

        // Print compressed data as Base64
        Console.WriteLine("Compressed (Base64):");
        Console.WriteLine(Convert.ToBase64String(compressed));

        // Decompress and print decompressed values
        var decompressed = compressed.DecompressDoubleWithPrecision(values.Length, precision);
        Console.WriteLine("\nOriginal values:    " + string.Join(", ", values.Select(v => v.ToString("G17"))));
        Console.WriteLine("Decompressed values:" + string.Join(", ", decompressed.Select(v => v.ToString("G17"))));
    }
}
