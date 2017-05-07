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
namespace BoboBrowse.Net.Query.Scoring
{
    using Lucene.Net.Search;
    using Lucene.Net.Support;
    using System.Collections.Generic;

    public abstract class BoboDocScorer
    {
        protected readonly IFacetTermScoringFunction m_function;
        protected readonly float[] m_boostList;

        protected BoboDocScorer(IFacetTermScoringFunction scoreFunction, float[] boostList)
        {
            m_function = scoreFunction;
            m_boostList = boostList;
        }

        public abstract float Score(int docid);

        public abstract Explanation Explain(int docid);

        public static float[] BuildBoostList(IList<string> valArray, IDictionary<string, float> boostMap)
        {
            float[] boostList = new float[valArray.Count];
            Arrays.Fill(boostList, 1.0f);
            if (boostMap != null && boostMap.Count > 0)
            {
                using (var iter = boostMap.GetEnumerator())
                {
                    while (iter.MoveNext())
                    {
                        KeyValuePair<string, float> entry = iter.Current;
                        int index = valArray.IndexOf(entry.Key);
                        if (index >= 0)
                        {
                            float fval = entry.Value;
                            if (fval >= 0)
                            {
                                boostList[index] = fval;
                            }
                        }
                    }
                }
            }
            return boostList;
        }
    }
}
