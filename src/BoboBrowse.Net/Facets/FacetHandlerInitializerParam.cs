// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The dummy interface to indicate that a class type can be used for initializing RuntimeFacetHandlers.
    /// 
    /// author xiaoyang
    /// </summary>
    [Serializable]
    public abstract class FacetHandlerInitializerParam
    {
        public static readonly FacetHandlerInitializerParam EMPTY_PARAM = new EmptyFacetHandlerInitializerParam();

        public class EmptyFacetHandlerInitializerParam : FacetHandlerInitializerParam
        {
            public override IEnumerable<string> GetStringParam(string name)
            {
                return new string[0];
            }

            public override int[] GetIntParam(string name)
            {
                return new int[0];
            }

            public override bool[] GetBooleanParam(string name)
            {
                return new bool[0];
            }

            public override long[] GetLongParam(string name)
            {
                return new long[0];
            }

            public override sbyte[] GetByteArrayParam(string name)
            {
                return new sbyte[0];
            }

            public override double[] GetDoubleParam(string name)
            {
                return new double[0];
            }

            public override IEnumerable<string> BooleanParamNames
            {
                get { return new List<string>(); }
            }

            public override IEnumerable<string> StringParamNames
            {
                get { return new List<string>(); }
            }

            public override IEnumerable<string> IntParamNames
            {
                get { return new List<string>(); }
            }

            public override IEnumerable<string> ByteArrayParamNames
            {
                get { return new List<string>(); }
            }

            public override IEnumerable<string> LongParamNames
            {
                get { return new List<string>(); }
            }

            public override IEnumerable<string> DoubleParamNames
            {
                get { return new List<string>(); }
            }
        }

        private static long serialVersionUID = 1L;

        /// <summary>
        /// The transaction ID
        /// </summary>
        private long tid = -1;

        /// <summary>
        /// Get or sets the transaction ID.
        /// </summary>
        /// <returns>the transaction ID.</returns>
        public long Tid 
        { 
            get { return tid; }
            set { this.tid = value; }
        }

        public abstract IEnumerable<string> GetStringParam(string name);
        public abstract int[] GetIntParam(string name);
        public abstract bool[] GetBooleanParam(string name);
        public abstract long[] GetLongParam(string name);
        public abstract byte[] GetByteArrayParam(string name);
        public abstract double[] GetDoubleParam(string name);
        public abstract IEnumerable<string> BooleanParamNames { get; }
        public abstract IEnumerable<string> StringParamNames { get; }
        public abstract IEnumerable<string> IntParamNames { get; }
        public abstract IEnumerable<string> ByteArrayParamNames { get; }
        public abstract IEnumerable<string> LongParamNames { get; }
        public abstract IEnumerable<string> DoubleParamNames { get; }
    }
}
