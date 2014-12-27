using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using BoboBrowse.Net;
using BoboBrowse.Net.Facets;
using BoboBrowse.Net.Facets.Data;
using BoboBrowse.Net.Facets.Impl;
using BoboBrowse.Net.Impl;
using BoboBrowse.Net.Support;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System.Text.RegularExpressions;

namespace CarDemo.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }

        //public ActionResult Browse(string q, string df, int start, int rows, bool facet)
        //{
        //    return View();
        //}

        [HttpPost]
        public ActionResult Browse(Models.BoboRequest bobo)
        {
            //string qstring = Request.QueryString["q"];
            //string df = Request.QueryString["df"];
            //string sortString = Request.QueryString["sort"];
            //BoboDefaultQueryBuilder qbuilder = new BoboDefaultQueryBuilder();
            //Query query = qbuilder.ParseQuery(qstring, df);
            //Sort sort = qbuilder.ParseSort(sortString);
            //var br = new BrowseRequest();

            //int offset = GetRequestInt("start", 0);
            //int count = GetRequestInt("rows", 10);

            ////string defaultMinCountParam = "f." + df + ".facet.mincount";
            //string defaultMinCountParam = "facet.mincount";
            //int defaultMinCount = GetRequestInt(defaultMinCountParam, 0);

            ////string defaultLimitParam = "f." + df + ".facet.limit";
            //string defaultLimitParam = "facet.limit";
            //int defaultLimit = GetRequestInt(defaultLimitParam, 100);
            //if (defaultLimit < 0) defaultLimit = int.MaxValue;

            //string[] fields = Request.QueryString["facet.field"].Split(',');

            ////string facetSortString = Request.QueryString["f." + df + ".facet.sort"];
            //string facetSortString = Request.QueryString["facet.sort"];

            ////foreach (var field in fields)
            ////{
            ////    string fieldSortParam = "f." + field + ".facet.sort";
            ////    var fieldSort = Request.QueryString[fieldSortParam];
            ////    if (!string.IsNullOrEmpty(fieldSort) && fieldSort != "index")
            ////    {
            ////        facetSortString = fieldSort;
            ////    }
            ////}

            //FacetSpec.FacetSortSpec defaultFacetSortSpec = ParseFacetSort(facetSortString, FacetSpec.FacetSortSpec.OrderHitsDesc);

            //br.Offset = offset;
            //br.Count = count;
            //br.Query = query;

            //if (sort != null)
            //{
            //    SortField[] sortFields = sort.GetSort();
            //    if(sortFields != null && sortFields.Length > 0)
            //    {
            //        br.Sort = sortFields;
            //    }
            //}

            //// Fill Bobo Selections
            //FillBoboSelections(br);
               
            //if (string.IsNullOrEmpty(Request.QueryString["facet"]) || Request.QueryString["facet"].Equals("true") && fields != null)
            //{
            //    // filling facets
            //    foreach (var facetField in fields)
            //    {
            //        FacetSpec fspec = new FacetSpec();
            //        br.SetFacetSpec(facetField, fspec);

            //        fspec.MinHitCount = GetRequestInt("f." + facetField + ".facet.mincount", defaultMinCount);
            //        fspec.MaxCount = GetRequestInt("f." + facetField + ".facet.limit", defaultLimit);
            //        fspec.ExpandSelection = GetRequestBool("f." + facetField + ".facet.expand", false);
            //        fspec.OrderBy = ParseFacetSort(Request.QueryString["f." + facetField + ".facet.sort"], defaultFacetSortSpec);
            //    }
            //}

            BoboDefaultQueryBuilder qbuilder = new BoboDefaultQueryBuilder();
            Query query = QueryProducer.Convert(bobo.Query, bobo.Df);
            Sort sort = qbuilder.ParseSort(bobo.Sort);

            var br = new BrowseRequest();
            br.Offset = bobo.Start;
            br.Count = bobo.Rows;
            br.Query = query;

            if (sort != null)
            {
                SortField[] sortFields = sort.GetSort();
                if (sortFields != null && sortFields.Length > 0)
                {
                    br.Sort = sortFields;
                }
            }

            var selMap = new Dictionary<string, BrowseSelection>();
            foreach (var selection in bobo.Selections)
            {
                BrowseSelection sel = selMap.ContainsKey(selection.Name) ? selMap[selection.Name] : null;
                if (sel == null)
                {
                    sel = new BrowseSelection(selection.Name);
                    selMap.Add(selection.Name, sel);
                }
                foreach (var val in selection.Values)
                {
                    sel.AddValue(val);
                }

                sel.SelectionOperation = selection.SelectionOperation;

                sel.SetSelectionProperty("depth", selection.Depth.ToString());
                sel.SetSelectionProperty("strict", selection.Strict.ToString().ToLower());
            }
            if (selMap.Count > 0)
            {
                var sels = selMap.Values;
                foreach (var sel in sels)
                {
                    br.AddSelection(sel);
                }
            }

            if (bobo.Facet == true)
            {
                foreach (var facet in bobo.Facets)
                {
                    FacetSpec fspec = new FacetSpec();
                    br.SetFacetSpec(facet.Name, fspec);

                    fspec.MinHitCount = facet.MinCount == int.MinValue ? 0 : facet.MinCount;
                    fspec.MaxCount = facet.Limit == int.MinValue ? 100 : facet.Limit;
                    fspec.ExpandSelection = facet.Expand;
                    fspec.OrderBy = ParseFacetSort(facet.Sort, FacetSpec.FacetSortSpec.OrderHitsDesc);
                }
            }

            // End build request

            try
            {
                using (var browseResult = Browse(br))
                {
                    //browseResult.Time

                    return Json(new Models.BoboResult(browseResult), "application/json");
                }
            }
            catch (Exception ex)
            {
                var message = ex.ToString();
                return null;
            }
        }

        private BrowseResult Browse(BrowseRequest browseRequest)
        {


            // TODO: put this value in the config file.
            string indexDir = Server.MapPath("~/LuceneIndex/");

            // TODO: configure facet handlers via XML
            var facetHandlers = new List<IFacetHandler>();
            facetHandlers.Add(new SimpleFacetHandler("color") { TermCountSize = BoboBrowse.Net.Facets.TermCountSize.Small });
            facetHandlers.Add(new SimpleFacetHandler("category") { TermCountSize = BoboBrowse.Net.Facets.TermCountSize.Medium });
            facetHandlers.Add(new PathFacetHandler("city") { Separator = "/" });
            facetHandlers.Add(new PathFacetHandler("makemodel") { Separator = "/" });
            facetHandlers.Add(new RangeFacetHandler("year", new PredefinedTermListFactory<int>("00000000000000000000"), new string[] { "[1993 TO 1994]", "[1995 TO 1996]", "[1997 TO 1998]", "[1999 TO 2000]", "[2001 TO 2002]" }));
            facetHandlers.Add(new RangeFacetHandler("price", new PredefinedTermListFactory<float>("00000000000000000000"), new string[] { "[2001 TO 6700]", "[6800 TO 9900]", "[10000 TO 13100]", "[13200 TO 17300]", "[17400 TO 19500]" }));
            facetHandlers.Add(new RangeFacetHandler("mileage", new PredefinedTermListFactory<int>("00000000000000000000"), new string[] { "[* TO 12500]", "[12501 TO 15000]", "[15001 TO 17500]", "[17501 TO *]" }));
            facetHandlers.Add(new MultiValueFacetHandler("tags"));



            System.IO.DirectoryInfo idxDir = new System.IO.DirectoryInfo(indexDir);
            using (IndexReader reader = IndexReader.Open(FSDirectory.Open(idxDir), true))
            {
                using (BoboIndexReader boboReader = BoboIndexReader.GetInstance(reader, facetHandlers))
                {
                    using (BoboBrowser browser = new BoboBrowser(boboReader))
                    {
                        var response = browser.Browse(browseRequest);

                        //response.FacetMap.Values.First().GetFacets().First().FacetValueHitCount;
                        //response.FacetMap.Values.First().GetFacets().First().Value;
                        //response.Hits[0].FieldValues

                        return response;
                    }
                }
            }
        }

        private int GetRequestInt(string fieldName, int defaultValue)
        {
            string value = Request.QueryString[fieldName];
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            int result;
            if (int.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        private bool GetRequestBool(string fieldName, bool defaultValue)
        {
            string value = Request.QueryString[fieldName];
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }
            return defaultValue;
        }

        private void FillBoboSelections(BrowseRequest req)
        {
            string[] facetQueries = Request.QueryString["facet.query"].Split(',');
            if (facetQueries != null && facetQueries.Length > 0)
            {
                var selMap = new Dictionary<string, BrowseSelection>();
                foreach (var facetQuery in facetQueries)
                {
                    string[] parts = facetQuery.Split(':');
                    string name = parts[0];
                    string valval = parts[1];
                    string[] vals = valval.Split(',');
                    if (vals.Length > 0)
                    {
                        BrowseSelection sel = selMap.ContainsKey(name) ? selMap[name] : null;
                        if (sel == null)
                        {
                            sel = new BrowseSelection(name);
                            selMap.Add(name, sel);
                        }
                        foreach (var val in vals)
                        {
                            sel.AddValue(val);
                        }

                        sel.SelectionOperation = GetSelectionOperation(name);

                        string[] selNot = GetSelectionNotValues(name);
                        if (selNot != null && selNot.Length > 0)
                        {
                            sel.NotValues = selNot;
                        }

                        IDictionary<string, string> propMaps = GetSelectionProperties(name);
                        if (propMaps != null && propMaps.Count > 0)
                        {
                            sel.SetSelectionProperties(propMaps);
                        }
                    }
                }

                if (selMap.Count > 0)
                {
                    var sels = selMap.Values;
                    foreach (var sel in sels)
                    {
                        req.AddSelection(sel);
                    }
                }
            }
        }

        public BrowseSelection.ValueOperation GetSelectionOperation(string name)
        {
            string selop = GetBoboParam(name, "selection.op");
            if (selop != null)
            {
                if ("and".Equals(selop))
                {
                    return BrowseSelection.ValueOperation.ValueOperationAnd;
                }
                else if ("or".Equals(selop))
                {
                    return BrowseSelection.ValueOperation.ValueOperationOr;
                }
                else
                {
                    throw new BrowseException(name + ": selection operation: " + selop + " not supported");
                }
            }
            else
            {
                return BrowseSelection.ValueOperation.ValueOperationOr;
            }
        }

        private string[] GetBoboParams(string field,string param)
        {
		    return Request.QueryString["f.bobo." + field + "." + param].Split(',');
	    }

        private string GetBoboParam(string field,string param)
        {
            return Request.QueryString["f.bobo." + field + "." + param];
        }

        public string[] GetSelectionNotValues(string name)
        {
		    return GetBoboParams(name, "selection.not");
	    }

        public IDictionary<string,string> GetSelectionProperties(string name)
        {
		    return GetBoboParamProps(name,"selection.op");
	    }

        private IDictionary<string,string> GetBoboParamProps(string field,string name)
        {
		    var propMap = new Dictionary<string,string>();
		    string[] props = GetBoboParams(field, name + ".prop");
		    if (props!=null && props.Length>0)
            {
			    foreach (string prop in props)
                {
				    string[] parts = prop.Split(':');
				    if (parts.Length==2)
                    {
					    propMap.Add(parts[0], parts[1]);
				    }
			    }
		    }
		    return propMap;
	    }


        private FacetSpec.FacetSortSpec ParseFacetSort(string facetSortString, FacetSpec.FacetSortSpec defaultSort)
        {
            FacetSpec.FacetSortSpec defaultFacetSortSpec;

            if ("count".Equals(facetSortString))
            {
                defaultFacetSortSpec = FacetSpec.FacetSortSpec.OrderHitsDesc;
            }
            else if ("index".Equals(facetSortString))
            {
                defaultFacetSortSpec = FacetSpec.FacetSortSpec.OrderValueAsc;
            }
            else
            {
                defaultFacetSortSpec = defaultSort;
            }
            return defaultFacetSortSpec;
        }
    }

    public class BoboDefaultQueryBuilder
    {
        private static Regex sortSep = new Regex(",", RegexOptions.Compiled);

        public Query ParseQuery(string query, string defaultField)
        {
            try
            {
                return QueryProducer.Convert(query, defaultField);
            }
            catch
            {
                return null;
            }
        }

        public Sort ParseSort(string sortSpec)
        {
            if (sortSpec == null || sortSpec.Length == 0) return null;

            string[] parts = sortSep.Split(sortSpec.Trim());
            if (parts.Length == 0) return null;

            SortField[] lst = new SortField[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                bool top = true;

                int idx = part.IndexOf(' ');
                if (idx > 0)
                {
                    string order = part.Substring(idx + 1).Trim();
                    if ("desc".Equals(order) || "top".Equals(order))
                    {
                        top = true;
                    }
                    else if ("asc".Equals(order) || "bottom".Equals(order))
                    {
                        top = false;
                    }
                    else
                    {
                        throw new ArgumentException("Unknown sort order: " + order);
                    }
                    part = part.Substring(0, idx).Trim();
                }
                else
                {
                    throw new ArgumentException("Missing sort order.");
                }

                if ("score".Equals(part))
                {
                    if (top)
                    {
                        // If thre is only one thing in the list, just do the regular thing...
                        if (parts.Length == 1)
                        {
                            return null; // do normal scoring...
                        }
                        lst[i] = SortField.FIELD_SCORE;
                    }
                    else
                    {
                        lst[i] = new SortField(null, SortField.SCORE, true);
                    }
                }
                else
                {
                    lst[i] = new SortField(part, SortField.STRING, top);
                }
            }
            return new Sort(lst);
        }
    }

    //public class BoboRequestBuilder
    //{
    //    public const string BOBO_PREFIX="bobo";
    //    public const string BOBO_FIELD_SEL_PREFIX="selection";
    //    public const string BOBO_FIELD_SEL_OP=BOBO_FIELD_SEL_PREFIX + ".op";
    //    public const string BOBO_FIELD_SEL_NOT=BOBO_FIELD_SEL_PREFIX + ".not";
    //    public const string BOBO_FACET_EXPAND = "facet.expand"; 

    //    //public static void applyFacetExpand(SolrQuery @params,String name,bool expand){
    //    //    @params.Add("f."+BOBO_PREFIX+"."+name+"."+BOBO_FACET_EXPAND, expand.ToString());
    //    //}


}
