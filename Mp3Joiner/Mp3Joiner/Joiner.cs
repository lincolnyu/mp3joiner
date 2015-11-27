using System.Collections.Generic;
using System.IO;

namespace Mp3Joiner
{
    public class Joiner
    {
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

        public void Join(IEnumerable<string> inputFileNames, string outputFileName)
        {
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
                        CopyMp3(br, bw);
                    }
                }
            }
        }

        private void CopyMp3(BinaryReader br, BinaryWriter bw)
        {
            var sr = new SmoothReader(br);
            var len = sr.FileLength;
            sr.Initialise();
            var state = 0;
            var ffcount = 0;
            var doublefivecount = 0;
            for (var i = 0L; i < len && state < 2; i++)
            {
                var b = sr.GetAt(i);
                switch (state)
                {
                    case 0:
                        switch (b)
                        {
                            case 0xff:
                                ffcount++;
                                break;
                            case 0xfb:
                                if (ffcount >= 2)
                                {
                                    // found
                                    bw.Write(new byte[] {0xff, 0xff, 0xfb});
                                    state = 1;
                                }
                                break;
                            default:
                                ffcount = 0;
                                break;
                        }
                        break;
                    case 1:
                        switch (b)
                        {
                            case 0x55:
                                doublefivecount++;
                                if (doublefivecount > 4)
                                {
                                    state = 2;
                                }
                                break;
                            default:
                                for (; doublefivecount > 0; doublefivecount--)
                                {
                                    bw.Write(0x55);
                                }
                                bw.Write(b);
                                break;
                        }
                        break;
                }
            }
        }
    }
}
