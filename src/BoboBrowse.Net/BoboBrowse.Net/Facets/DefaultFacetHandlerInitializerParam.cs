// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The 'generic' type for FacetHandler initialization parameters for the purpose of easy serialization.
    /// When this type is used, it is completely up to the program logic of the utilizing RuntimeFacetHandler
    /// and its client code to find the data at the right place.
    /// 
    /// author ymatsuda
    /// </summary>
    public class DefaultFacetHandlerInitializerParam : FacetHandlerInitializerParam
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly IDictionary<string, bool[]> _boolMap;
        private readonly IDictionary<string, int[]> _intMap;
        private readonly IDictionary<string, long[]> _longMap;
        private readonly IDictionary<string, IEnumerable<string>> _stringMap;
        private readonly IDictionary<string, sbyte[]> _byteMap;
        private readonly IDictionary<string, double[]> _doubleMap;

        public DefaultFacetHandlerInitializerParam()
        {
            _boolMap = new Dictionary<string, bool[]>();
            _intMap = new Dictionary<string, int[]>();
            _longMap = new Dictionary<string, long[]>();
            _stringMap = new Dictionary<string, IEnumerable<string>>();
            _byteMap = new Dictionary<string, sbyte[]>();
            _doubleMap = new Dictionary<string, double[]>();
        }

        public override IEnumerable<string> BooleanParamNames
        {
            get { return _boolMap.Keys; }
        }

        public override IEnumerable<string> StringParamNames
        {
            get { return _stringMap.Keys; }
        }

        public override IEnumerable<string> IntParamNames
        {
            get { return _intMap.Keys; }
        }

        public override IEnumerable<string> ByteArrayParamNames
        {
            get { return _byteMap.Keys; }
        }

        public override IEnumerable<string> LongParamNames
        {
            get { return _longMap.Keys; }
        }

        public override IEnumerable<string> DoubleParamNames
        {
            get { return _doubleMap.Keys; }
        }

        public virtual DefaultFacetHandlerInitializerParam PutBooleanParam(string key, bool[] value)
        {
            _boolMap.Put(key, value);
            return this;
        }

        public override bool[] GetBooleanParam(string name)
        {
            return _boolMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutByteArrayParam(string key, sbyte[] value)
        {
            _byteMap.Put(key, value);
            return this;
        }

        public override sbyte[] GetByteArrayParam(string name)
        {
            return _byteMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutIntParam(string key, int[] value)
        {
            _intMap.Put(key, value);
            return this;
        }

        public override int[] GetIntParam(string name)
        {
            return _intMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutLongParam(string key, long[] value)
        {
            _longMap.Put(key, value);
            return this;
        }

        public override long[] GetLongParam(string name)
        {
            return _longMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutStringParam(string key, IEnumerable<string> value)
        {
            _stringMap.Put(key, value);
            return this;
        }

        public override IEnumerable<string> GetStringParam(string name)
        {
            return _stringMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutDoubleParam(string key, double[] value)
        {
            _doubleMap.Put(key, value);
            return this;
        }

        public override double[] GetDoubleParam(string name)
        {
            return _doubleMap.Get(name);
        }

        public virtual void Clear()
        {
            _boolMap.Clear();
            _intMap.Clear();
            _longMap.Clear();
            _stringMap.Clear();
            _byteMap.Clear();
        }
    }
}
