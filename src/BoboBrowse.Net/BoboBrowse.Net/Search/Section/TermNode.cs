// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;

    public class TermNode : AbstractTerminalNode
    {
        private byte[] _payloadBuf;
        protected int _positionInPhrase;

        public TermNode(Term term, IndexReader reader)
            : this(term, 0, reader)
        {
        }

        public TermNode(Term term, int positionInPhrase, IndexReader reader)
            : base(term, reader)
        {
            _payloadBuf = new byte[4];
            _positionInPhrase = positionInPhrase; // relative position in a phrase
        }

        internal virtual int PositionInPhrase
        {
            get { return _positionInPhrase; }
        }

        public override int FetchSec(int targetSec)
        {
            if (_posLeft > 0)
            {
                while (true)
                {
                    _curPos = _tp.NextPosition();
                    _posLeft--;

                    if (ReadSecId() >= targetSec) return _curSec;

                    if (_posLeft <= 0) break;
                }
            }
            _curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
            return _curSec;
        }

        // NOTE: Added this method so FetchPos() can be utilized internally
        // without changing the scope of FetchPos() method from protected.
        internal virtual int FetchPosInternal()
        {
            return this.FetchPos();
        }

        protected override int FetchPos()
        {
            if (_posLeft > 0)
            {
                _curPos = _tp.NextPosition();
                _posLeft--;
                return _curPos;
            }
            _curPos = SectionSearchQueryPlan.NO_MORE_POSITIONS;
            return _curPos;
        }

        public virtual int ReadSecId()
        {
            if (_tp.IsPayloadAvailable)
            {
                _curSec = intDecoders[_tp.PayloadLength].Decode(_tp.GetPayload(_payloadBuf, 0));
            }
            else
            {
                _curSec = -1;
            }
            return _curSec;
        }

        private abstract class IntDecoder
        {
            public abstract int Decode(byte[] d);
        }

        private class IntDecoder1 : IntDecoder
        {
            public override int Decode(byte[] d)
            {
                return 0;
            }
        }

        private class IntDecoder2 : IntDecoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff);
            }
        }

        private class IntDecoder3 : IntDecoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8);
            }
        }

        private class IntDecoder4 : IntDecoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8) | ((d[2] & 0xff) << 16);
            }
        }

        private class IntDecoder5 : IntDecoder
        {
            public override int Decode(byte[] d)
            {
                return (d[0] & 0xff) | ((d[1] & 0xff) << 8) | ((d[2] & 0xff) << 16) | ((d[3] & 0xff) << 24);
            }
        }

        private readonly static IntDecoder[] intDecoders = new IntDecoder[]
        {
            new IntDecoder1(),
            new IntDecoder2(),
            new IntDecoder3(),
            new IntDecoder4(),
            new IntDecoder5()
        };
    }
}
