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
using NPOI.POIFS.FileSystem;
using System.Collections;
using System.Collections.Generic;
using System;
#if NETFRAMEWORK
using Color = IronSoftware.Drawing.Color;
#else
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
#endif

namespace NPOI.Util
{
    public class POIUtils
    {

        /**
         * Copies an Entry into a target POIFS directory, recursively
         */

        public static void CopyNodeRecursively(Entry entry, DirectoryEntry target)
        {
            // System.err.println("copyNodeRecursively called with "+entry.GetName()+
            // ","+tarGet.getName());
            DirectoryEntry newTarget = null;
            if (entry.IsDirectoryEntry)
            {
                newTarget = target.CreateDirectory(entry.Name);
                IEnumerator entries = ((DirectoryEntry)entry).Entries;

                while (entries.MoveNext())
                {
                    CopyNodeRecursively((Entry)entries.Current, newTarget);
                }
            }
            else
            {
                DocumentEntry dentry = (DocumentEntry)entry;
                using (DocumentInputStream dstream = new DocumentInputStream(dentry))
                {
                    target.CreateDocument(dentry.Name, dstream);
                    //now part of usings call to Dispose: dstream.Close();
                }
            }
        }

        /**
         * Copies nodes from one POIFS to the other minus the excepts
         * 
         * @param source
         *            is the source POIFS to copy from
         * @param target
         *            is the target POIFS to copy to
         * @param excepts
         *            is a list of Strings specifying what nodes NOT to copy
         */
        public static void CopyNodes(DirectoryEntry sourceRoot,
                DirectoryEntry targetRoot, List<String> excepts)
        {
            IEnumerator entries = sourceRoot.Entries;
            while (entries.MoveNext())
            {
                Entry entry = (Entry)entries.Current;
                if (!excepts.Contains(entry.Name))
                {
                    CopyNodeRecursively(entry, targetRoot);
                }
            }
        }

        /**
         * Copies nodes from one POIFS to the other minus the excepts
         * 
         * @param source
         *            is the source POIFS to copy from
         * @param target
         *            is the target POIFS to copy to
         * @param excepts
         *            is a list of Strings specifying what nodes NOT to copy
         */
        public static void CopyNodes(POIFSFileSystem source,
                POIFSFileSystem target, List<String> excepts)
        {
            // System.err.println("CopyNodes called");
            CopyNodes(source.Root, target.Root, excepts);
        }

        // TODO-ImageSharp: Not sure how to translate System.Drawing.Color.Empty - should
        // we use null instead, or is System.Drawing.Color.Empty used in NPOI like RGB=0?
        public static readonly Rgb24 Color_Empty = new Rgb24(0, 0, 0);

        // Color <-> Rgb24 implicit conversions were removed in SixLabors.ImageSharp 4.x; the explicit
        // FromPixel/ToPixel calls compile and behave identically on every ImageSharp version we target.
#if NETFRAMEWORK
        // .NET Framework has no ImageSharp: Color is IronSoftware.Drawing.Color (a class) and Rgb24 is NPOI.Util.Rgb24.
        internal static Color ToColor(Rgb24 pixel) { return Color.FromArgb(pixel.R, pixel.G, pixel.B); }
        internal static Rgb24 ToRgb24(Color color) { return color is null ? Color_Empty : new Rgb24(color.R, color.G, color.B); }
#else
        internal static Color ToColor(Rgb24 pixel) { return Color.FromPixel(pixel); }
        internal static Rgb24 ToRgb24(Color color) { return color.ToPixel<Rgb24>(); }
#endif

        // SixLabors.Fonts 3.x (used with ImageSharp 4.x on net8.0) removed TextMeasurer.MeasureSize and the
        // culture overload of SystemFonts.TryGet. MeasureBounds returns the same rectangle MeasureSize did.
        internal static SixLabors.Fonts.FontRectangle MeasureTextSize(System.ReadOnlySpan<char> text, SixLabors.Fonts.TextOptions options)
        {
#if NET8_0_OR_GREATER
            return SixLabors.Fonts.TextMeasurer.MeasureBounds(text, options);
#else
            return SixLabors.Fonts.TextMeasurer.MeasureSize(text, options);
#endif
        }

        // string overload: netstandard2.0 has no implicit string -> ReadOnlySpan<char> conversion.
        internal static SixLabors.Fonts.FontRectangle MeasureTextSize(string text, SixLabors.Fonts.TextOptions options)
        {
#if NET8_0_OR_GREATER
            return SixLabors.Fonts.TextMeasurer.MeasureBounds(text, options);
#else
            return SixLabors.Fonts.TextMeasurer.MeasureSize(text, options);
#endif
        }

        internal static bool TryGetSystemFont(string name, out SixLabors.Fonts.FontFamily family)
        {
#if NET8_0_OR_GREATER
            return SixLabors.Fonts.SystemFonts.TryGet(name, out family);
#else
            return SixLabors.Fonts.SystemFonts.TryGet(name, System.Globalization.CultureInfo.CurrentCulture, out family);
#endif
        }  
    }

}