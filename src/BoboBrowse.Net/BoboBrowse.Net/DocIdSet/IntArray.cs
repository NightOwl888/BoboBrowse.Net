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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.DocIdSet
{
    using System;

    /// <summary>
    /// NOTE: This was IntArray in bobo-browse
    /// </summary>
    [Serializable]
    public class Int32Array : PrimitiveArray<int>
    {
        public Int32Array(int len)
            : base(len)
        {
        }

        public Int32Array()
        {
        }

        public virtual void Add(int val)
        {
            EnsureCapacity(m_count + 1);
            int[] array = (int[])base.m_array;
            array[m_count] = val;
            m_count++;
        }


        public virtual void Set(int index, int val)
        {
            EnsureCapacity(index);
            int[] array = (int[])base.m_array;
            array[index] = val;
            m_count = Math.Max(m_count, index + 1);
        }

        public virtual int Get(int index)
        {
            int[] array = (int[])base.m_array;
            return array[index];
        }

        public virtual bool Contains(int elem)
        {
            int size = m_count;
            for (int i = 0; i < size; ++i)
            {
                if (Get(i) == elem)
                {
                    return true;
                }
            }
            return false;
        }

        protected override int[] BuildArray(int len)
        {
            return new int[len];
        }

        /// <summary>
        /// NOTE: This was GetSerialIntNum() in bobo-browse
        /// </summary>
        public static int GetSerialInt32Num(Int32Array instance)
        {
            int num = 3 + instance.m_count; // _len, _count, _growth
            return num;
        }

        public static int ConvertToBytes(Int32Array instance, byte[] @out, int offset)
        {
            int numInt = 0;
            Conversion.Int32ToByteArray(instance.m_len, @out, offset);
            offset += Conversion.BYTES_PER_INT32;
            numInt++;

            Conversion.Int32ToByteArray(instance.m_count, @out, offset);
            offset += Conversion.BYTES_PER_INT32;
            numInt++;

            Conversion.Int32ToByteArray(instance.m_growth, @out, offset);
            offset += Conversion.BYTES_PER_INT32;
            numInt++;

            for (int i = 0; i < instance.m_count; i++)
            {
                int data = instance.Get(i);
                Conversion.Int32ToByteArray(data, @out, offset);
                offset += Conversion.BYTES_PER_INT32;
            }
            numInt += instance.m_count;
            return numInt;
        }

        public static Int32Array NewInstanceFromBytes(byte[] inData, int offset)
        {
            int len = Conversion.ByteArrayToInt32(inData, offset);
            offset += Conversion.BYTES_PER_INT32;

            Int32Array instance = new Int32Array(len);

            int count = Conversion.ByteArrayToInt32(inData, offset);
            offset += Conversion.BYTES_PER_INT32;

            int growth = Conversion.ByteArrayToInt32(inData, offset);
            offset += Conversion.BYTES_PER_INT32;

            for (int i = 0; i < count; i++)
            {
                int data = Conversion.ByteArrayToInt32(inData, offset);
                offset += Conversion.BYTES_PER_INT32;
                instance.Add(data);
            }

            instance.m_growth = growth;
            if (instance.m_count != count) throw new Exception("cannot build Int32Array from byte[]");

            return instance;
        }
    }
}
