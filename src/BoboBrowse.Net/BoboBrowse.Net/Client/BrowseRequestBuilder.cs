namespace BoboBrowse.Net.Client
{
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BrowseRequestBuilder
    {
        private BrowseRequest _req;
        private string _qString;

        public BrowseRequestBuilder()
        {
            Clear();
        }

        public void AddSelection(string name, string val, bool isNot)
        {
            BrowseSelection sel = _req.GetSelection(name);
            if (sel == null)
            {
                sel = new BrowseSelection(name);
            }
            if (isNot)
            {
                sel.AddNotValue(val);
            }
            else
            {
                sel.AddValue(val);
            }
            _req.AddSelection(sel);
        }

        public void ClearSelection(string name)
        {
            _req.RemoveSelection(name);
        }

        public void ApplyFacetSpec(string name, int minHitCount, int maxCount, bool expand, FacetSpec.FacetSortSpec orderBy)
        {
            FacetSpec fspec = new FacetSpec();
            fspec.MinHitCount = minHitCount;
            fspec.MaxCount = maxCount;
            fspec.ExpandSelection = expand;
            fspec.OrderBy = orderBy;
            _req.SetFacetSpec(name, fspec);
        }

        public void ApplySort(SortField[] sorts)
        {
            if (sorts == null)
            {
                _req.ClearSort();
            }
            else
            {
                _req.Sort = sorts;
            }
        }

        public void ClearFacetSpecs()
        {
            _req.FacetSpecs.Clear();
        }
        public void ClearFacetSpec(string name)
        {
            _req.FacetSpecs.Remove(name);
        }

        public int Offset
        {
            set { _req.Offset = value; }
        }

        public int Count
        {
            set { _req.Count = value; }
        }

        public string Query
        {
            set { _qString = value; }
        }

        public void Clear()
        {
            _req = new BrowseRequest();
            _req.Offset = 0;
            _req.Count = 5;
            _req.FetchStoredFields = true;
            _qString = null;
        }

        public void ClearSelections()
        {
            _req.ClearSelections();
        }

        public BrowseRequest Request
        {
            get { return _req; }
        }

        public string QueryString
        {
            get { return _qString; }
        }
    }
}
