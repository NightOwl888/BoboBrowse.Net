//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Util;

    public class TermNode : AbstractTerminalNode
    {
        protected int _positionInPhrase;

        public TermNode(Term term, AtomicReader reader)
            : this(term, 0, reader)
        {
        }

        public TermNode(Term term, int positionInPhrase, AtomicReader reader)
            : base(term, reader)
        {
            _positionInPhrase = positionInPhrase; // relative position in a phrase
        }

        /// <summary>
        /// Added in the .NET version as an accessor to the _positionInPhrase field.
        /// </summary>
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
                    _curPos = _dp.NextPosition();
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
                _curPos = _dp.NextPosition();
                _posLeft--;
                return _curPos;
            }
            _curPos = SectionSearchQueryPlan.NO_MORE_POSITIONS;
            return _curPos;
        }

        public virtual int ReadSecId()
        {
            BytesRef payload = _dp.GetPayload();
            if (payload != null)
            {
                _curSec = intDecoders[payload.Length].Decode(payload.Bytes);
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
