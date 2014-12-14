// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Abstract class for RuntimeFacetHandlers. A concrete RuntimeFacetHandler should implement
    /// the FacetHandlerFactory and RuntimeInitializable so that bobo knows how to create new
    /// instance of the handler at run time and how to initialize it at run time respectively.
    /// 
    /// author ymatsuda
    /// </summary>
    /// <typeparam name="D">type parameter for FacetData</typeparam>
    public abstract class RuntimeFacetHandler : FacetHandler
    {
        /// <summary>
        /// Constructor that specifying the dependent facet handlers using names.
        /// </summary>
        /// <param name="name">the name of this FacetHandler, which is used in FacetSpec and 
        /// Selection to specify the facet. If we regard a facet as a field, the name is like a field name.</param>
        /// <param name="dependsOn">Set of names of facet handlers this facet handler depend on for loading.</param>
        public RuntimeFacetHandler(string name, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">the name of this FacetHandler, which is used in FacetSpec and Selection to specify
        /// the facet. If we regard a facet as a field, the name is like a field name.</param>
        public RuntimeFacetHandler(string name)
            : base(name)
        { }

        //public override D GetFacetData(BoboIndexReader reader)
        //{
        //    return (D)reader.GetRuntimeFacetData(_name);
        //}

        public override object GetFacetData(BoboIndexReader reader)
        {
            return reader.GetRuntimeFacetData(_name);
        }

        public override void LoadFacetData(BoboIndexReader reader)
        {
            reader.PutRuntimeFacetData(_name, Load(reader));
            reader.PutRuntimeFacetData(_name, this);
        }

        public virtual void Close()
        {
        }
    }
}
