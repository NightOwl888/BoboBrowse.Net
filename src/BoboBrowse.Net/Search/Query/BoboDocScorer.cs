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
    using Lucene.Net.Search;
    using BoboBrowse.Net.Utils;

    public abstract class BoboDocScorer
    {
        protected internal readonly IFacetTermScoringFunction Function;
        protected internal readonly float[] BoostList;

        protected BoboDocScorer(IFacetTermScoringFunction scoreFunction, float[] boostList)
        {
            Function = scoreFunction;
            BoostList = boostList;
        }

        public abstract float Score(int docid);

        public abstract Explanation Explain(int docid);

        public static float[] BuildBoostList(List<string> valArray, Dictionary<string, float> boostMap)
        {
            float[] boostList = new float[valArray.Count];
            Arrays.Fill(boostList, 1.0f);
            if (boostMap != null && boostMap.Count > 0)
            {
                Dictionary<string, float>.Enumerator iter = boostMap.GetEnumerator();
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
            return boostList;
        }
    }
}
