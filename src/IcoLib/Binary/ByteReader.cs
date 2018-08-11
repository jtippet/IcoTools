using System;

namespace Ico.Binary
{
    public class ByteReader
    {
        public Memory<byte> Data { get; private set; }
        public int SeekOffset { get; set; }

        public ByteReader(Memory<byte> data)
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new Exception("Big-endian systems are not supported");
            }

            Data = data;
            SeekOffset = 0;
        }

        public byte NextUint8()
        {
            return Data.Span[SeekOffset++];
        }

        public ushort NextUint16()
        {
            var result = BitConverter.ToUInt16(Data.Span.Slice(SeekOffset, 2).ToArray(), 0);
            SeekOffset += 2;
            return result;
        }

        public uint NextUint32()
        {
            var result = BitConverter.ToUInt32(Data.Span.Slice(SeekOffset, 4).ToArray(), 0);
            SeekOffset += 4;
            return result;
        }

        public int NextInt32()
        {
            var result = BitConverter.ToInt32(Data.Span.Slice(SeekOffset, 4).ToArray(), 0);
            SeekOffset += 4;
            return result;
        }

        public ulong NextUint64()
        {
            var result = BitConverter.ToUInt64(Data.Span.Slice(SeekOffset, 8).ToArray(), 0);
            SeekOffset += 8;
            return result;
        }

        public bool IsEof => SeekOffset == Data.Length;
    }
}
