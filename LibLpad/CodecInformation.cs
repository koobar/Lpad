using System;

namespace LibLpad
{
    public static class CodecInformation
    {
        // 公開定数
        public const byte ENCODER_VERSION_ID = 0x03;
        public const byte DECODER_VERSION_ID = 0x03;
        public const byte FORMAT_VERSION_ID = 0x03;

        /// <summary>
        /// エンコーダのバージョン
        /// </summary>
        public static Version EncoderVersion 
        {
            get
            {
                return new Version(0, 3);
            }
        }

        /// <summary>
        /// デコーダのバージョン
        /// </summary>
        public static Version DecoderVersion
        {
            get
            {
                return new Version(0, 3);
            }
        }

        /// <summary>
        /// フォーマットのバージョン
        /// </summary>
        public static Version FormatVersion
        {
            get
            {
                return new Version(0, 3);
            }
        }
    }
}
