namespace Mp3Joiner
{
    /// <summary>
    ///  Well, MP3 as such a crappy format deserves this name and the most
    ///  readable and accessible doc is suprisingly the code below written
    ///  in, take a breath, PHP
    /// </summary>
    /// <remarks>
    ///  http://www.zedwood.com/article/php-calculate-duration-of-mp3
    ///  later this came up:
    ///  http://www.mp3-tech.org/programmer/frame_header.html
    /// </remarks>
    public class Mp3Fcuker
    {
        public enum Versions
        {
            V2p5 =0,// 2.5
            X,      // x (reserved)
            V2,     // 2
            V1,     // 1
        }

        public enum Layers
        {
            X=0,    // x (reserved)
            V3,     // 3
            V2,     // 2
            V1,     // 1
        }
        
        public enum ChannelModes
        {
            Stereo = 0,
            JointStereo,
            DualChannel,
            SingleChannel
        }

        public enum ChannelModeExtensions
        {
            // Layer 1 & 2
            Bands4To31,
            Bands8To31,
            Bands12To31,
            Bands16To31,
            BothOff,
            IntensityStereoOn,
            MsStereoOn,
            BothOn,
            NotApplicable
        }

        public enum Emphases
        {
            None = 0,
            FiftyFifteenMs, // 50/15 ms
            Reserved,
            CCITJ17,    // CCITJ17
        }


        public class HeaderInfo
        {
            public Versions Version;
            public Layers Layer;
            public int BitRate;
            public int SampleRate;
            public bool Padded;
            public bool PrivateBit;
            public ChannelModes ChannelMode;
            public ChannelModeExtensions ChannelModeExtension;
            public bool Copyrighted;
            public bool CopyOfOriginal;
            public Emphases Emphasis;
            public int SampleNumber;
            public int EstimatedFrameSize;
        }

        public static readonly int[][] BitRateLookup =
        {
            new []{0,32,64,96,128,160,192,224,256,288,320,352,384,416,448,-1 }, //V1L1
            new []{0,32,48,56, 64, 80, 96,112,128,160,192,224,256,320,384,-1 }, //V1L2
            new [] {0,32,40,48, 56, 64, 80, 96,112,128,160,192,224,256,320,-1 }, //V1L3
            new [] {0,32,48,56, 64, 80, 96,112,128,144,160,176,192,224,256,-1 }, //V2L1
            new [] {0, 8,16,24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,-1 }, //V2L2
            new [] { 0, 8,16,24, 32, 40, 48, 56, 64, 80, 96,112,128,144,160,-1}, //V2L3
        };

        public static readonly int[][] SampleRateLookup =
        {
            new[] { 44100, 48000, 32000 }, // V1
            new[] { 22050,24000,16000 }, // V2
            new[] { 11025,12000, 8000 }, // V2.5
        };

        public static readonly int[][] SampleNumberLookup =
        {
            new[] { 384, 1152, 1152 }, // MPEGv1, Layer1,2,3
            new[] { 384, 1152, 576 }, // MPEGv2/2.5, Layer 1,2,3
        };

        public static void ParseHeader(byte[] header, HeaderInfo info)
        {
            ParseHeader(header, out info.Version, out info.Layer,
                out info.BitRate, out info.SampleRate, out info.Padded,
                out info.PrivateBit, out info.ChannelMode, out info.ChannelModeExtension,
                out info.Copyrighted, out info.CopyOfOriginal, out info.Emphasis,
                out info.SampleNumber, out info.EstimatedFrameSize);
        }

        public static void ParseHeader(byte[] header, 
            out Versions version,
            out Layers layer,
            out int bitrate, 
            out int sampleRate, 
            out bool padded, out bool privateBit,
            out ChannelModes channelMode, 
            out ChannelModeExtensions channelModeExtension,
            out bool copyrighted, out bool copyOfOriginal,
            out Emphases emphasis,
            out int samples,
            out int estimatedFrameSize)
        {
            var versionBits = (header[1] & 0x18) >> 3;
            var layerBits = (header[1] & 0x6) >> 1;
            var protectionBit = header[1] & 0x1;

            version = (Versions)versionBits;
            layer = (Layers)layerBits;

            var layerIdx = GetLayerIdx(layer);
            var simpleVersion = GetSimpleVersion(version);
            var bitRateKey = GetBitRateKey(simpleVersion, layerIdx);
            if (bitRateKey >= 0)
            {
                var bitRateIdx = (header[2] & 0xf0) >> 4;
                bitrate = BitRateLookup[bitRateKey][bitRateIdx];
            }
            else
            {
                bitrate = 0;// not available
            }

            var sampleRateBits = (header[2] & 0x0c) >> 2;
            var versionIdx = GetVersionIdx(version);
            if (sampleRateBits < 3 && versionIdx >= 0)
            {
                sampleRate = SampleRateLookup[versionIdx][sampleRateBits];
            }
            else
            {
                sampleRate = 0;// not available
            }

            var paddingBit = (header[2] & 0x02) >> 1;
            padded = paddingBit != 0;
            privateBit = (header[2] & 0x01) != 0;

            var channelModeBits = (header[3] & 0xc0) >> 6;
            channelMode = (ChannelModes)channelModeBits;

            channelModeExtension = ChannelModeExtensions.NotApplicable;
            if (channelMode == ChannelModes.JointStereo)
            {
                var modeExtensionBits = (header[3] & 0x30) >> 4;

                if (layer == Layers.V1 || layer == Layers.V2)
                {
                    channelModeExtension = (ChannelModeExtensions)modeExtensionBits;
                }
                else if (layer == Layers.V3)
                {
                    channelModeExtension = (ChannelModeExtensions)(modeExtensionBits+4);
                }
            }

            var copyrightBit = (header[3] & 0x08) >> 3;
            copyrighted = copyrightBit != 0;
            var originalBit = (header[3] & 0x04) >> 2;
            copyOfOriginal = originalBit != 0;
            var emphasisBits = (header[3] & 0x03);
            emphasis = (Emphases)emphasisBits;

            if (layerIdx >= 0 && simpleVersion >= 0)
            {
                samples = SampleNumberLookup[simpleVersion][layerIdx];
            }
            else
            {
                samples = 0;// not available
            }

            estimatedFrameSize = FrameSize(layer, bitrate, sampleRate, paddingBit);
        }

        public static int GetVersionIdx(Versions version)
        {
            switch (version)
            {
                case Versions.V1:
                    return 0;
                case Versions.V2:
                    return 1;
                case Versions.V2p5:
                    return 2;
            }
            return -1;
        }

        public static int GetSimpleVersion(Versions version)
        {
            switch (version)
            {
                case Versions.V1:
                    return 0;
                case Versions.V2:
                case Versions.V2p5:
                    return 1;
            }
            return -1;  // invalid
        }

        public static int GetLayerIdx(Layers layer)
        {
            switch (layer)
            {
                case Layers.V1:
                    return 0;
                case Layers.V2:
                    return 1;
                case Layers.V3:
                    return 2;
            }
            return -1;  // invalid
        }

        private static int GetBitRateKey(int simpleVersion, int layerIdx)
        {
            if (simpleVersion < 0 || layerIdx < 0)
            {
                return -1;
            }
            return simpleVersion * 3 + layerIdx;
        }

        private static int FrameSize(Layers layer, int bitrate,
            int sampleRate, int paddingBit)
        {
            if (bitrate <= 0|| sampleRate <= 0)
            {
                return paddingBit;
            }
            if (layer == Layers.V2 || layer == Layers.V3)
            {
                return (int)(((144 * bitrate * 1000.0) / sampleRate) + paddingBit);
            }
            else if (layer == Layers.V1)
            {
                return (int)(((12 * bitrate * 1000.0 / sampleRate) + paddingBit) * 4);
            }
            else
            {
                return 0;// reserved, unsupported layer type
            }
        }
    }
}
