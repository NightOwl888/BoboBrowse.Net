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
namespace BoboBrowse.Net.Service
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using System.Linq;

    public class DefaultBrowseQueryParser : IBrowseQueryParser
    {
        public virtual DocIdSet Parse(SelectionNode[] selectionNodes, SelectionNode[] notSelectionNodes, int maxDoc)
        {
            DocIdSet docSet = null;
            DocIdSet selSet = null;

            if (selectionNodes != null && selectionNodes.Length > 0)
            {
                List<DocIdSet> selSetList = new List<DocIdSet>(selectionNodes.Length);
                foreach (SelectionNode selectionNode in selectionNodes)
                {
                    DocIdSet ds = selectionNode.DocSet;

                    if (ds != null)
                    {
                        selSetList.Add(ds);
                    }
                }

                if (selSetList.Count > 0)
                {
                    if (selSetList.Count == 1)
                    {
                        selSet = selSetList[0];
                    }
                    else
                    {
                        selSet = new AndDocIdSet(selSetList);
                    }
                }
            }

            DocIdSet notSelSet = null;

            if (notSelectionNodes != null && notSelectionNodes.Length > 0)
            {
                List<DocIdSet> notSelSetList = new List<DocIdSet>(notSelectionNodes.Length);
                foreach (SelectionNode selectionNode in notSelectionNodes)
                {
                    DocIdSet ds = selectionNode.DocSet;

                    if (ds != null)
                    {
                        notSelSetList.Add(ds);
                    }

                    if (notSelSetList.Count > 0)
                    {
                        if (notSelSetList.Count == 1)
                        {
                            notSelSet = notSelSetList[0];
                        }
                        else
                        {
                            notSelSet = new OrDocIdSet(notSelSetList);
                        }
                    }
                }
            }

            if (notSelSet != null)
            {
                notSelSet = new NotDocIdSet(notSelSet, maxDoc);
            }

            if (selSet != null && notSelSet != null)
            {
                DocIdSet[] sets = new DocIdSet[] { selSet, notSelSet };
                docSet = new AndDocIdSet(sets.ToList());
            }
            else if (selSet != null)
            {
                docSet = selSet;
            }
            else if (notSelSet != null)
            {
                docSet = notSelSet;
            }

            return docSet;
        }
    }
}