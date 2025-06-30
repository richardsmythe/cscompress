using BenchmarkDotNet.Attributes;
using FloatingPointCompressor.Compressors;
using FloatingPointCompressor.Models;
using System.Numerics;

namespace FloatingPointCompressor.Benchmark
{
    public class CSCompressBenchmark
    {
        private Precision _precision = Precision.Thousandsth;
        private float[] _values;


        [GlobalSetup]
        public void Setup()
        {
            int numValues = 1000000;
            _values = new float[numValues];
            var r = new Random();

            for (int i = 0; i < _values.Length; i++)
            {  
                _values[i] = (float)(r.NextDouble() * 10);
                _values[i] = (float)Math.Round(_values[i], 6);
            }
        }

        [Benchmark]
        public byte[] CompressBenchmark()
        {
            CSCompress<float> fc = new CSCompress<float>(_values, _precision, new IntegerQuantization<float>());
            return fc.Compress();
        }

        [Benchmark]
        public float[] DecompressBenchmark()
        {
            CSCompress<float> fc = new CSCompress<float>(_values, _precision, new IntegerQuantization<float>());
            var compressedData = fc.Compress();
            return fc.Decompress(compressedData);
        }
    }
}
