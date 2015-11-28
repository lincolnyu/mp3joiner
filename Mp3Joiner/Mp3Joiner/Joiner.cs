using System;
using System.Collections.Generic;
using System.IO;

namespace Mp3Joiner
{
    public class Joiner
    {
        // NOTE we assume its Xing and the field offsets are pre-determined and fixed
        const long SamplesOffset = 0x2c;
        const long LengthOffset = 0x30;

        // TAG occurs in this region at the end of the file is regarded as ID3 tag and discarded no matter what
        // normally TAG occurs around end-8*16
        // if this presents a problem, we may adjust the value or revise the strategy
        const int TagPossibleRegion = 8 * 16 + 4 * 16;

        public void JoinTrivial(IEnumerable<string> inputFileNames, string outputFileName)
        {
            const int bufferSize = 4096;
            var buf = new byte[bufferSize];

            using (var fsOut = new FileStream(outputFileName, FileMode.Create))
            using (var bw = new BinaryWriter(fsOut))
            {
                foreach (var fileName in inputFileNames)
                {
                    if (fileName.ToLower() == outputFileName.ToLower())
                    {
                        continue;
                    }
                    using (var fs = new FileStream(fileName, FileMode.Open))
                    using (var br = new BinaryReader(fs))
                    {
                        int read;
                        do
                        {
                            read = br.Read(buf, 0, bufferSize);
                            bw.Write(buf, 0, read);
                        } while (read == bufferSize);
                    }
                }
            }
        }

        public void Join(IList<string> inputFileNames, string outputFileName)
        {
            using (var fsOut = new FileStream(outputFileName, FileMode.Create))
            using (var bw = new BinaryWriter(fsOut))
            {
                uint totalSamples = 0;
                uint totalLength = 0;
                for (var i = 0; i< inputFileNames.Count; i++)
                {
                    var fileName = inputFileNames[i];
                    if (fileName.ToLower() == outputFileName.ToLower())
                    {
                        continue;
                    }
                    using (var fs = new FileStream(fileName, FileMode.Open))
                    using (var br = new BinaryReader(fs))
                    {
                        var isFirst = i == 0;
                        uint length, samples;
                        CopyMp3(br, bw, isFirst, out length, out samples);
                        totalSamples += samples;
                        totalLength += length;
                    }
                }
                bw.BaseStream.Position = SamplesOffset;
                bw.Write(SetUInt(totalSamples));
                bw.BaseStream.Position = LengthOffset;
                bw.Write(SetUInt(totalLength));
            }
        }

        private void CopyMp3(BinaryReader br, BinaryWriter bw, bool writeInfoFrame,
            out uint length, out uint samples)
        {
            var sr = new SmoothReader(br);
            var len = sr.FileLength;
            sr.Initialise();

            int i = 0;
            var obw = writeInfoFrame ? bw : null;
            var headerFound = FindAndProcessFirstHeader(sr, ref i, len, obw, null);
            if (!headerFound)
            {
                throw new FormatException("MP3 frame header not found");
            }

            ProcessFirstFrame(sr, ref i, len, obw, out length, out samples);
            
            while(AdvanceToBeforeNextHeader(sr, ref i, len, bw)) { }
        }

        private static bool FindAndProcessFirstHeader(SmoothReader sr, ref int i, long len, 
            BinaryWriter bw, int? estimatedSize)
        {
            var headerFound = false;
            for (; !headerFound && i < len; i++)
            {
                var b = sr.GetAt(i);
                if (b == 0xff)
                {
                    if (i < len-3)
                    {
                        var b2 = sr.GetAt(++i);
                        if ((b2 >> 5) == 0x07 && b2 != 0xff)
                        {
                            // synced, frame header
                            var b3 = sr.GetAt(++i);
                            var b4 = sr.GetAt(++i);
                            var header = new byte[]
                            {
                                b,
                                b2,
                                b3,
                                b4
                            };
                            if (estimatedSize != null)
                            {
                                var info = new Mp3Fcuker.HeaderInfo();
                                Mp3Fcuker.ParseHeader(header, info);
                                estimatedSize = info.EstimatedFrameSize;
                            }
                            headerFound = true;
                            if (bw != null)
                            {
                                bw.Write(header);
                            }
                        }
                    }
                }
            }
            return headerFound;
        }

        /// <summary>
        ///  <paramref name="i"></paramref> is at the end of the header of the first frame
        ///  which is assumed to be a Xing frame
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="i"></param>
        /// <param name="len"></param>
        /// <param name="bw"></param>
        /// <param name="length"></param>
        /// <param name="samples"></param>
        /// <returns></returns>
        private static bool ProcessFirstFrame(SmoothReader sr, ref int i, long len,
            BinaryWriter bw, out uint length, out uint samples)
        {
            var headerFound = false;
            var samplesOffset = i + SamplesOffset - 4;
            var lengthOffset = i + LengthOffset - 4;
            length = samples = 0;
            var start = i; // excluding the start
            for (; !headerFound && i < len; i++)
            {
                if (i == samplesOffset)
                {
                    var bytes = LoadFourBytes(sr, i);
                    samples = GetUInt(bytes);
                    if (bw != null)
                    {
                        foreach (var b in bytes)
                        {
                            bw.Write(b);
                        }
                    }
                    i += 3;
                }
                else if (i == lengthOffset)
                {
                    var bytes = LoadFourBytes(sr, i);
                    length = GetUInt(bytes);
                    if (bw != null)
                    {
                        foreach (var b in bytes)
                        {
                            bw.Write(b);
                        }
                    }
                    i += 3;
                }
                else
                {
                    var b = sr.GetAt(i);
                    if (b == 'T' && i > len - TagPossibleRegion)
                    {
                        if (i < len - 2)
                        {
                            var b2 = sr.GetAt(++i);
                            var b3 = sr.GetAt(++i);
                            if (b2 == 'A' && b3 == 'G')
                            {
                                i -= 2;
                                return false;// end of frame, TAG not to be included
                            }
                            else if (bw != null)
                            {
                                bw.Write(b);
                                bw.Write(b2);
                                bw.Write(b3);
                            }
                        }
                    }
                    else if (b == 0xff)
                    {
                        var b2 = sr.GetAt(i+1);
                        if ((b2 >> 5) == 0x07 && b2 != 0xff)
                        {
                            // another header
                            return true;
                        }
                        else if (bw != null)
                        {
                            bw.Write(b);
                        }
                    }
                    else if (bw != null)
                    {
                        bw.Write(b);
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="i"></param>
        /// <param name="len"></param>
        /// <param name="bw"></param>
        /// <returns>True if there's another frame, false if it's end of file or no more frame available</returns>
        private static bool AdvanceToBeforeNextHeader(SmoothReader sr, ref int i, long len,
            BinaryWriter bw)
        {
            var start = i;
            var headerFound = false;
            for (; !headerFound && i < len; i++)
            {
                var b = sr.GetAt(i);
                if (b == 'T' && i > len- TagPossibleRegion)
                {
                    if (i < len - 2)
                    {
                        var b2 = sr.GetAt(++i);
                        var b3 = sr.GetAt(++i);
                        if (b2 == 'A' && b3 == 'G')
                        {
                            i -= 2;
                            return false;// end of frame, TAG not to be included
                        }
                        else if (bw != null)
                        {
                            bw.Write(b);
                            bw.Write(b2);
                            bw.Write(b3);
                        }
                    }
                    else if (bw != null)
                    {
                        bw.Write(b);
                    }
                }
                else if (b == 0xff && i > start)
                {
                    var b2 = sr.GetAt(i+1);
                    if ((b2 >> 5) == 0x07 && b2 != 0xff)
                    {
                        // another header
                        return true;
                    }
                    else if (bw != null)
                    {
                        bw.Write(b);
                    }
                }
                else if (bw != null)
                {
                    bw.Write(b);
                }
            }
            return false;
        }

        private static byte[] LoadFourBytes(SmoothReader sr, int i)
        {
            var bytes = new byte[4];
            bytes[0] = sr.GetAt(i++);
            bytes[1] = sr.GetAt(i++);
            bytes[2] = sr.GetAt(i++);
            bytes[3] = sr.GetAt(i++);
            return bytes;
        }

        private static uint GetUInt(byte[] bytes)
        {
            var b0 = (uint)bytes[0];
            var b1 = (uint)bytes[1];
            var b2 = (uint)bytes[2];
            var b3 = (uint)bytes[3];

            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        private static byte[] SetUInt(uint val)
        {
            var bytes = new byte[4]
            {
                (byte)(val>>24),
                (byte)((val>>16)&0xff),
                (byte)((val>>8)&0xff),
                (byte)(val&0xff)
            };
            return bytes;
        }
    }
}
