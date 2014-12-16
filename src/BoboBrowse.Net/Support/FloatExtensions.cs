// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Support
{
    using System;
    using System.IO;

    /// <summary>
    /// Based on answer from: http://stackoverflow.com/questions/7173677/c-sharp-how-to-convert-float-to-int
    /// </summary>
    public static class FloatExtensions
    {
        public static int FloatToIntBits(this float f)
        {
            var s = new MemoryStream();
            var w = new BinaryWriter(s);
            w.Write(f);
            s.Position = 0;
            var r = new BinaryReader(s);
            return r.ReadInt32();
        }
    }

    public static class Float
    {
        public static int floatToIntBits(float f)
        {
            return f.FloatToIntBits();
        }
    }
}
