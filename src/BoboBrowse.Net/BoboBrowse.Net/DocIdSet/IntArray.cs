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

    [Serializable]
    public class IntArray : PrimitiveArray<int>
    {
        public IntArray(int len)
            : base(len)
        {
        }

        public IntArray()
        {
        }

        public virtual void Add(int val)
        {
            EnsureCapacity(Count + 1);
            int[] array = (int[])base.Array;
            array[Count] = val;
            Count++;
        }


        public virtual void Set(int index, int val)
        {
            EnsureCapacity(index);
            int[] array = (int[])base.Array;
            array[index] = val;
            Count = Math.Max(Count, index + 1);
        }

        public virtual int Get(int index)
        {
            int[] array = (int[])base.Array;
            return array[index];
        }

        public virtual bool Contains(int elem)
        {
            int size = Size();
            for (int i = 0; i < size; ++i)
            {
                if (Get(i) == elem)
                {
                    return true;
                }
            }
            return false;
        }

        protected internal override object BuildArray(int len)
        {
            return new int[len];
        }

        public static int GetSerialIntNum(IntArray instance)
        {
            int num = 3 + instance.Count; // _len, _count, _growth
            return num;
        }

        public static int ConvertToBytes(IntArray instance, byte[] @out, int offset)
        {
            int numInt = 0;
            Conversion.IntToByteArray(instance.Len, @out, offset);
            offset += Conversion.BYTES_PER_INT;
            numInt++;

            Conversion.IntToByteArray(instance.Count, @out, offset);
            offset += Conversion.BYTES_PER_INT;
            numInt++;

            Conversion.IntToByteArray(instance.Growth, @out, offset);
            offset += Conversion.BYTES_PER_INT;
            numInt++;

            for (int i = 0; i < instance.Size(); i++)
            {
                int data = instance.Get(i);
                Conversion.IntToByteArray(data, @out, offset);
                offset += Conversion.BYTES_PER_INT;
            }
            numInt += instance.Size();
            return numInt;
        }

        public static IntArray NewInstanceFromBytes(byte[] inData, int offset)
        {
            int len = Conversion.ByteArrayToInt(inData, offset);
            offset += Conversion.BYTES_PER_INT;

            IntArray instance = new IntArray(len);

            int count = Conversion.ByteArrayToInt(inData, offset);
            offset += Conversion.BYTES_PER_INT;

            int growth = Conversion.ByteArrayToInt(inData, offset);
            offset += Conversion.BYTES_PER_INT;

            for (int i = 0; i < count; i++)
            {
                int data = Conversion.ByteArrayToInt(inData, offset);
                offset += Conversion.BYTES_PER_INT;
                instance.Add(data);
            }

            instance.Growth = growth;
            if (instance.Count != count) throw new Exception("cannot build IntArray from byte[]");

            return instance;
        }
    }
}
