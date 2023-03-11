using LibLpad.Streams;
using System.IO;
using System.Threading.Tasks;
using static LibLpad.Codec.Lpad;

namespace LibLpad.Codec
{
    internal class LpadDecoder
    {
        // 非公開フィールド
        private readonly BinaryReader InputStream;

        #region コンストラクタ

        public LpadDecoder(BinaryReader stream)
        {
            this.InputStream = stream;
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// 複数のスレッドを使用してデコードするかどうか
        /// </summary>
        public bool DecodeWithMultithread { set; get; }

        #endregion

        /// <summary>
        /// 量子化された予測残差を逆量子化する。
        /// </summary>
        /// <param name="stepSizeTable">ステップサイズテーブル</param>
        /// <param name="indexTable">インデックス変化量テーブル</param>
        /// <param name="currentStepIndex">現在のステップサイズ参照用インデックス</param>
        /// <param name="scale">スケール</param>
        /// <param name="quantizedResidual">量子化された予測残差</param>
        /// <returns>逆量子化された予測残差</returns>
        private int DequantizeResidual(int[] stepSizeTable, int[] indexTable, ref int currentStepIndex, int scale, int quantizedResidual)
        {
            // 0から15の範囲で表現されるスケールを、1から16の範囲での表現に変換
            scale += 1;

            // 符号情報（ステップサイズを加算するのか、減算するのか）を取得する。
            int a = indexTable.Length;
            int sign = quantizedResidual >= a ? -1 : 1;

            // インデックス変化量テーブルを参照するためのインデックスを取得する。
            int index = sign == 1 ? quantizedResidual : quantizedResidual - a;

            // ステップサイズテーブルの参照用インデックスを求める
            int change = indexTable[index] * scale;
            currentStepIndex = MathEx.Clamp(currentStepIndex + change, 0, stepSizeTable.Length - 1);

            // ステップサイズテーブルからステップサイズを参照し、符号を反映したものを逆量子化された予測残差として返す。
            return sign * stepSizeTable[currentStepIndex];
        }

        /// <summary>
        /// 指定された予測器、テーブル、インデックスを基に、指定された量子化予測残差を逆量子化し、
        /// 予測器から予測したサンプルに予測残差を加算してサンプルを求める。
        /// </summary>
        /// <param name="lmsFilter"></param>
        /// <param name="indexTable">インデックス変化量テーブル</param>
        /// <param name="scale"></param>
        /// <param name="currentStepIndex"></param>
        /// <param name="quantizedResidual"></param>
        /// <returns>デコードされたサンプル(16ビットPCM)</returns>
        private short DecodeSample(Lms lmsFilter, int[] indexTable, ref int currentStepIndex, int scale, int quantizedResidual)
        {
            // 予測残差を逆量子化
            int dequantizedResidual = DequantizeResidual(StepSizeTable, indexTable, ref currentStepIndex, scale, quantizedResidual);

            // 予測サンプルに逆量子化された予測残差を加算し、出力サンプルを求める。
            int predicted = lmsFilter.Predict();
            int reconstructed = MathEx.Clamp(predicted + dequantizedResidual, short.MinValue, short.MaxValue);

            // 予測器を更新
            lmsFilter.Update(reconstructed, dequantizedResidual);
            return (short)reconstructed;
        }

        /// <summary>
        /// 指定されたビットストリームの位置から、次のブロックを読み込んでデコードする。
        /// </summary>
        /// <param name="bitStream"></param>
        /// <param name="lmsFilter"></param>
        /// <param name="currentStepIndex"></param>
        /// <param name="blockSize"></param>
        /// <param name="result"></param>
        private void ReadBlock(BitStream bitStream, Lms lmsFilter, ref int currentStepIndex, int blockSize, short[] result)
        {
            int scale = bitStream.ReadUInt(BITS_OF_SCALE);
            int bitsPerSample = BitsIDToBitsDepth(bitStream.ReadUInt(BITS_OF_BITS_PER_SAMPLE));
            int[] indexTable = GetIndexTable(bitsPerSample);

            // サンプルを読み込む。
            for (uint i = 0; i < blockSize; ++i)
            {
                int quantizedResidual = bitStream.ReadUInt(bitsPerSample);
                result[i] = DecodeSample(lmsFilter, indexTable, ref currentStepIndex, scale, quantizedResidual);
            }
        }

        /// <summary>
        /// ストリームの現在の位置から、次の1チャンネル分のデータを読み込んでデコードし、16ビットPCMとして返す。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        private void ReadChannel(byte[] data, int numChannels, int channel, int numBlocks, int blockSize, short[] output)
        {
            var mem = new MemoryStream(data);
            var bitStream = new BitStream(mem);
            var lmsFilter = new Lms();
            int currentStepIndex = 0;
            var outputOffset = channel;
            var decodedBlock = new short[blockSize];

            // 全てのブロックを読み込んでデコードする。
            for (int i = 0; i < numBlocks; ++i)
            {
                // ブロックを読み込む。
                ReadBlock(bitStream, lmsFilter, ref currentStepIndex, blockSize, decodedBlock);

                for (int blockOffset = 0; blockOffset < blockSize; ++blockOffset)
                {
                    output[outputOffset] = decodedBlock[blockOffset];
                    outputOffset += numChannels;
                }
            }
        }

        /// <summary>
        /// 複数のスレッドを使用して高速なデコードを行う。
        /// </summary>
        /// <param name="numChannels"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        private short[] DecodeWithMultiThread(int numChannels, int blockSize)
        {
            // 1チャンネルあたりのブロック数を読み込む。
            int numBlocks = this.InputStream.ReadInt32();

            // デコード結果用の領域を確保
            short[] result = new short[numBlocks * blockSize * numChannels];

            // 各チャンネルをデコードするタスクを生成
            var tasks = new Task[numChannels];
            for (int c = 0; c < numChannels; ++c)
            {
                int channel = c;
                byte[] data = this.InputStream.ReadBytes(this.InputStream.ReadInt32());

                tasks[channel] = Task.Factory.StartNew(() => ReadChannel(data, numChannels, channel, numBlocks, blockSize, result));
            }

            Task.WaitAll(tasks);

            return result;
        }

        /// <summary>
        /// シングルスレッドでデコードを行う。
        /// </summary>
        /// <param name="numChannels"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        private short[] DecodeWithSingleThread(int numChannels, int blockSize)
        {
            // 1チャンネルあたりのブロック数を読み込む。
            int numBlocks = this.InputStream.ReadInt32();

            // デコード結果用の領域を確保
            short[] result = new short[numBlocks * blockSize * numChannels];

            // 各チャンネルをデコード
            for (int channel = 0; channel < numChannels; ++channel)
            {
                byte[] data = this.InputStream.ReadBytes(this.InputStream.ReadInt32());
                ReadChannel(data, numChannels, channel, numBlocks, blockSize, result);
            }

            return result;
        }

        /// <summary>
        /// ストリームからデータを読み込んでデコードし、16ビットPCMとして返す。
        /// </summary>
        /// <returns>16ビットで量子化されたPCM</returns>
        public short[] Decode(int numChannels, int blockSize)
        {
            if (this.DecodeWithMultithread)
            {
                return DecodeWithMultiThread(numChannels, blockSize);
            }

            return DecodeWithSingleThread(numChannels, blockSize);
        }
    }
}
