using System.IO;

namespace Mp3Joiner
{
    public class ByteReader
    {
        private const int BufSize = 4096;

        private const int UpperThr = 3072;

        private const int LowerThr = 1024;

        /// <summary>
        ///  dual buffer
        /// </summary>
        private readonly byte[][] _bufs =
        {
            new byte[BufSize],
            new byte[BufSize]
        };

        private readonly BinaryReader _br;

        private int _bufIndex;

        private long _position;
        
        public ByteReader(BinaryReader br)
        {
            _br = br;
            _bufIndex = 0;
            _br.BaseStream.Position = 0;
            _position = 0;
            FileLength = _br.BaseStream.Length;
            IniLoad();
        }

        public long FileLength { get; private set; }

        private void IniLoad()
        {
            var buf=_bufs[_bufIndex];
            _br.Read(buf, 0, BufSize);
        }

        public byte GetAt(long at)
        {
            var offset = at - _position;
            byte[] buf;
            if (offset < 0)
            {
                var bi = 1 - _bufIndex;
                buf = _bufs[bi];
                offset += BufSize;
                if (offset < LowerThr)
                {
                    var otherBuf = _bufs[_bufIndex];
                    _br.Read(otherBuf, 0, BufSize);
                    // update pointers
                    _position -= BufSize*2;
                    _br.BaseStream.Position = _position;
                    _bufIndex = 1 - _bufIndex;
                }
            }
            else
            {
                buf = _bufs[_bufIndex];
                if (offset > UpperThr)
                {
                    var otherBuf = _bufs[1 - _bufIndex];
                    _br.Read(otherBuf, 0, BufSize);
                    // update 
                    _position += BufSize;
                    _br.BaseStream.Position = _position;
                    _bufIndex = 1 - _bufIndex;
                }
            }
            return buf[offset];
        }
    }
}
