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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Statistics
{
    using System;
    using System.Text;

    [Serializable]
    public class FacetCountStatistics
    {
        public virtual double Distribution { get; set; }
        public virtual int TotalSampleCount { get; set; }
        public virtual int CollectedSampleCount { get; set; }
        public virtual int NumSamplesCollected { get; set; }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("num samples collected: ").Append(this.NumSamplesCollected);
            buf.Append("\ncollected sample count: ").Append(this.CollectedSampleCount);
            buf.Append("\ntotal samples count: ").Append(this.TotalSampleCount);
            buf.Append("\ndistribution score: ").Append(this.Distribution);
            return buf.ToString();
        }

        public override bool Equals(object o)
        {
            bool ret = false;
            if (o is FacetCountStatistics)
            {
                FacetCountStatistics stat = (FacetCountStatistics)o;
                if (this.CollectedSampleCount == stat.CollectedSampleCount && 
                    this.NumSamplesCollected == stat.NumSamplesCollected && 
                    this.TotalSampleCount == stat.TotalSampleCount && 
                    this.Distribution == stat.Distribution)
                {
                    ret = true;
                }
            }
            return ret;
        }

        // Required by .NET because Equals() was overridden.
        // Source: http://stackoverflow.com/questions/70303/how-do-you-implement-gethashcode-for-structure-with-two-string#21604191
        public override int GetHashCode()
        {
            // Since any of the properties could change at any time, we need to
            // rely on the default implementation of GetHashCode for Contains.
            return base.GetHashCode();

            //unchecked
            //{
            //    int hashCode = 0;

            //    // int properties
            //    hashCode = (hashCode * 397) ^ this.CollectedSampleCount;
            //    hashCode = (hashCode * 397) ^ this.NumSamplesCollected;
            //    hashCode = (hashCode * 397) ^ this.TotalSampleCount;

            //    // double properties
            //    hashCode = (hashCode * 397) ^ this.Distribution.GetHashCode();

            //    return hashCode;
            //}
        }
    }
}