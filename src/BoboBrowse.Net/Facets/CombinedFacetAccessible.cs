
//namespace BoboBrowse.Net.Facets
//{
//    using Common.Logging;
//    using Lucene.Net.Util;
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;

//     <summary>
//     author nnarkhed
//     </summary>
//    public class CombinedFacetAccessible : IFacetAccessible
//    {
//        private static ILog logger = LogManager.GetLogger<CombinedFacetAccessible>();
//        private readonly IEnumerable<IFacetAccessible> _list;
//        private readonly FacetSpec _fspec;
//        private bool _closed;

//        public CombinedFacetAccessible(FacetSpec fspec, IEnumerable<IFacetAccessible> list)
//        {
//            _list = list;
//            _fspec = fspec;
//        }

//        public override string ToString()
//        {
//            return "_list:" + _list + " _fspec:" + _fspec;
//        }

//        public BrowseFacet GetFacet(string value)
//        {
//            if (_closed)
//            {
//                throw new InvalidOperationException("This instance of count collector was already closed");
//            }
//            int sum = -1;
//            String foundValue = null;
//            if (_list != null)
//            {
//                foreach (IFacetAccessible facetAccessor in _list)
//                {
//                    BrowseFacet facet = facetAccessor.GetFacet(value);
//                    if (facet != null)
//                    {
//                        foundValue = facet.Value;
//                        if (sum == -1) sum = facet.FacetValueHitCount;
//                        else sum += facet.FacetValueHitCount;
//                    }
//                }
//            }
//            if (sum == -1) return null;
//            return new BrowseFacet(foundValue, sum);
//        }

//        public int GetCappedFacetCount(object value, int cap)
//        {
//            if (_closed)
//            {
//                throw new InvalidOperationException("This instance of count collector was already closed");
//            }
//            int sum = 0;
//            if (_list != null)
//            {
//                foreach (IFacetAccessible facetAccessor in _list)
//                {
//                    if (facetAccessor is CombinedFacetAccessible)
//                        sum += ((CombinedFacetAccessible)facetAccessor).GetCappedFacetCount(value, cap - sum);
//                    else
//                        sum += facetAccessor.GetFacetHitsCount(value);
//                    if (sum >= cap)
//                        return cap;
//                }
//            }
//            return sum;
//        }

//        public int GetFacetHitsCount(object value)
//        {
//            if (_closed)
//            {
//                throw new InvalidOperationException("This instance of count collector was already closed");
//            }
//            int sum = 0;
//            if (_list != null)
//            {
//                foreach (IFacetAccessible facetAccessor in _list)
//                {
//                    sum += facetAccessor.GetFacetHitsCount(value);
//                }
//            }
//            return sum;
//        }

//        public IEnumerable<BrowseFacet> GetFacets()
//        {
//            if (_closed)
//            {
//              throw new InvalidOperationException("This instance of count collector was already closed");
//            }
//            int maxCnt = _fspec.MaxCount;
//            if(maxCnt <= 0)
//              maxCnt = int.MaxValue;
//            int minHits = _fspec.MinHitCount;
//            List<BrowseFacet> list = new List<BrowseFacet>();

//            int cnt = 0;
//            IComparable facet = null;
//            FacetIterator iter = (FacetIterator)this.Iterator();
//            Comparator<BrowseFacet> comparator;
//            if (FacetSpec.FacetSortSpec.OrderValueAsc.Equals(_fspec.OrderBy))
//            {
//              while((facet = iter.next(minHits)) != null) 
//              {
//                // find the next facet whose combined hit count obeys minHits
//                list.Add(new BrowseFacet(Convert.ToString(facet), iter.count));
//                if(++cnt >= maxCnt) break;                  
//              }
//            }
//            else if(FacetSpec.FacetSortSpec.OrderHitsDesc.Equals(_fspec.OrderBy))
//            {
//              comparator = new Comparator<BrowseFacet>()
//              {
//                public int compare(BrowseFacet f1, BrowseFacet f2)
//                {
//                  int val=f2.FacetValueHitCount - f1.FacetValueHitCount;
//                  if (val==0)
//                  {
//                    val = (f1.Value.CompareTo(f2.Value));
//                  }
//                  return val;
//                }
//              };       
//              if(maxCnt != int.MaxValue)
//              {
//                // we will maintain a min heap of size maxCnt
//                // Order by hits in descending order and max count is supplied
//                PriorityQueue queue = createPQ(maxCnt, comparator);
//                int qsize = 0;
//                while( (qsize < maxCnt) && ((facet = iter.next(minHits)) != null) )
//                {
//                  queue.add(new BrowseFacet(String.valueOf(facet), iter.count));
//                  qsize++;
//                }
//                if(facet != null)
//                {
//                  BrowseFacet rootFacet = (BrowseFacet)queue.top();
//                  minHits = rootFacet.getHitCount() + 1;
//                  // facet count less than top of min heap, it will never be added 
//                  while(((facet = iter.next(minHits)) != null))
//                  {
//                    rootFacet.setValue(String.valueOf(facet));
//                    rootFacet.setHitCount(iter.count);
//                    rootFacet = (BrowseFacet) queue.UpdateTop();
//                    minHits = rootFacet.getHitCount() + 1;
//                  }
//                }
//                // at this point, queue contains top maxCnt facets that have hitcount >= minHits
//                while(qsize-- > 0)
//                {
//                  // append each entry to the beginning of the facet list to order facets by hits descending
//                  list.addFirst((BrowseFacet) queue.pop());
//                }
//              }
//              else
//              {
//                // no maxCnt specified. So fetch all facets according to minHits and sort them later
//                while((facet = iter.next(minHits)) != null)
//                  list.add(new BrowseFacet(String.valueOf(facet), iter.count));
//                Collections.sort(list, comparator);
//              }
//            }
//            else // FacetSortSpec.OrderByCustom.equals(_fspec.getOrderBy()
//            {
//              comparator = _fspec.getCustomComparatorFactory().newComparator();
//              if(maxCnt != Integer.MAX_VALUE)
//              {
//                PriorityQueue queue = createPQ(maxCnt, comparator);
//                BrowseFacet browseFacet = new BrowseFacet();        
//                int qsize = 0;
//                while( (qsize < maxCnt) && ((facet = iter.next(minHits)) != null) )
//                {
//                  queue.add(new BrowseFacet(String.valueOf(facet), iter.count));
//                  qsize++;
//                }
//                if(facet != null)
//                {
//                  while((facet = iter.next(minHits)) != null)
//                  {
//                    // check with the top of min heap
//                    browseFacet.setHitCount(iter.count);
//                    browseFacet.setValue(String.valueOf(facet));
//                    browseFacet = (BrowseFacet)queue.insertWithOverflow(browseFacet);
//                  }
//                }
//                // remove from queue and add to the list
//                while(qsize-- > 0)
//                  list.addFirst((BrowseFacet)queue.pop());
//              }
//              else 
//              {
//                // order by custom but no max count supplied
//                while((facet = iter.next(minHits)) != null)
//                  list.add(new BrowseFacet(String.valueOf(facet), iter.count));
//                Collections.sort(list, comparator);
//              }
//            }
//            return list;
//        }

//        private PriorityQueue CreatePQ(int max, Comparator<BrowseFacet> comparator)
//        {
//            PriorityQueue queue = new PriorityQueue()
//            {
//              {
//                this.Initialize(max);
//              }
//              @Override
//              protected boolean lessThan(Object arg0, Object arg1)
//              {
//                BrowseFacet o1 = (BrowseFacet)arg0;
//                BrowseFacet o2 = (BrowseFacet)arg1;
//                return comparator.compare(o1, o2) > 0;
//              }     
//            };
//            return queue;
//        }

//        private class CombinedFacetPriorityQueue<T> : PriorityQueue<T>
//        {
//            public override bool LessThan(T a, T b)
//            {
//                BrowseFacet o1 = (BrowseFacet)a;
//                BrowseFacet o2 = (BrowseFacet)b;
//                return comparator.compare(o1, o2) > 0;
//            }
//        }

//        // TODO: Finish implementation
//    }
//}
