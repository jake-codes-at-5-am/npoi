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
#if NETFRAMEWORK
namespace NPOI.Util
{
    using System;

    /// <summary>
    /// 24-bit RGB colour used on .NET Framework in place of SixLabors.ImageSharp.PixelFormats.Rgb24.
    /// IronSoftware.System.Drawing for .NET Framework is built on SkiaSharp and does not ship ImageSharp,
    /// so NPOI carries this minimal struct with the same fields, constructor and equality.
    /// </summary>
    public struct Rgb24 : IEquatable<Rgb24>
    {
        public byte R;
        public byte G;
        public byte B;

        public Rgb24(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public bool Equals(Rgb24 other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override bool Equals(object obj)
        {
            return obj is Rgb24 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (R << 16) | (G << 8) | B;
        }

        public static bool operator ==(Rgb24 left, Rgb24 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rgb24 left, Rgb24 right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Rgb24({R}, {G}, {B})";
        }
    }
}
#endif