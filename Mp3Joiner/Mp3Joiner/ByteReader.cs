using System.IO;

namespace Mp3Joiner
{
    public class ByteReader
    {
        private const int BufSize = 4096;

        private const int UpperThr = 2048;

        private const int LowerThr = 2048;

        private byte[][] _bufs = new byte[2][] { new byte[BufSize], new byte[BufSize] };

        private BinaryReader _br;

        private int _bufIndex;

        private long _position;

        public ByteReader(BinaryReader br)
        {
            _br = br;
            _bufIndex = 0;
            _br.BaseStream.Position = 0;
            _position = 0;
        }

        public byte GetAt(long at)
        {
            var offset = at - _position;
            if (offset < 0)
            {
                var bi = 1 - _bufIndex;
                var buf = _bufs[bi];
                offset += BufSize;
                if (offset < LowerThr)
                {
                    var otherBuf = _bufs[_bufIndex];
                    _br.BaseStream.Position = _position - BufSize - BufSize;
                    _br.Read(otherBuf, 0, BufSize);
                }
                return buf[offset];
            }
            else
            {
                var buf = _bufs[_bufIndex];
                if (offset > UpperThr)
                {
                    var otherBuf = _bufs[1 - _bufIndex];
                    _br.BaseStream.Position = _position + BufSize;
                    _br.Read(otherBuf, 0, BufSize);
                }
                return buf[offset];
            }
        }
    }
}
