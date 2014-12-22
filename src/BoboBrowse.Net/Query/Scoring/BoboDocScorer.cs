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

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Query.Scoring
{
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    public abstract class BoboDocScorer
    {
        protected readonly IFacetTermScoringFunction _function;
        protected readonly float[] _boostList;

        protected BoboDocScorer(IFacetTermScoringFunction scoreFunction, float[] boostList)
        {
            _function = scoreFunction;
            _boostList = boostList;
        }

        public abstract float Score(int docid);

        public abstract Explanation Explain(int docid);

        public static float[] BuildBoostList(IEnumerable<string> valArray, IDictionary<string, float> boostMap)
        {
            var valArray2 = new List<string>(valArray.Count());
            // NOTE: we must loop through the list in order to make it format
            // the values so it can match the formatted values in the boostMap.
            foreach (var item in valArray)
            {
                valArray2.Add(item);
            }
            float[] boostList = new float[valArray2.Count];
            Arrays.Fill(boostList, 1.0f);
            if (boostMap != null && boostMap.Count > 0)
            {
                Dictionary<string, float>.Enumerator iter = new Dictionary<string, float>(boostMap).GetEnumerator();
                while (iter.MoveNext())
                {
                    KeyValuePair<string, float> entry = iter.Current;
                    int index = valArray2.IndexOf(entry.Key);
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
