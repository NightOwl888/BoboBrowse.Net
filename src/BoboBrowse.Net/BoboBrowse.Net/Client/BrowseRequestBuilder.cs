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
        private BrowseRequest m_req;
        private string m_qString;

        public BrowseRequestBuilder()
        {
            Clear();
        }

        public void AddSelection(string name, string val, bool isNot)
        {
            BrowseSelection sel = m_req.GetSelection(name);
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
            m_req.AddSelection(sel);
        }

        public void ClearSelection(string name)
        {
            m_req.RemoveSelection(name);
        }

        public void ApplyFacetSpec(string name, int minHitCount, int maxCount, bool expand, FacetSpec.FacetSortSpec orderBy)
        {
            FacetSpec fspec = new FacetSpec();
            fspec.MinHitCount = minHitCount;
            fspec.MaxCount = maxCount;
            fspec.ExpandSelection = expand;
            fspec.OrderBy = orderBy;
            m_req.SetFacetSpec(name, fspec);
        }

        public void ApplySort(SortField[] sorts)
        {
            if (sorts == null)
            {
                m_req.ClearSort();
            }
            else
            {
                m_req.Sort = sorts;
            }
        }

        public void ClearFacetSpecs()
        {
            m_req.FacetSpecs.Clear();
        }
        public void ClearFacetSpec(string name)
        {
            m_req.FacetSpecs.Remove(name);
        }

        public int Offset
        {
            set { m_req.Offset = value; }
        }

        public int Count
        {
            set { m_req.Count = value; }
        }

        public string Query
        {
            set { m_qString = value; }
        }

        public void Clear()
        {
            m_req = new BrowseRequest();
            m_req.Offset = 0;
            m_req.Count = 5;
            m_req.FetchStoredFields = true;
            m_qString = null;
        }

        public void ClearSelections()
        {
            m_req.ClearSelections();
        }

        public BrowseRequest Request
        {
            get { return m_req; }
        }

        public string QueryString
        {
            get { return m_qString; }
        }
    }
}
