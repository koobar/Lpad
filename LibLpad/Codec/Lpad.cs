using System;

namespace LibLpad.Codec
{
    internal static class Lpad
    {
        public const int BITS_PER_SAMPLE_VARIABLE = 0;              // ブロックのサンプルの量子化ビット数を、ブロックのデータに応じて適応的に変化させる場合に、エンコーダに指定するビット数
        public const int SCALE_MIN = 0;                             // ブロックのスケールの最小値
        public const int SCALE_MAX = 15;                            // ブロックのスケールの最大値
        public const int BITS_OF_SCALE = 4;                         // ブロックのスケールを示す値のビット数
        public const int BITS_OF_BITS_PER_SAMPLE = 3;               // ブロックのサンプルの量子化ビット数を示す値のビット数

        private static readonly int[] indexTable2Bits = new int[2]
        {
            -1, 3
        };
        private static readonly int[] indexTable3Bits = new int[4]
        {
            -3, -1, 2, 4
        };
        private static readonly int[] indexTable4Bits = new int[8]
        {
            -5, -4, -2, -1, 1, 2, 4, 6
        };
        private static readonly int[] indexTable5Bits = new int[16]
        {
            -7, -6, -5, -4, -3, -2, -1, 0,
            1, 2, 3, 4, 5, 6, 7, 8
        };
        private static readonly int[] indexTable6Bits = new int[32]
        {
            -16, -15, -14, -13, -12, -11, -10, -9,
            -7, -6, -5, -4, -3, -2, -1, 0,
            1, 2, 3, 4, 5, 6, 7, 8,
            9, 10, 11, 12, 13, 14, 15, 16
        };
        private static readonly int[] indexTable7Bits = CreateHighBitsIndexTable(64);
        private static readonly int[] indexTable8Bits = CreateHighBitsIndexTable(128);
        private static readonly int[] indexTable9Bits = CreateHighBitsIndexTable(256);
        public static readonly int[] StepSizeTable = CreateStepSizeTable();

        /// <summary>
        /// ステップサイズテーブルを生成する。
        /// </summary>
        /// <returns></returns>
        private static int[] CreateStepSizeTable()
        {
            int[] result = new int[256];
            result[0] = 1;

            for (int i = 1; i < result.Length; ++i)
            {
                result[i] = (int)(result[i - 1] + ((result[i - 1] * 0.02954) + 1));
                result[i] = MathEx.Clamp(result[i], 0, 32767);
            }

            return result;
        }

        /// <summary>
        /// 高ビット用のインデックス変化量テーブルを生成する。
        /// </summary>
        /// <param name="size">テーブルサイズ</param>
        /// <returns></returns>
        private static int[] CreateHighBitsIndexTable(int size)
        {
            int[] result = new int[size];
            int k = size / 2;

            for (int i = 0; i < size; ++i)
            {
                result[i] = i - k;
            }

            return result;
        }

        /// <summary>
        /// ビット数をビット数IDに変換する。
        /// </summary>
        /// <param name="bitsDepth"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int BitsDepthToBitsID(int bitsDepth)
        {
            return bitsDepth - 2;
        }

        /// <summary>
        /// ビット数IDをビット数に変換する。
        /// </summary>
        /// <param name="bitsID"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int BitsIDToBitsDepth(int bitsID)
        {
            return bitsID + 2;
        }

        /// <summary>
        /// 指定された量子化ビット数のインデックス調整テーブルを取得する。
        /// </summary>
        /// <param name="bitsPerResidual"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int[] GetIndexTable(int bitsPerResidual)
        {
            switch (bitsPerResidual)
            {
                case 2:
                    return indexTable2Bits;
                case 3:
                    return indexTable3Bits;
                case 4:
                    return indexTable4Bits;
                case 5:
                    return indexTable5Bits;
                case 6:
                    return indexTable6Bits;
                case 7:
                    return indexTable7Bits;
                case 8:
                    return indexTable8Bits;
                case 9:
                    return indexTable9Bits;
                default:
                    throw new Exception("Unsupported bits depth.");
            }
        }

        /// <summary>
        /// 指定された量子化ビット数の調整テーブルのサイズを取得する。
        /// </summary>
        /// <param name="bitsPerResidual"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int GetSizeOfAdjustTable(int bitsPerResidual)
        {
            return 2 ^ (bitsPerResidual - 1);
        }
    }
}
