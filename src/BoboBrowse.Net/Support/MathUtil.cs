namespace BoboBrowse.Net.Support
{
    using System;

    public static class MathUtil
    {
        public static double ToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }
    }
}
