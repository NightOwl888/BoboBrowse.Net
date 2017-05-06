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
namespace BoboBrowse.Net.Util
{
    /// <summary>
    /// NOTE: This was BigFloatArray in bobo-browse
    /// </summary>
    public class BigSingleArray
    {
        private float[][] m_array;
        private int m_numrows;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        public BigSingleArray(int size)
        {
            m_numrows = size >> SHIFT_SIZE;
            m_array = new float[m_numrows + 1][];
            for (int i = 0; i <= m_numrows; i++)
            {
                m_array[i] = new float[BLOCK_SIZE];
            }
        }

        public virtual void Add(int docId, float val)
        {
            m_array[docId >> SHIFT_SIZE][docId & MASK] = val;
        }

        public virtual float Get(int docId)
        {
            return m_array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public virtual int Capacity()
        {
            return m_numrows * BLOCK_SIZE;
        }

        public virtual void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > m_array.Length)
            {
                float[][] newArray = new float[newNumrows][]; // grow
                System.Array.Copy(m_array, 0, newArray, 0, m_array.Length);
                for (int i = m_array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new float[BLOCK_SIZE];
                }
                m_array = newArray;
            }
            m_numrows = newNumrows;
        }
    }
}
