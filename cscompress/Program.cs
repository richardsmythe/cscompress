using FloatingPointCompressor.Models;
using FloatingPointCompressor.Utils;

internal class Program
{
    private static async Task Main(string[] args)
    {
        double[] scientificDoubleValues = new double[] {
            5.545000086,
            -7.55112505,
            123456.789,
            -98765.4297,
            3.1415925,
            -2.71828175,
            1.61803389,
            -0.577215672,
            299792.4695
        };

        Precision precision = Precision.Thousandsth;
        var compressed = scientificDoubleValues.CompressWithPrecision(precision);
        compressed.SaveToFile("compressed_doubles.txt");

        // Print compressed data as Base64
        Console.WriteLine($"Precision: {precision}");
        Console.WriteLine("Compressed (Base64):");

        Console.WriteLine(Convert.ToBase64String(compressed));

        // Decompress and print decompressed values
        var decompressed = compressed.DecompressDouble();
        Console.WriteLine("\nOriginal values:    " + string.Join(", ", scientificDoubleValues.Select(v => v.ToString("F9"))));
        Console.WriteLine("Decompressed values:" + string.Join(", ", decompressed.Select(v => v.ToString("F9"))));

        // Print error analysis
        double tolerance = precision.Value;
        bool allWithinTolerance = true;
        Console.WriteLine("\nError analysis (tolerance: " + tolerance + "):");
        for (int i = 0; i < scientificDoubleValues.Length; i++)
        {
            double original = scientificDoubleValues[i];
            double recon = decompressed[i];
            double error = Math.Abs(original - recon);
            bool within = error <= tolerance + 1e-12;
            if (!within) allWithinTolerance = false;
            Console.WriteLine($"Index {i}: = {error:G17} {(within ? "(OK)" : "(EXCEEDS TOLERANCE!)")}");
        }
        Console.WriteLine(allWithinTolerance ? "\nAll values are within the specified tolerance." : "\nSome values exceed the specified tolerance!");
    }
}
