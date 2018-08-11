using System;
using System.Collections.Generic;

namespace Ico.Binary
{
    internal class ByteWriter
    {
        public List<byte> Data { get; } = new List<byte>();
        public int SeekOffset { get; set; } = 0;

        public ByteWriter()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new Exception("Big-endian systems are not supported");
            }
        }

        public void AddUint8(byte value)
        {
            EnsureCapacity(1);
            Data[SeekOffset++] = value;
        }

        public void AddUint16(ushort value)
        {
            EnsureCapacity(2);
            foreach (var b in BitConverter.GetBytes(value))
            {
                Data[SeekOffset++] = b;
            }
        }

        public void AddUint32(uint value)
        {
            EnsureCapacity(4);
            foreach (var b in BitConverter.GetBytes(value))
            {
                Data[SeekOffset++] = b;
            }
        }

        public void AddInt32(int value)
        {
            EnsureCapacity(4);
            foreach (var b in BitConverter.GetBytes(value))
            {
                Data[SeekOffset++] = b;
            }
        }

        private void EnsureCapacity(int additionalBytesNeeded)
        {
            while (Data.Count < SeekOffset + additionalBytesNeeded)
            {
                Data.Add(0);
            }
        }

        internal void AddBlob(byte[] blob)
        {
            EnsureCapacity(blob.Length);
            foreach (var b in blob)
            {
                Data[SeekOffset++] = b;
            }
        }
    }
}
