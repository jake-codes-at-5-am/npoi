using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using ICSharpCode.SharpZipLib.Zip;

namespace NPOI.OpenXml4Net.Util
{
    /**
     * Provides a way to get at all the ZipEntries
     *  from a ZipInputStream, as many times as required.
     * Allows a ZipInputStream to be treated much like
     *  a ZipFile, for a price in terms of memory.
     * Be sure to call {@link #close()} as soon as you're
     *  done, to free up that memory!
     */
    public class ZipInputStreamZipEntrySource : ZipEntrySource
    {
        private List<FakeZipEntry> zipEntries;

        /**
         * Reads all the entries from the ZipInputStream
         *  into memory, and closes the source stream.
         * We'll then eat lots of memory, but be able to
         *  work with the entries at-will.
         */
        public ZipInputStreamZipEntrySource(ZipInputStream inp)
        {
            zipEntries = new List<FakeZipEntry>();

            try
            {
                ZipEntry zipEntry;
                while ((zipEntry = inp.GetNextEntry()) != null)
                {
                    FakeZipEntry entry = new FakeZipEntry(zipEntry, inp);
                    zipEntries.Add(entry);
                }
            }
            catch
            {
                // On failure (e.g. OutOfMemoryException), release already-decompressed
                // entries to reduce memory pressure before re-throwing
                foreach (var entry in zipEntries)
                {
                    entry.ClearData();
                }
                zipEntries.Clear();
                throw;
            }
            finally
            {
                inp.Close();
            }
        }

        /// <summary>
        /// Releases the decompressed byte[] data from all entries without closing the source.
        /// Subsequent GetInputStream() calls will return empty streams.
        /// Call this after all part data has been parsed into object models.
        /// </summary>
        public void ReleaseEntryData()
        {
            if (zipEntries == null)
                return;
            foreach (var entry in zipEntries)
            {
                entry.ClearData();
            }
        }

        /// <summary>
        /// Releases the decompressed byte[] data from a specific entry.
        /// Call this after the entry's data has been fully parsed into an object model
        /// and is no longer needed. Subsequent GetInputStream() calls for this entry
        /// will return an empty stream.
        /// </summary>
        public void ReleaseEntryData(ZipEntry entry)
        {
            if (entry is FakeZipEntry fakeEntry)
            {
                fakeEntry.ClearData();
            }
        }

        public IEnumerator Entries
        {
            get
            {
                return new EntryEnumerator(zipEntries);
            }
        }

        public Stream GetInputStream(ZipEntry zipEntry)
        {
            FakeZipEntry entry = (FakeZipEntry)zipEntry;
            return entry.GetInputStream();
        }

        public void Close()
        {
            if (zipEntries != null)
            {
                // Explicitly release data references to help GC
                foreach (var entry in zipEntries)
                {
                    entry.ClearData();
                }
                zipEntries = null;
            }
        }

        public bool IsClosed
        {
            get { return zipEntries == null; }
        }
        /**
         * Why oh why oh why are Iterator and Enumeration
         *  still not compatible?
         */
        internal class EntryEnumerator : IEnumerator
        {
            private List<FakeZipEntry>.Enumerator iterator;

            internal EntryEnumerator(List<FakeZipEntry> zipEntries)
            {
                iterator = zipEntries.GetEnumerator();
            }

            public bool MoveNext()
            {
                return iterator.MoveNext();
            }

            public object Current
            {
                get
                {
                    return iterator.Current;
                }
            }

            #region IEnumerator Members


            public void Reset()
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        /**
         * So we can close the real zip entry and still
         *  effectively work with it.
         * Holds the (decompressed!) data in memory, so
         *  close this as soon as you can!
         */
        public class FakeZipEntry : ZipEntry
        {
            private byte[] data;

            // Larger buffer size (80KB) for improved throughput on large entries,
            // matching .NET's Stream.CopyTo default buffer size
            private const int CopyBufferSize = 81920;

            public FakeZipEntry(ZipEntry entry, ZipInputStream inp) : base(entry.Name)
            {
                long entrySize = entry.Size;

                if (entrySize != -1)
                {
                    if (entrySize >= Int32.MaxValue)
                    {
                        throw new IOException("ZIP entry size is too large");
                    }

                    // Known size: allocate exact byte[] and read directly into it.
                    // This avoids the MemoryStream + ToArray() double-allocation that the
                    // fallback path below performs.
                    //
                    // Trust contract: we trust ZipEntry.Size as reported by SharpZipLib's
                    // ZIP central-directory parser. If a malformed entry produces *more*
                    // decompressed bytes than its declared size, the read loop terminates
                    // at remaining == 0 and any excess bytes are NOT consumed. This means
                    // for malformed inputs:
                    //   - the next entry boundary in the ZipInputStream may be misaligned
                    //     (the next GetNextEntry() call may fail or return a corrupt entry),
                    //   - the silently-discarded bytes are not surfaced to the caller.
                    // For well-formed XLSX files written by Excel, NPOI, OpenOffice, etc.,
                    // the declared size always matches the decompressed length, so this is
                    // the correct trade-off (one allocation, no copy). The malformed-input
                    // case is caught at a higher level when ZIP parsing fails.
                    data = new byte[(int)entrySize];
                    int offset = 0;
                    int remaining = data.Length;
                    while (remaining > 0)
                    {
                        int read = inp.Read(data, offset, remaining);
                        if (read <= 0) break;
                        offset += read;
                        remaining -= read;
                    }

                    // If we read fewer bytes than expected (truncated entry), trim the
                    // array so callers see only the bytes that were actually present.
                    if (remaining > 0)
                    {
                        Array.Resize(ref data, offset);
                    }
                }
                else
                {
                    // Unknown size: use MemoryStream, then try to avoid ToArray() copy
                    MemoryStream baos = new MemoryStream();
                    byte[] buffer = new byte[CopyBufferSize];
                    int read;
                    while ((read = inp.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        baos.Write(buffer, 0, read);
                    }

                    // Try to get the underlying buffer without copying
                    if (baos.TryGetBuffer(out ArraySegment<byte> segment)
                        && segment.Offset == 0
                        && segment.Count == segment.Array.Length)
                    {
                        data = segment.Array;
                    }
                    else
                    {
                        data = baos.ToArray();
                    }
                }
            }

            public Stream GetInputStream()
            {
                if (data == null)
                {
                    return new MemoryStream(Array.Empty<byte>(), writable: false);
                }
                return new MemoryStream(data, writable: false);
            }

            /// <summary>
            /// Releases the decompressed data held by this entry to free memory.
            /// After calling this method, GetInputStream() will return an empty stream.
            /// </summary>
            public void ClearData()
            {
                data = null;
            }
        }
    }

}
