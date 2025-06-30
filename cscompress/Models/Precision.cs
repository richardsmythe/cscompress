namespace FloatingPointCompressor.Models
{
    public struct Precision
    {
        public double Value { get; }
        private Precision(double value)
        {
            Value = value;
        }
        public static readonly Precision TenTrillionths = new Precision(1e-13);
        public static readonly Precision Trillionths = new Precision(1e-12);
        public static readonly Precision HundredMillionths = new Precision(1e-8);
        public static readonly Precision TenMillionths = new Precision(1e-7);
        public static readonly Precision Millionths = new Precision(1e-6);
        public static readonly Precision HundredThousandths = new Precision(1e-5);
        public static readonly Precision TenThousandths = new Precision(1e-4);
        public static readonly Precision Thousandsth = new Precision(1e-3);
        public static readonly Precision Hundredths = new Precision(1e-2);
        public static readonly Precision Tenths = new Precision(1e-1);

        public override string ToString() => Value.ToString("G");
    }


}
