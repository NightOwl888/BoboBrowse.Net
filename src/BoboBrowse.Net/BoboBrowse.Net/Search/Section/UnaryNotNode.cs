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
namespace BoboBrowse.Net.Search.Section
{
    using System;

    /// <summary>
    /// UNARY-NOT operator node
    /// (this node is not supported by SectionSearchQueryPlan)
    /// </summary>
    public class UnaryNotNode : SectionSearchQueryPlan
    {
        private SectionSearchQueryPlan _subquery;

        public UnaryNotNode(SectionSearchQueryPlan subquery)
        {
            _subquery = subquery;
        }

        public virtual SectionSearchQueryPlan GetSubquery()
        {
            return _subquery;
        }

        public override int FetchDoc(int targetDoc)
        {
            throw new NotSupportedException("UnaryNotNode does not support fetchDoc");
        }

        public override int FetchSec(int targetSec)
        {
            throw new NotSupportedException("UnaryNotNode does not support fetchSec");
        }
    }
}
