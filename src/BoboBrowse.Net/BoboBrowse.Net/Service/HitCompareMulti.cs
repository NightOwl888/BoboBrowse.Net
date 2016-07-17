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
    using System.Collections.Generic;

    public class HitCompareMulti : IComparer<BrowseHit>
    {
        private IComparer<BrowseHit>[] _hcmp;

        public HitCompareMulti(IComparer<BrowseHit>[] hcmp)
        {
            _hcmp = hcmp;
        }

        // HitCompare
        public virtual int Compare(BrowseHit h1, BrowseHit h2)
        {
            int retVal = 0;
            for (int i = 0; i < _hcmp.Length; ++i)
            {
                retVal = _hcmp[i].Compare(h1, h2);
                if (retVal != 0) break;
            }
            return retVal;
        }
    }
}