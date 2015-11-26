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
    }
}
