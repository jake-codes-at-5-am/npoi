/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */
namespace TestCases
{
#if NETFRAMEWORK
    using Color = IronSoftware.Drawing.Color;
#else
    using SixLabors.ImageSharp;
#endif

    /// <summary>
    /// Builds the colour type that XSSFColor(Color) takes: SixLabors.ImageSharp.Color, or IronSoftware.Drawing.Color
    /// on .NET Framework, where NPOI has no ImageSharp.
    /// </summary>
    internal static class TestColors
    {
        public static Color FromRgb(byte r, byte g, byte b)
        {
#if NETFRAMEWORK
            return Color.FromArgb(r, g, b);
#else
            return Color.FromRgb(r, g, b);
#endif
        }
    }
}