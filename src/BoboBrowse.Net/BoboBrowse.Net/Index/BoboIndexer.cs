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
namespace BoboBrowse.Net.Index
{
    using BoboBrowse.Net.Index.Digest;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Store;

    public class BoboIndexer
    {
        private Directory _index;
	    private DataDigester _digester;
	    private IndexWriter _writer;	
	    private Analyzer _analyzer;
	
	    private class MyDataHandler : DataDigester.IDataHandler
        {
		    private IndexWriter _writer;
            public MyDataHandler(IndexWriter writer)
            {
                _writer = writer;
            }
            public virtual void HandleDocument(Document doc)
            {
                _writer.AddDocument(doc);
            }
	    }

        public virtual Analyzer Analyzer
        {
            get { return _analyzer == null ? new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT) : _analyzer; }
            set { _analyzer = value; }
        }
	
	    public BoboIndexer(DataDigester digester, Directory index)
            : base()
        {
		    _index = index;
		    _digester = digester;
	    }	

	    public virtual void Index() 
        {
            using (_writer = new IndexWriter(_index, this.Analyzer, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                MyDataHandler handler = new MyDataHandler(_writer);
                _digester.Digest(handler);
                _writer.Optimize();
            }
	    }	
    }
}
