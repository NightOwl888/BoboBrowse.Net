using BoboBrowse.Net;
using BoboBrowse.Net.Impl;
using CarDemo.Models;
using Lucene.Net.Search;
using System.Collections.Generic;

namespace CarDemo.BoboServices
{
    public class BrowseRequestConverter
    {
        private readonly BoboDefaultQueryBuilder queryBuilder;

        public BrowseRequestConverter()
        {
            this.queryBuilder = new BoboDefaultQueryBuilder();
        }

        public BrowseRequest ConvertBrowseRequest(BoboRequest boboRequest)
        {
            Query query = this.queryBuilder.ParseQuery(boboRequest.Query, boboRequest.Df);
            Sort sort = this.queryBuilder.ParseSort(boboRequest.Sort);

            var browseRequest = new BrowseRequest();
            browseRequest.Offset = boboRequest.Start;
            browseRequest.Count = boboRequest.Rows;
            browseRequest.Query = query;

            if (sort != null)
            {
                SortField[] sortFields = sort.GetSort();
                if (sortFields != null && sortFields.Length > 0)
                {
                    browseRequest.Sort = sortFields;
                }
            }

            var selMap = new Dictionary<string, BrowseSelection>();
            foreach (var selection in boboRequest.Selections)
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
                    browseRequest.AddSelection(sel);
                }
            }

            if (boboRequest.Facet == true)
            {
                foreach (var facet in boboRequest.Facets)
                {
                    FacetSpec fspec = new FacetSpec();
                    browseRequest.SetFacetSpec(facet.Name, fspec);

                    fspec.MinHitCount = facet.MinCount == int.MinValue ? 0 : facet.MinCount;
                    fspec.MaxCount = facet.Limit == int.MinValue ? 100 : facet.Limit;
                    fspec.ExpandSelection = facet.Expand;
                    fspec.OrderBy = ParseFacetSort(facet.Sort, FacetSpec.FacetSortSpec.OrderHitsDesc);
                }
            }

            return browseRequest;
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
}