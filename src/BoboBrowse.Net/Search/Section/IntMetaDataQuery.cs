// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;

    public class IntMetaDataQuery : MetaDataQuery
    {
        private static long serialVersionUID = 1L;
  
        private Validator _validator;

        public abstract class Validator
        {
            public abstract bool Validate(int datum);
        }

        public class SimpleValueValidator : Validator
        {
            private static long serialVersionUID = 1L;

            private readonly int _val;

            public SimpleValueValidator(int val)
            {
                _val = val;
            }

            public override bool Validate(int datum)
            {
                return (datum == _val);
            }

            public override string ToString()
            {
                return "SingleValueValidator[" + _val + "]";
            }
        }

        public class SimpleRangeValidator : Validator
        {
            private static long serialVersionUID = 1L;

            private readonly int _lower;
            private readonly int _upper;

            public SimpleRangeValidator(int lower, int upper)
            {
                _lower = lower;
                _upper = upper;
            }

            public override bool Validate(int datum)
            {
                return (datum >= _lower && datum <= _upper);
            }

            public override string ToString()
            {
                return "RangeValidator[" + _lower + "," + _upper + "]";
            }
        }

        /// <summary>
        /// constructs IntMetaDataQueryQuery
        /// </summary>
        /// <param name="term"></param>
        /// <param name="validator"></param>
        public IntMetaDataQuery(Term term, Validator validator)
            : base(term)
        {
            _validator = validator;
        }

        public override string ToString(string field)
        {
            return "IntMetaDataQuery(" + _validator.ToString() + ")";
        }

        public override Weight CreateWeight(Searcher searcher)
        {
            throw new NotSupportedException();
        }

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override SectionSearchQueryPlan GetPlan(IndexReader reader)
        {
            return new IntMetaDataNodeNoCache(_term, reader, _validator);
        }

        public override SectionSearchQueryPlan GetPlan(IMetaDataCache cache)
        {
            return new IntMetaDataNode((IntMetaDataCache)cache, _validator);
        }

        public class IntMetaDataNodeNoCache : AbstractTerminalNode
        {
            private readonly Validator _validator;
            private byte[] _data;
            private int _dataLen;

            public IntMetaDataNodeNoCache(Term term, IndexReader reader, Validator validator)
                : base(term, reader)
            {
                _validator = validator;
            }

            public override int FetchDoc(int targetDoc)
            {
                _dataLen = -1;
                return base.FetchDoc(targetDoc);
            }

            public override int FetchSec(int targetSec)
            {
                if (_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return _curSec;

                if (targetSec <= _curSec) targetSec = _curSec + 1;

                if (_dataLen == -1 && _posLeft > 0)
                {
                    _tp.NextPosition();
                    if (_tp.IsPayloadAvailable)
                    {
                        _dataLen = _tp.PayloadLength;
                        _data = _tp.GetPayload(_data, 0);
                    }
                }
                int offset = targetSec * 4;
                while (offset + 4 <= _dataLen)
                {
                    int datum = ((_data[offset] & 0xff) |
                                 ((_data[offset + 1] & 0xff) << 8) |
                                 ((_data[offset + 2] & 0xff) << 16) |
                                 ((_data[offset + 3] & 0xff) << 24));

                    if (_validator.Validate(datum))
                    {
                        _curSec = targetSec;
                        return _curSec;
                    }
                    targetSec++;
                    offset = targetSec * 4;
                }
                _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return _curSec;
            }
        }

        public class IntMetaDataNode : SectionSearchQueryPlan
        {
            private readonly IntMetaDataCache _cache;
            private readonly Validator _validator;
            private readonly int _maxDoc;

            private int _maxSec;

            public IntMetaDataNode(IntMetaDataCache cache, Validator validator)
            {
                _cache = cache;
                _maxDoc = cache.MaxDoc;
                _validator = validator;
            }

            public override int FetchDoc(int targetDoc)
            {
                if (_curDoc == DocIdSetIterator.NO_MORE_DOCS) return _curDoc;

                if (targetDoc <= _curDoc) targetDoc = _curDoc + 1;

                _curSec = -1;

                while (targetDoc < _maxDoc)
                {
                    _maxSec = _cache.GetNumItems(targetDoc);

                    if (_maxSec <= 0)
                    {
                        targetDoc++;
                        continue;
                    }
                    _curDoc = targetDoc;
                    return _curDoc;
                }
                _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                return _curDoc;
            }

            public override int FetchSec(int targetSec)
            {
                if (_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return _curSec;

                if (targetSec <= _curSec) targetSec = _curSec + 1;

                while (targetSec < _maxSec)
                {
                    int datum = _cache.GetValue(_curDoc, targetSec, 0);

                    if (_validator.Validate(datum))
                    {
                        _curSec = targetSec;
                        return _curSec;
                    }
                    targetSec++;
                }
                _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return _curSec;
            }
        }
    }
}
