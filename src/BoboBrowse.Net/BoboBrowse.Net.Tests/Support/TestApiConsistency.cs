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
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class TestApiConsistency
    {
        [Test]
        [TestCase(typeof(BoboBrowse.Net.BoboBrowser))]
        public virtual void TestForPublicMembersContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            var names = GetMembersContainingNonNetNumeric(typeFromTargetAssembly.GetTypeInfo().Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names containing the word 'Int' not followed " +
                "by 16, 32, or 64, 'Long', 'Short', or 'Float' detected. " +
                "In .NET, we need to change to 'Short' to 'Int16', 'Int' to 'Int32', 'Long' to 'Int64', and 'Float' to 'Short'.");
        }

        [Test]
        [TestCase(typeof(BoboBrowse.Net.BoboBrowser))]
        public virtual void TestForTypesContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            var names = GetTypesContainingNonNetNumeric(typeFromTargetAssembly.GetTypeInfo().Assembly);

            //if (VERBOSE)
            //{
            foreach (var name in names)
            {
                Console.WriteLine(name);
            }
            //}

            Assert.IsFalse(names.Any(), names.Count() + " member names containing the word 'Int' not followed " +
                "by 16, 32, or 64, 'Long', 'Short', or 'Float' detected. " +
                "In .NET, we need to change to 'Short' to 'Int16', 'Int' to 'Int32', 'Long' to 'Int64', and 'Float' to 'Short'." +
                "\n\nIMPORTANT: Before making changes, make sure to rename any types with ambiguous use of the word `Single` (meaning 'singular' rather than `System.Single`) to avoid confusion.");
        }




        /// <summary>
        /// Public methods and properties should not contain the word "Int" that is not followed by 16, 32, or 64,
        /// "Long", "Short", or "Float". These should be converted to their .NET names "Int32", "Int64", "Int16", and "Short".
        /// Note we need to ignore common words such as "point", "intern", and "intersect".
        /// </summary>
        private static Regex ContainsNonNetNumeric = new Regex("(?<![Pp]o|[Pp]r|[Jj]o)[Ii]nt(?!16|32|64|er|eg|ro)|[Ll]ong(?!est|er)|[Ss]hort(?!est|er)|[Ff]loat", RegexOptions.Compiled);

        /// <summary>
        /// Constants should not contain the word INT that is not followed by 16, 32, or 64, LONG, SHORT, or FLOAT
        /// </summary>
        private static Regex ConstContainsNonNetNumeric = new Regex("(?<!PO|PR|JO)INT(?!16|32|64|ER|EG|RO)|LONG(?!EST|ER)|SHORT(?!EST|ER)|FLOAT", RegexOptions.Compiled);



        private static IEnumerable<string> GetMembersContainingNonNetNumeric(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                //if (ContainsComparer.IsMatch(t.Name) && t.IsVisible)
                //{
                //    result.Add(t.FullName);
                //}

                var members = t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                foreach (var member in members)
                {
                    //// Ignore properties, methods, and events with IgnoreNetNumericConventionAttribute
                    //if (System.Attribute.IsDefined(member, typeof(ExceptionToNetNumericConventionAttribute)))
                    //{
                    //    continue;
                    //}

                    if (ContainsNonNetNumeric.IsMatch(member.Name) && member.DeclaringType.Equals(t.GetTypeInfo().UnderlyingSystemType))
                    {
                        if (member.MemberType == MemberTypes.Method && !(member.Name.StartsWith("get_", StringComparison.Ordinal) || member.Name.StartsWith("set_", StringComparison.Ordinal)))
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, "()"));
                        }
                        else if (member.MemberType == MemberTypes.Property)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name));
                        }
                        else if (member.MemberType == MemberTypes.Event)
                        {
                            result.Add(string.Concat(t.FullName, ".", member.Name, " (event)"));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private static IEnumerable<string> GetTypesContainingNonNetNumeric(Assembly assembly)
        {
            var result = new List<string>();

            var types = assembly.GetTypes();

            foreach (var t in types)
            {
                if (ContainsNonNetNumeric.IsMatch(t.Name))
                {
                    result.Add(t.FullName);
                }
            }

            return result.ToArray();
        }
    }
}
