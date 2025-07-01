# CSCompress

This is a utility for compressing and decompressing arrays of floating-point numbers for .NET. The main goal is to reduce the size of floating-point data arrays by applying compression based on a specified precision level. This is useful for optimizing storage and transmission of numerical data where full precision is not always required. Additionally, the compressor utilizes low-level optimizations using **SIMD (Single Instruction, Multiple Data)** for high-speed processing, making it suitable for performance-sensitive applications. Currently, the default compressor is QuantizedInteger, with plans to include more.

## Features
- **Compress floating-point arrays** into compact byte arrays.
- **Decompress** byte arrays back to floating-point arrays.
- **Precision control**: Choose from various precision levels (hundredths, thousandths, millionths, etc.).
- **Efficient storage**: Reduce data size while preserving required accuracy.
- **High-speed SIMD optimization** for performance-critical scenarios.
- **Custom quantization strategies** supported.
- **Fluent API via Builder extensions** for concise usage. Can save to file or read from data file.

## Requirements
- .NET 8 or later

## Quick Start

### Standard Usageusing FloatingPointCompressor;
<pre>
using FloatingPointCompressor.Models;

float[] values = { 1.23f, 4.56f, 7.89f };
Precision precision = Precision.TenThousandths;

// Create compressor instance
var compressor = new CSCompress<float>(values, precision);

// Compress
byte[] compressed = compressor.Compress();

// Decompress
float[] decompressed = compressor.Decompress(compressed);
</pre>

### Fluent API & Builder Extensions
You can use the provided extension methods for a fluent and concise workflow:
<pre>
•	CompressWithPrecision(this float[] values, Precision precision, IQuantizationStrategy<float>? strategy = null)
•	CompressWithPrecision(this double[] values, Precision precision, IQuantizationStrategy<double>? strategy = null)
•	DecompressFloatWithPrecision(this byte[] compressed, int originalLength, Precision precision, IQuantizationStrategy<float>? strategy = null)
•	DecompressDoubleWithPrecision(this byte[] compressed, int originalLength, Precision precision, IQuantizationStrategy<double>? strategy = null)
•	ReadFloatArrayFromFile(string path)
•	ReadDoubleArrayFromFile(string path)
•	SaveToFile(this byte[] data, string path) </pre>

## Precision Levels

The compressor supports the following precision levels, which define how many decimal places are retained in the floating-point values:

- **Ten Trillionths**: 13 decimal places (e.g., `1.2345678901234`)
- **Trillionths**: 12 decimal places (e.g., `1.234567890123`)
- **Hundred Millionths**: 8 decimal places (e.g., `1.23456789`)
- **Ten Millionths**: 7 decimal places (e.g., `1.2345678`)
- **Millionths**: 6 decimal places (e.g., `1.234567`)
- **Hundred Thousandths**: 5 decimal places (e.g., `1.23456`)
- **Ten Thousandths**: 4 decimal places (e.g., `1.2345`)
- **Thousandths**: 3 decimal places (e.g., `1.234`)
- **Hundredths**: 2 decimal places (e.g., `1.23`)
- **Tenths**: 1 decimal place (e.g., `1.2`)

Each level allows you to control how many decimal places are retained, letting you balance between compression ratio and numeric accuracy.

## Benchmark

The benchmark involved compressing and decompressing an array of **1,000,000** floating-point numbers with a precision of "tenths" (1 decimal place). The results are as follows:

### Benchmark Results

| Method              | Mean      | Error    | StdDev    |
|-------------------- |----------:|---------:|----------:|
| CompressBenchmark   |  69.30 ms | 1.262 ms |  1.964 ms |
| DecompressBenchmark | 146.32 ms | 4.639 ms | 13.236 ms |

## TODOs

- Add more compressor types for lossless compression, the aim is to create a suite of compressors.

## Contribution

Please feel free to contribute! Open issues or submit pull requests for improvements and new features.


## Example
<pre>
using System.IO;
using FloatingPointCompressor.Models;
using FloatingPointCompressor.Utils;

// Use some existing data:
double[] values = { 1.23647, 4.5666, -47.823449 };

// Specify your precision:
Precision precision = Precision.Thousandths;

// Compress the double array using the Builder extension:
byte[] compressed = values.CompressWithPrecision(precision);

// Save the compressed data to a file
values.CompressWithPrecision(precision).SaveToFile("compressed_doubles.txt");

// Print to console the compressed base64 data:
Console.WriteLine("Compressed (Base64):");
Console.WriteLine(Convert.ToBase64String(compressed));

// Decompress to see the results:
var decompressed = compressed.DecompressDoubleWithPrecision(values.Length, precision);
Console.WriteLine("\nOriginal values:    " + string.Join(", ", values.Select(v => v.ToString("G17"))));
Console.WriteLine("Decompressed values:" + string.Join(", ", decompressed.Select(v => v.ToString("G17"))));
</pre>

## Installation
CsCompress is available via  <a href='https://www.nuget.org/packages/cscompress'>Nuget</a>
