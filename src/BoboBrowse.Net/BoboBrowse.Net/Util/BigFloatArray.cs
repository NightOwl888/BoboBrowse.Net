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
namespace BoboBrowse.Net.Util
{
    public class BigFloatArray
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private float[][] _array;
        private int _numrows;

        // Remember that 2^SHIFT_SIZE = BLOCK_SIZE 
        private const int BLOCK_SIZE = 1024;
        private const int SHIFT_SIZE = 10;
        private const int MASK = BLOCK_SIZE - 1;

        public BigFloatArray(int size)
        {
            _numrows = size >> SHIFT_SIZE;
            _array = new float[_numrows + 1][];
            for (int i = 0; i <= _numrows; i++)
            {
                _array[i] = new float[BLOCK_SIZE];
            }
        }

        public virtual void Add(int docId, float val)
        {
            _array[docId >> SHIFT_SIZE][docId & MASK] = val;
        }

        public virtual float Get(int docId)
        {
            return _array[docId >> SHIFT_SIZE][docId & MASK];
        }

        public virtual int Capacity()
        {
            return _numrows * BLOCK_SIZE;
        }

        public virtual void EnsureCapacity(int size)
        {
            int newNumrows = (size >> SHIFT_SIZE) + 1;
            if (newNumrows > _array.Length)
            {
                float[][] newArray = new float[newNumrows][]; // grow
                System.Array.Copy(_array, 0, newArray, 0, _array.Length);
                for (int i = _array.Length; i < newNumrows; ++i)
                {
                    newArray[i] = new float[BLOCK_SIZE];
                }
                _array = newArray;
            }
            _numrows = newNumrows;
        }
    }
}
