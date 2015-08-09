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
namespace BoboBrowse.Net.Index.Digest
{
    using System.Text;

    public abstract class FileDigester : DataDigester
    {
        private string _file;

        public FileDigester(string file)
        {
            _file = file;
            this.Encoding = Encoding.UTF8;
        }

        public virtual int MaxDocs { get; set; }
        public virtual Encoding Encoding { get; set; }
        public virtual string File { get { return _file; } }
    }
}
