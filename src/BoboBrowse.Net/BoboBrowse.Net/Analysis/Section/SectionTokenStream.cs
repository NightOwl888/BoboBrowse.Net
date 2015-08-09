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

    /// <summary>
    /// This class augments a token stream by attaching a section id as payloads.
    /// </summary>
    public sealed class SectionTokenStream : TokenFilter
    {
        private Payload _payload;
        private PayloadAttribute _payloadAtt;

        public SectionTokenStream(TokenStream tokenStream, int sectionId)
            : base(tokenStream)
        {
            // NOTE: Calling the AddAttribute<T> method failed, so 
            // switched to using AddAttributeImpl.
            _payloadAtt = new PayloadAttribute();
            AddAttributeImpl(_payloadAtt);
            _payload = EncodeIntPayload(sectionId);
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                _payloadAtt.Payload = _payload;
                return true;
            }
            return false;
        }

        public static Payload EncodeIntPayload(int id)
        {
            byte[] data = new byte[4];
            int off = data.Length;

            do
            {
                data[--off] = (byte)(id);
                id = (int)(((uint)id) >> 8);
            }
            while (id > 0);

            return new Payload(data, off, data.Length - off);
        }

        public static int DecodeIntPayload(Payload payload)
        {
            return DecodeIntPayload(payload.GetData(), payload.Offset, payload.Length);
        }

        public static int DecodeIntPayload(byte[] data, int off, int len)
        {
            int endOff = off + len;
            int val = 0;
            while (off < endOff)
            {
                val <<= 8;
                val += (data[off++] & 0xFF);
            }
            return val;
        }
    }
}
