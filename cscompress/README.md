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
using FloatingPointCompressor.Models;

float[] values = { 1.23f, 4.56f, 7.89f };
Precision precision = Precision.TenThousandths;

// Create compressor instance
var compressor = new CSCompress<float>(values, precision);

// Compress
byte[] compressed = compressor.Compress();

// Decompress
float[] decompressed = compressor.Decompress(compressed);

### Fluent API & Builder Extensions
You can use extension methods for a more fluent and concise API:
using FloatingPointCompressor.Utils;

float[] values = { 1.23f, 4.56f, 7.89f };
Precision precision = Precision.TenThousandths;

// Compress with extension method
byte[] compressed = values.CompressWithPrecision(precision);

// Decompress with extension method
float[] decompressed = Builder.DecompressWithPrecision(compressed, values.Length, precision);
You can also specify a custom quantization strategy as an optional argument.

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

- Add more compressor types for lossless compression.

## Contribution

Please feel free to contribute! Open issues or submit pull requests for improvements and new features.



