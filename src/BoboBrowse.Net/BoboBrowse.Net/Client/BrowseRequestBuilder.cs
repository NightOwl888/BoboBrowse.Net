//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Client
{
    using Lucene.Net.Search;

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
