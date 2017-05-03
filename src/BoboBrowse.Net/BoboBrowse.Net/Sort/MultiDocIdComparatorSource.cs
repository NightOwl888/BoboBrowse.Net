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

﻿// Version compatibility level: 4.2.0
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;

    public class MultiDocIdComparerSource : DocComparerSource
    {
        private readonly DocComparerSource[] m_compSources;

        public MultiDocIdComparerSource(DocComparerSource[] compSources)
        {
            m_compSources = compSources;
        }

        public override DocComparer GetComparer(AtomicReader reader, int docbase)
        {
            DocComparer[] comparers = new DocComparer[m_compSources.Length];
            for (int i = 0; i < m_compSources.Length; ++i)
            {
                comparers[i] = m_compSources[i].GetComparer(reader, docbase);
            }
            return new MultiDocIdComparer(comparers);
        }
    }
}
