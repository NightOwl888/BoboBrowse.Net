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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Analysis.Section
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Tokenattributes;
    using Lucene.Net.Index;

    public class IntMetaDataTokenStream : TokenStream
    {
        private readonly string _tokenText;
        private readonly TermAttribute _termAttribute;
        private readonly OffsetAttribute _offsetAttribute;
        private readonly PayloadAttribute _payloadAtt;
        private Payload _payload;
        private bool _returnToken = false;

        public IntMetaDataTokenStream(string tokenText)
        {
            _tokenText = tokenText;

            // NOTE: Calling the AddAttribute<T> method failed, so 
            // switched to using AddAttributeImpl.
            _termAttribute = new TermAttribute();
            _offsetAttribute = new OffsetAttribute();
            _payloadAtt = new PayloadAttribute();
            base.AddAttributeImpl(_termAttribute);
            base.AddAttributeImpl(_offsetAttribute);
            base.AddAttributeImpl(_payloadAtt);
        }

        /// <summary>
        /// sets meta data
        /// </summary>
        /// <param name="data">array of integer metadata indexed by section id</param>
        public virtual void SetMetaData(int[] data)
        {
            byte[] buf = new byte[data.Length * 4];
            int i = 0;

            for (int j = 0; j < data.Length; j++)
            {
                int datum = data[j];
                buf[i++] = (byte)(datum);
                buf[i++] = (byte)(((uint)datum) >> 8);
                buf[i++] = (byte)(((uint)datum) >> 16);
                buf[i++] = (byte)(((uint)datum) >> 24);
            }

            _payload = new Payload(buf);
            _returnToken = true;
        }

        public override bool IncrementToken()
        {
            if (_returnToken)
            {
                _termAttribute.SetTermBuffer(_tokenText);
                _offsetAttribute.SetOffset(0, 0);
                _payloadAtt.Payload = _payload;
                _returnToken = false;
                return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
