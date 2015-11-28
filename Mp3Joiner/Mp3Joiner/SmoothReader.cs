using System.IO;

namespace Mp3Joiner
{
    public class SmoothReader
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

        private int _bufIndex;

        private long _position;
        
        public SmoothReader(BinaryReader br)
        {
            BaseReader = br;
            _bufIndex = 0;
            BaseReader.BaseStream.Position = 0;
            _position = 0;
            FileLength = BaseReader.BaseStream.Length;
        }

        public BinaryReader BaseReader { get; private set; }

        public long FileLength { get; private set; }

        public void Initialise()
        {
            var buf = _bufs[_bufIndex];
            BaseReader.Read(buf, 0, BufSize);
        }

        public byte GetAt(long at)
        {
            var offset = at - _position;
            byte[] buf;
            if (offset < 0) // read backwards
            {
                var bi = 1 - _bufIndex;
                buf = _bufs[bi];
                offset += BufSize;
                if (offset < LowerThr)
                {
                    var otherBuf = _bufs[_bufIndex];
                    // update pointers
                    _position -= BufSize*2;
                    BaseReader.BaseStream.Position = _position;
                    _bufIndex = 1 - _bufIndex;
                    // read BufSize at _position
                    BaseReader.Read(otherBuf, 0, BufSize);
                }
            }
            else // read forwards (normal)
            {
                buf = _bufs[_bufIndex];
                if (offset > UpperThr)
                {
                    var otherBuf = _bufs[1 - _bufIndex];
                    // update 
                    _position += BufSize;
                    BaseReader.BaseStream.Position = _position;
                    _bufIndex = 1 - _bufIndex;
                    // read BufSize at _position
                    BaseReader.Read(otherBuf, 0, BufSize);
                }
            }
            return buf[offset];
        }
    }
}
