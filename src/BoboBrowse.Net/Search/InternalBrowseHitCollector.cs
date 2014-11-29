//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lucene.Net.Search;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using BoboBrowse.Net.Utils;
    using BoboBrowse.Net.Facets;

    internal class InternalBrowseHitCollector : TopDocsSortedHitCollector
    {
       
        private readonly SortedHitQueue hitQueue;
        private readonly SortField[] sortFields;
        private int totalHits;        
        private readonly BoboIndexReader reader;
        private readonly int offset;
        private readonly int count;
        private readonly BoboBrowser boboBrowser;
        private readonly bool fetchStoredFields;
        private FieldDocEntry bottom;
        private int numHits;
        private Scorer scorer;
        private bool queueFull;
        private int docBase;
        private int[] reverseMul;

        public InternalBrowseHitCollector(BoboBrowser boboBrowser, SortField[] sort, int offset, int count, bool fetchStoredFields)
        {
            this.boboBrowser = boboBrowser;
            reader = boboBrowser.GetIndexReader();
            sortFields = QueryUtils.convertSort(sort, reader);
            this.offset = offset;
            this.count = count;
            this.numHits = offset + count;
            hitQueue = new SortedHitQueue(this.boboBrowser, sortFields, offset + count);
            totalHits = 0;
            this.fetchStoredFields = fetchStoredFields;
            this.reverseMul = (from s in sortFields select s.Reverse ? -1 : 1).ToArray();

        }

        public override void SetScorer(Scorer _scorer)
        {
            this.scorer = _scorer;
            for (int i = 0; i < hitQueue.comparators.Length; i++)
            {
                hitQueue.comparators[i].SetScorer(_scorer);
            }
        }

        public override void Collect(int doc)
        {
            totalHits++;
            var score = scorer.Score();
            if (queueFull)
            {
                // Fastmatch: return if this hit is not competitive
                for (int i = 0; i < reverseMul.Length; i++)
                {
                    int c = reverseMul[i] * hitQueue.comparators[i].CompareBottom(doc);
                    if (c < 0)
                    {
                        // Definitely not competitive.
                        return;
                    }
                    else if (c > 0)
                    {
                        // Definitely competitive.
                        break;
                    }
                    else if (i == hitQueue.comparators.Length - 1)
                    {
                        // Here c=0. If we're at the last comparator, this doc is not
                        // competitive, since docs are visited in doc Id order, which means
                        // this doc cannot compete with any other document in the queue.
                    //    return;
                    }
                }
                for (int i = 0; i < hitQueue.comparators.Length; i++)
                {
                    hitQueue.comparators[i].Copy(bottom.Slot, doc);
                }

                UpdateBottom(doc, score);

                for (int i = 0; i < hitQueue.comparators.Length; i++)
                {
                    hitQueue.comparators[i].SetBottom(bottom.Slot);
                }
            }
            else
            {
                int slot = totalHits - 1;
                for (int i = 0; i < hitQueue.comparators.Length; i++)
                {
                    hitQueue.comparators[i].Copy(slot, doc);
                }

                Add(slot, doc, score);
                if (queueFull)
                {
                    for (int i = 0; i < hitQueue.comparators.Length; i++)
                    {
                        hitQueue.comparators[i].SetBottom(bottom.Slot);
                    }
                }
            }
        }

        internal void Add(int slot, int doc, float score)
        {
            bottom = hitQueue.Add(new FieldDocEntry(slot, doc, score));
            queueFull = totalHits == numHits;
        }

        internal void UpdateBottom(int doc, float score)
        {
            bottom.Doc = docBase + doc;
            bottom.Score = score;
            bottom = hitQueue.UpdateTop();
        }

        public override void SetNextReader(IndexReader _reader, int docBase)
        {
            for (int i = 0; i < hitQueue.comparators.Length; i++)
            {
                hitQueue.comparators[i].SetNextReader(_reader, docBase);
            }
            this.docBase = docBase;
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                return true;
            }
        }

        public override int GetTotalHits()
        {
            return totalHits;
        }

        private void FillInRuntimeFacetValues(BrowseHit[] hits)
        {
            ICollection<FacetHandler> runtimeFacetHandlers = boboBrowser.GetRuntimeFacetHandlerMap().Values;
            foreach (BrowseHit hit in hits)
            {
                Dictionary<string, string[]> map = hit.FieldValues;
                int docid = hit.DocId;
                foreach (FacetHandler facetHandler in runtimeFacetHandlers)
                {
                    string[] values = facetHandler.GetFieldValues(docid);
                    if (values != null)
                    {
                        map.Add(facetHandler.Name, values);
                    }
                }
            }
        }

        public override BrowseHit[] GetTopDocs()
        {
            FieldDocEntry[] fdocs = hitQueue.GetTopDocs(offset, count);
            return BuildHits(fdocs);
        }

        public virtual BrowseHit[] BuildHits(FieldDocEntry[] fdocs)
        {
            BrowseHit[] hits = new BrowseHit[fdocs.Length];
            int i = 0;

            ICollection<FacetHandler> facetHandlers = reader.GetFacetHandlerMap().Values;
            foreach (FieldDocEntry fdoc in fdocs)
            {
                BrowseHit hit = new BrowseHit();
                if (fetchStoredFields)
                {
                    hit.StoredFields = reader.Document(fdoc.Doc);
                }
                Dictionary<string, string[]> map = new Dictionary<string, string[]>();
                foreach (FacetHandler facetHandler in facetHandlers)
                {
                    map.Add(facetHandler.Name, facetHandler.GetFieldValues(fdoc.Doc));
                }
                hit.FieldValues = map;
                hit.DocId = fdoc.Doc;
                hit.Score = fdoc.Score;
                foreach (SortField f in sortFields)
                {
                    if (f.Type != SortField.SCORE && f.Type != SortField.DOC)
                    {
                        string fieldName = f.Field;
                        FieldComparator comparator;
                        hitQueue.comparatorMap.TryGetValue(fieldName, out comparator);
                        if (comparator != null)
                        {
                            hit.AddComparable(fieldName, comparator[fdoc.Slot]);
                        }
                    }
                }
                hits[i++] = hit;
            }
            FillInRuntimeFacetValues(hits);
            return hits;
        }
    }
}
