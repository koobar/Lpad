using System;

namespace LibLpad
{
    public static class CodecInformation
    {
        /// <summary>
        /// フォーマットのバージョン
        /// </summary>
        public static Version FormatVersion
        {
            get
            {
                return new Version(0, 2);
            }
        }

        /// <summary>
        /// エンコーダ/デコーダ（このライブラリ）のバージョン
        /// </summary>
        public static Version CodecVersion
        {
            get
            {
                return new Version(0, 2);
            }
        }

        /// <summary>
        /// このエンコーダ/デコーダが対応するLPADのバージョンID
        /// </summary>
        public static byte FormatVersionId = 0x01;

        /// <summary>
        /// エンコーダ/デコーダ（このライブラリ）のバージョンID
        /// </summary>
        public static byte CodecVersionId = 0x01;
    }
}
