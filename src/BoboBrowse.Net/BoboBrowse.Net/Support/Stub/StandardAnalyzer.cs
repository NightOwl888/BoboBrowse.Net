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

namespace Lucene.Net.Analysis.Standard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Core;
    using Lucene.Net.Analysis.Util;
    using Lucene.Net.Util;

    /// <summary>
    /// This class is a temporary stub so the project will compile until an implementation is available in Lucene.Net
    /// 
    /// NOTE: This is the same implementation as the Lucene.Net SimpleAnalyzer (rather than StandardAnalyzer).
    /// </summary>
    public class StandardAnalyzer : Analyzer
    {
        private readonly LuceneVersion matchVersion;

        public StandardAnalyzer(LuceneVersion matchVersion)
        {
            this.matchVersion = matchVersion;
        }

        public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            return new TokenStreamComponents(new LowerCaseTokenizer(matchVersion, reader));
        }
    }
}