//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2015  Shad Storhaug
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

namespace BoboBrowse.Net.Support
{
    using System;

    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns the number of milliseconds since January 1, 1970, 00:00:00 GMT represented by this DateTime object 
        /// in universal coordinated time (UTC).
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static long GetTime(this DateTime date)
        {
            DateTime startDate = new DateTime(1970, 1, 1);
            return (long)date.ToUniversalTime().Subtract(startDate).TotalMilliseconds;
        }
    }
}
