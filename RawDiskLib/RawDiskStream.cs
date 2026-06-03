using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace RawDiskLib
{
    public class RawDiskStream : Stream
    {
        private class ChunkInfo 
        {
            public byte[] Data { get; init; }

            public bool IsDirty { get; set; }
        }

        private const int MAX_CACHE_SIZE_BYTES = 1024 * 1024 * 1024; // Arbitrary limit for caching

        private readonly FileStream _diskStream;
        private readonly int _smallestChunkSize;
        private readonly long _length;

        // Caching mechanism: chunk index to chunk data
        private readonly ArrayPool<byte> _bufferArrayPool;
        private readonly SortedDictionary<long, ChunkInfo> _chunks;
        private readonly Queue<long> _accesses;

        internal RawDiskStream(FileStream diskStream, int smallestChunkSize, long length, ArrayPool<byte> bufferArrayPool)
        {
            _diskStream = diskStream;
            _smallestChunkSize = smallestChunkSize;
            _length = length;

            _bufferArrayPool = bufferArrayPool;
            _chunks = new SortedDictionary<long, ChunkInfo>();
            _accesses = new Queue<long>();
        }

        private ChunkInfo GetChunk(long chunkIndex)
        {
            // Evict least recently used chunks if cache exceeds limit
            while (Math.BigMul(_chunks.Count, _smallestChunkSize) > MAX_CACHE_SIZE_BYTES && 
                _accesses.TryDequeue(out long oldestChunkIndex))
            {
                _chunks.Remove(oldestChunkIndex, out ChunkInfo oldestChunkInfo);
                if (oldestChunkInfo.IsDirty)
                {
                    _diskStream.Seek(oldestChunkIndex * _smallestChunkSize, SeekOrigin.Begin);
                    _diskStream.Write(oldestChunkInfo.Data, 0, _smallestChunkSize);
                }

                _bufferArrayPool.Return(oldestChunkInfo.Data, clearArray: true);
            }

            if (!_chunks.TryGetValue(chunkIndex, out ChunkInfo chunkInfo))
            {
                chunkInfo = new ChunkInfo
                {
                    Data = _bufferArrayPool.Rent(_smallestChunkSize),
                };

                _diskStream.Seek(chunkIndex * _smallestChunkSize, SeekOrigin.Begin);
                _diskStream.ReadExactly(chunkInfo.Data, 0, _smallestChunkSize);

                _chunks.Add(chunkIndex, chunkInfo);
                _accesses.Enqueue(chunkIndex);
            }

            return chunkInfo;
        }

        public override void Flush()
        {
            foreach ((long chunkIndex, ChunkInfo chunkInfo) in _chunks)
            {
                if (chunkInfo.IsDirty)
                {
                    _diskStream.Seek(chunkIndex * _smallestChunkSize, SeekOrigin.Begin);
                    _diskStream.Write(chunkInfo.Data, 0, _smallestChunkSize);
                }

                _bufferArrayPool.Return(chunkInfo.Data, clearArray: true);
            }

            _chunks.Clear();
            _accesses.Clear();

            _diskStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition = Position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition += offset;
                    break;
                case SeekOrigin.End:
                    newPosition = Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            // Is it valid?
            if (0 > newPosition || newPosition > Length)
                throw new ArgumentOutOfRangeException("Out of bounds");

            // Valid
            Position = newPosition;

            return Position;
        }

        public override void SetLength(long value)
        {
            _diskStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            (long chunkIndex, long chunkOffset) = Math.DivRem(Position, _smallestChunkSize);

            long totalRead = 0;
            while (totalRead < count && Position + totalRead < Length)
            {
                ChunkInfo chunkInfo = GetChunk(chunkIndex++);

                long toCopy = Math.Min(_smallestChunkSize - chunkOffset, count - totalRead);
                toCopy = Math.Min(toCopy, Length - (Position + totalRead));

                Array.Copy(chunkInfo.Data, chunkOffset, buffer, offset + totalRead, toCopy);
                chunkOffset = (chunkOffset + toCopy) % _smallestChunkSize;

                totalRead += toCopy;
            }

            Position += totalRead;
            return (int)totalRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            (long chunkIndex, long chunkOffset) = Math.DivRem(Position, _smallestChunkSize);

            long totalWritten = 0;
            while (totalWritten < count && Position + totalWritten < Length)
            {
                ChunkInfo chunkInfo = GetChunk(chunkIndex++);

                long toCopy = Math.Min(_smallestChunkSize - chunkOffset, count - totalWritten);
                toCopy = Math.Min(toCopy, Length - (Position + totalWritten));
                if(toCopy > 0) chunkInfo.IsDirty = true;

                Array.Copy(buffer, offset + totalWritten, chunkInfo.Data, chunkOffset, toCopy);
                chunkOffset = (chunkOffset + toCopy) % _smallestChunkSize;

                totalWritten += toCopy;
            }

            Position += totalWritten;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Flush(); _diskStream.Dispose();
            }
        }

        public override bool CanRead => _diskStream.CanRead;

        public override bool CanSeek => _diskStream.CanSeek;

        public override bool CanWrite => _diskStream.CanWrite;

        public override long Length => _length;

        public override long Position { get; set; }
    }
}
