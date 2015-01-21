namespace BoboBrowse.Net.Support
{
    using BoboBrowse.Net;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;

    [TestFixture]
    public class RangeStringFormatterTest
    {
        [Test]
        public void TestCurrencyRangeFormat()
        {
            var facets = new List<BrowseFacet>()
            {
                new BrowseFacet("[* TO 00015.99]", 23),
                new BrowseFacet("[00016.00 TO 00049.99]", 16),
                new BrowseFacet("[00073.34 TO 00117.83]", 31),
                new BrowseFacet("[00117.84 TO *]", 14)
            };

            {
                var formatter = new RangeStringFormatter<double>("{0:c} - {1:c}", "< {1:c}", "> {0:c}", new CultureInfo("en-US"));

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "< $15.99";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "$16.00 - $49.99";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "$73.34 - $117.83";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "> $117.84";
                Assert.AreEqual(expected4, actual4);
            }

            {
                var formatter = new RangeStringFormatter<double>("{0:c} - {1:c}", "Menor que {1:c}", "{0:c} e até", new CultureInfo("pt-PT"));

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "Menor que 15,99 €";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "16,00 € - 49,99 €";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "73,34 € - 117,83 €";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "117,84 € e até";
                Assert.AreEqual(expected4, actual4);
            }

            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                var formatter = new RangeStringFormatter<double>("{0:c} to {1:c}");

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "* to $15.99";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "$16.00 to $49.99";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "$73.34 to $117.83";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "$117.84 to *";
                Assert.AreEqual(expected4, actual4);
            }
        }

        [Test]
        public void TestDateRangeFormat()
        {
            var facets = new List<BrowseFacet>()
            {
                new BrowseFacet("[* TO 2000/12/31]", 23),
                new BrowseFacet("[2001/01/01 TO 2002/12/31]", 16),
                new BrowseFacet("[2003/01/01 TO 2006/06/25]", 31),
                new BrowseFacet("[2006/06/26 TO *]", 14)
            };

            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                var formatter = new RangeStringFormatter<DateTime>("{0:d} - {1:d}", "Before {1:d}", "{0:d} And After");

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "Before 12/31/2000";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "1/1/2001 - 12/31/2002";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "1/1/2003 - 6/25/2006";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "6/26/2006 And After";
                Assert.AreEqual(expected4, actual4);
            }

            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

                var formatter = new RangeStringFormatter<DateTime>("{0:D} - {1:D}", "perante {1:D}", "{0:D} e depois");

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "perante 31 de dezembro de 2000";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "1 de janeiro de 2001 - 31 de dezembro de 2002";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "1 de janeiro de 2003 - 25 de junho de 2006";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "26 de junho de 2006 e depois";
                Assert.AreEqual(expected4, actual4);
            }

            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                var formatter = new RangeStringFormatter<DateTime>("{0:d} - {1:d}");

                string actual1 = formatter.Format(facets[0].Value);
                string expected1 = "* - 12/31/2000";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(facets[1].Value);
                string expected2 = "1/1/2001 - 12/31/2002";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(facets[2].Value);
                string expected3 = "1/1/2003 - 6/25/2006";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(facets[3].Value);
                string expected4 = "6/26/2006 - *";
                Assert.AreEqual(expected4, actual4);
            }

            var luceneFacets = new List<BrowseFacet>()
            {
                new BrowseFacet("[* TO 20001231]", 23),
                new BrowseFacet("[20010101 TO 20021231]", 16),
                new BrowseFacet("[20030101 TO 20060625]", 31),
                new BrowseFacet("[20060626 TO *]", 14)
            };

            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

                var formatter = new RangeStringFormatter<DateTime>("{0:d} - {1:d}");

                string actual1 = formatter.Format(luceneFacets[0].Value);
                string expected1 = "* - 12/31/2000";
                Assert.AreEqual(expected1, actual1);

                string actual2 = formatter.Format(luceneFacets[1].Value);
                string expected2 = "1/1/2001 - 12/31/2002";
                Assert.AreEqual(expected2, actual2);

                string actual3 = formatter.Format(luceneFacets[2].Value);
                string expected3 = "1/1/2003 - 6/25/2006";
                Assert.AreEqual(expected3, actual3);

                string actual4 = formatter.Format(luceneFacets[3].Value);
                string expected4 = "6/26/2006 - *";
                Assert.AreEqual(expected4, actual4);
            }
        }
    }
}
