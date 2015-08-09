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
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Support;
    using System;

    public class GeoMatchUtil
    {
        public const float EARTH_RADIUS_MILES = 3956.0f;
        public const float EARTH_RADIUS_KM = 6371.0f;

        public const float LATITUDE_DEGREES_MIN = -90.0f;
        public const float LATITUDE_DEGREES_MAX = 90.0f;
        public const float LONGITUDE_DEGREES_MIN = -180.0f;
        public const float LONGITUDE_DEGREES_MAX = 180.0f;

        public static float DegreesToRadians(float degrees)
        {
            return (float)(degrees * (Math.PI / 180));
        }

        public static float GetMilesRadiusCosine(float radiusInMiles)
        {
            float radiusCosine = (float)(Math.Cos(radiusInMiles / EARTH_RADIUS_MILES));
            return radiusCosine;
        }

        public static float GetKMRadiusCosine(float radiusInKM)
        {
            float radiusCosine = (float)(Math.Cos(radiusInKM / EARTH_RADIUS_KM));
            return radiusCosine;
        }

        public static float[] GeoMatchCoordsFromDegrees(float latDegrees, float lonDegrees)
        {
            float[] geoMatchCoords;

            if (float.IsNaN(latDegrees) || float.IsNaN(lonDegrees))
            {
                geoMatchCoords = new float[] { float.NaN, float.NaN, float.NaN };
            }
            else
            {
                geoMatchCoords = GeoMatchCoordsFromRadians((float)(latDegrees * (Math.PI / 180)),
                                                           (float)(lonDegrees * (Math.PI / 180)));
            }
            return geoMatchCoords;
        }

        public static float[] GeoMatchCoordsFromRadians(float latRadians, float lonRadians)
        {
            float[] geoMatchCoords;

            if (float.IsNaN(latRadians) || float.IsNaN(lonRadians))
            {
                geoMatchCoords = new float[] { float.NaN, float.NaN, float.NaN };
            }
            else
            {
                geoMatchCoords = new float[]
                {
                    GeoMatchXCoordFromRadians(latRadians, lonRadians),
                    GeoMatchYCoordFromRadians(latRadians, lonRadians),
                    GeoMatchZCoordFromRadians(latRadians)
                };
            }

            return geoMatchCoords;
        }

        public static float GeoMatchXCoordFromDegrees(float latDegrees, float lonDegrees)
        {
            if (float.IsNaN(latDegrees) || float.IsNaN(lonDegrees))
            {
                return float.NaN;
            }

            return GeoMatchXCoordFromRadians((float)(latDegrees * (Math.PI / 180)),
                                             (float)(lonDegrees * (Math.PI / 180)));
        }

        public static float GeoMatchYCoordFromDegrees(float latDegrees, float lonDegrees)
        {
            if (float.IsNaN(latDegrees) || float.IsNaN(lonDegrees))
            {
                return float.NaN;
            }

            return GeoMatchYCoordFromRadians((float)(latDegrees * (Math.PI / 180)),
                                             (float)(lonDegrees * (Math.PI / 180)));
        }

        public static float GeoMatchZCoordFromDegrees(float latDegrees)
        {
            if (float.IsNaN(latDegrees))
            {
                return float.NaN;
            }

            return GeoMatchZCoordFromRadians((float)(latDegrees * (Math.PI / 180)));
        }

        public static float GeoMatchXCoordFromRadians(float latRadians, float lonRadians)
        {
            if (float.IsNaN(latRadians) || float.IsNaN(lonRadians))
            {
                return float.NaN;
            }

            return (float)(Math.Cos(latRadians) * Math.Cos(lonRadians));
        }

        public static float GeoMatchYCoordFromRadians(float latRadians, float lonRadians)
        {
            if (float.IsNaN(latRadians) || float.IsNaN(lonRadians))
            {
                return float.NaN;
            }

            return (float)(Math.Cos(latRadians) * Math.Sin(lonRadians));
        }

        public static float GeoMatchZCoordFromRadians(float latRadians)
        {
            if (float.IsNaN(latRadians))
            {
                return float.NaN;
            }

            return (float)Math.Sin(latRadians);
        }

        public static float GetMatchLatDegreesFromXYZCoords(float x, float y, float z)
        {
            return (float)MathUtil.ToDegrees(Math.Asin(z));
        }

        public static float GetMatchLonDegreesFromXYZCoords(float x, float y, float z)
        {
            float lon = (float)MathUtil.ToDegrees(Math.Asin(y / Math.Cos(Math.Asin(z))));

            if (x < 0 && y > 0)
                return 180.0f - lon;
            else if (y < 0 && x < 0)
                return -180.0f - lon;
            else
                return lon;
        }
    }
}
