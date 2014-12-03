namespace BoboBrowse.Net.Service
{
    using Lucene.Net.Search;
    using LuceneExt.Impl;
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