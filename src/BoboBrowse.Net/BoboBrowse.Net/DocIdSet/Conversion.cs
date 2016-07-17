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
    public class Conversion
    {
        public static int BYTES_PER_INT = 4;
        public static int BYTES_PER_LONG = 8;

        public static void IntToByteArray(int value, byte[] bytes, int offset)
        {
            uint uvalue = (uint)value;
            bytes[offset] = (byte)(int)(uvalue >> 24);
            bytes[offset + 1] = (byte)(int)(uvalue >> 16);
            bytes[offset + 2] = (byte)(int)(uvalue >> 8);
            bytes[offset + 3] = (byte)value;
        }

        public static int ByteArrayToInt(byte[] b, int offset)
        {
            return (b[offset] << 24) + ((b[offset + 1] & 0xFF) << 16) + ((b[offset + 2] & 0xFF) << 8)
                + (b[offset + 3] & 0xFF);
        }

        public static void LongToByteArray(long value, byte[] bytes, int offset)
        {
            ulong uvalue = (ulong)value;
            bytes[offset] = (byte)(long)(uvalue >> 56);
            bytes[offset + 1] = (byte)(long)(uvalue >> 48);
            bytes[offset + 2] = (byte)(long)(uvalue >> 40);
            bytes[offset + 3] = (byte)(long)(uvalue >> 32);
            bytes[offset + 4] = (byte)(long)(uvalue >> 24);
            bytes[offset + 5] = (byte)(long)(uvalue >> 16);
            bytes[offset + 6] = (byte)(long)(uvalue >> 8);
            bytes[offset + 7] = (byte)value;
        }

        public static long ByteArrayToLong(byte[] b, int offset)
        {
            return (b[offset] << 56) + ((b[offset + 1] & 0xFF) << 48) + ((b[offset + 2] & 0xFF) << 40)
                + ((b[offset + 3] & 0xFF) << 32) + ((b[offset + 4] & 0xFF) << 24)
                + ((b[offset + 5] & 0xFF) << 16) + ((b[offset + 6] & 0xFF) << 8) + (b[offset + 7] & 0xFF);
        }
    }
}
