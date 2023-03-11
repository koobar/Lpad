using LibLpad.Streams;
using System;
using System.IO;
using System.Threading.Tasks;
using static LibLpad.Codec.Lpad;

namespace LibLpad.Codec
{
    internal class LpadEncoder
    {
        // 非公開フィールド
        private readonly BinaryWriter OutputStream;

        #region コンストラクタ

        public LpadEncoder(BinaryWriter stream)
        {
            this.OutputStream = stream;
        }

        #endregion

        /// <summary>
        /// 指定されたサンプル配列をチャンネルごとに分割して返す。
        /// </summary>
        /// <param name="samples">サンプル配列</param>
        /// <param name="channels">チャンネル数</param>
        /// <returns>各チャンネルごとに分離されたサンプルの配列</returns>
        private short[][] SplitChannels(short[] samples, int channels)
        {
            var channelSize = samples.Length / channels;
            var separatedChannels = new short[channels][];
            var offsets = new int[channels];

            for (int i = 0; i < channels; ++i)
            {
                separatedChannels[i] = new short[channelSize];
            }

            for (int i = 0; i < samples.Length; i += channels)
            {
                for (int j = 0; j < channels; ++j)
                {
                    separatedChannels[j][offsets[j]++] = samples[i + j];
                }
            }

            return separatedChannels;
        }

        /// <summary>
        /// 指定されたサンプル数のサンプルを指定されたサイズのブロックに分割した際のブロック数を求める。
        /// </summary>
        /// <param name="numOfSamples">サンプル数</param>
        /// <param name="blockSize">1ブロックのサイズ</param>
        /// <returns></returns>
        private int ComputeNumBlocks(int numOfSamples, int blockSize)
        {
            var numBlocks = numOfSamples / blockSize;
            if (numOfSamples % blockSize > 0)
            {
                numBlocks += 1;
            }

            return numBlocks;
        }

        /// <summary>
        /// 指定されたサンプル配列の指定された位置から、ブロックの読み込み用バッファを満たす数のサンプルを読み込み、ブロックとして配列に格納する。
        /// </summary>
        /// <param name="samples">1チャンネル分のサンプル配列</param>
        /// <param name="offset">読み込み開始位置</param>
        /// <param name="block">ブロックの読み込み用バッファ</param>
        private void ReadBlock(short[] samples, ref int offset, ref short[] block)
        {
            for (uint i = 0; i < block.Length&& offset != samples.Length - 1; ++i)
            {
                block[i] = samples[offset++];
            }
        }

        /// <summary>
        /// 指定されたテーブルとインデックスを基に、与えられた予測残差を量子化する。
        /// また、エンコード時の予測器の更新に必要となる、逆量子化された予測残差を計算する。
        /// </summary>
        /// <param name="stepSizeTable">ステップサイズテーブル</param>
        /// <param name="indexTable">インデックス変化量テーブル</param>
        /// <param name="currentStepIndex">現在のステップサイズ参照用インデックス</param>
        /// <param name="scale">スケール</param>
        /// <param name="residual">量子化する予測残差</param>
        /// <returns>量子化された予測残差</returns>
        private int QuantizeResidual(int[] stepSizeTable, int[] indexTable, ref int currentStepIndex, int scale, int residual)
        {
            int minimumError = int.MaxValue;
            int defaultStepIndex = currentStepIndex;
            int quantizedResidual = 0;
            int absoluteResidual = Math.Abs(residual);

            // 0から15の範囲で表現されるスケールを、1から16の範囲での表現に変換
            scale += 1;

            // 力ずくで最も量子化誤差が小さくなる量子化予測残差を求める。
            for (int q = 0; q < indexTable.Length; q++)
            {
                // 試しにqを量子化された予測残差の絶対値とみなし、逆量子化する。
                int change = indexTable[q] * scale;
                int index = MathEx.Clamp(defaultStepIndex + change, 0, stepSizeTable.Length - 1);
                int dequantizedAbsoluteResidual = stepSizeTable[index];

                // 実際の予測残差と、逆量子化予測残差の差の絶対値を求める。
                int error = Math.Abs(dequantizedAbsoluteResidual - absoluteResidual);

                // 差が小さくなったか？
                if (error < minimumError)
                {
                    minimumError = error;
                    quantizedResidual = q;
                    currentStepIndex = index;
                }

                // 誤差がゼロならループを抜ける。
                if (error == 0) break;
            }

            // 符号情報を付加
            int b = residual < 0 ? indexTable.Length : 0;
            quantizedResidual += b;

            return quantizedResidual;
        }

        /// <summary>
        /// 量子化された予測残差を逆量子化する。
        /// </summary>
        /// <param name="stepSizeTable">ステップサイズテーブル</param>
        /// <param name="indexTable">インデックス変化量テーブル</param>
        /// <param name="currentStepIndex">現在のステップサイズ参照用インデックス</param>
        /// <param name="scale">スケール</param>
        /// <param name="quantizedResidual">量子化された予測残差</param>
        /// <returns>逆量子化された予測残差</returns>
        private int DequantizeResidual(int[] stepSizeTable, int[] indexTable, int currentStepIndex, int scale, int quantizedResidual)
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

            // ステップサイズを取得
            int dequantizedAbsoluteResidual = stepSizeTable[currentStepIndex];

            // ステップサイズに符号を反映し、逆量子化予測残差を求める。
            return sign * dequantizedAbsoluteResidual;
        }

        /// <summary>
        /// 指定されたブロックに最適なスケールを求める。
        /// </summary>
        /// <param name="lmsFilter">LMSフィルタ</param>
        /// <param name="stepSizeTable">ステップサイズテーブル</param>
        /// <param name="indexTable">インデックス変化量テーブル</param>
        /// <param name="block">エンコード対象のブロック</param>
        /// <param name="currentStepIndex">現在のステップサイズテーブル参照用インデックス</param>
        /// <param name="status">LMSフィルタの状態を保持するための変数</param>
        /// <returns>指定されたブロックに最も最適なスケール</returns>
        private int ComputeBestScale(Lms lmsFilter, int[] stepSizeTable, int[] indexTable, short[] block, int currentStepIndex, Lms.LmsStatus status)
        {
            int minimumTotalError = int.MaxValue;
            int defaultStepIndex = currentStepIndex;
            int bestScale = 0;
            status.LoadFrom(lmsFilter);

            // 全てのスケールでエンコードし、デコード結果と本来のサンプルの誤差が最小になるスケールを求める。
            for (int scale = SCALE_MIN; scale <= SCALE_MAX; ++scale)
            {
                int totalError = 0;

                for (int i = 0; i < block.Length; ++i)
                {
                    int sample = block[i];
                    int predicted = lmsFilter.Predict();
                    int residual = sample - predicted;
                    int index = currentStepIndex;
                    int quantizedResidual = QuantizeResidual(stepSizeTable, indexTable, ref currentStepIndex, scale, residual);
                    int dequantizedResidual = DequantizeResidual(stepSizeTable, indexTable, index, scale, quantizedResidual);

                    // 予測サンプルに逆量子化した予測残差を加算して出力サンプルを求め、
                    // 次のサンプルの予測のため、出力サンプルと逆量子化済み予測残差を以て、予測器の更新を行う。
                    int reconstructed = MathEx.Clamp(predicted + dequantizedResidual, short.MinValue, short.MaxValue);
                    lmsFilter.Update(reconstructed, dequantizedResidual);

                    // 誤差を加算
                    totalError += Math.Abs(sample - reconstructed);

                    // 誤差の合計が最小にならないことが確定した時点で、評価中のスケールの評価を終了する。
                    if (totalError > minimumTotalError)
                    {
                        break;
                    }
                }

                // インデックスを復元
                currentStepIndex = defaultStepIndex;

                // これまでのスケールの予測残差の誤差の合計よりも、
                // 評価中のスケールの予測残差の誤差の合計の方が小さい場合、
                // それは評価中のスケールの方が高い
                if (totalError < minimumTotalError)
                {
                    minimumTotalError = totalError;
                    bestScale = scale;
                }

                // 予測器の状態を復元
                lmsFilter.SetLmsStatus(status);

                // もし、誤差がゼロになった場合は終了する。
                if (totalError == 0) break;
            }

            return bestScale;
        }

        /// <summary>
        /// 指定されたブロックに最適な量子化ビット数を、2ビットから5ビットの範囲内で選択する。
        /// </summary>
        /// <param name="block">ブロック</param>
        /// <returns>指定されたブロックに最適な量子化ビット数</returns>
        private int ComputeBestBitsPerSample(short[] block, int srcSampleRate)
        {
            int b2 = 0;         // 2ビットで表現できそうな予測残差の個数
            int b3 = 0;         // 3ビットで表現できそうな予測残差の個数
            int b4 = 0;         // 4ビットで表現できそうな予測残差の個数
            int b5 = 0;         // 5ビットで表現できそうな予測残差の個数

            for (int i = 1; i < block.Length; ++i)
            {
                int absDiff = Math.Abs(block[i - 1] - block[i]);

                if (absDiff < 128)              // 極端に細かい変化の場合、2ビットでもある程度正確に表現可能
                {
                    b2 += 1;
                }
                else if (absDiff <= 4096)       // 3ビットだとこれくらいが限度？
                {
                    b3 += 1;
                }
                else if (absDiff <= 8192)       // 4ビットだとこれくらいが限度？
                {
                    b4 += 1;
                }
                else
                {
                    b5 += 1;
                }
            }

            int max = MathEx.Max(b2, b3, b4, b5);
            int result;

            if (max == b2)
            {
                result = 2;
            }
            else if (max == b3)
            {
                result = 3;
            }
            else if (max == b4)
            {
                result = 4;
            }
            else
            {
                result = 5;
            }

            // サンプルレートが高ければ、サンプルあたりの変化量は小さくなることが見込まれるため、
            // 量子化ビット数を落としても、ある程度の品質が確保されると考えられる。
            // そこで、サンプルレートが96Khz以上である場合には、求めた量子化ビット数を2減らす。
            int a = srcSampleRate <= 96000 ? 0 : -2;
            return MathEx.Clamp(result - a, 2, 5);
        }

        /// <summary>
        /// 指定されたブロックをエンコードする。
        /// </summary>
        /// <param name="bitStream"></param>
        /// <param name="lmsFilter"></param>
        /// <param name="block"></param>
        /// <param name="currentStepIndex"></param>
        private void EncodeBlock(BitStream bitStream, Lms lmsFilter, short[] block, int sampleRate, int dstBitsPerSample, ref int currentStepIndex, Lms.LmsStatus status)
        {
            if (dstBitsPerSample == BITS_PER_SAMPLE_VARIABLE)
            {
                dstBitsPerSample = ComputeBestBitsPerSample(block, sampleRate);
            }

            int[] stepSizeTable = GetStepSizeTable(dstBitsPerSample);
            int[] indexTable = GetIndexTable(dstBitsPerSample);
            int scale = ComputeBestScale(lmsFilter, stepSizeTable, indexTable, block, currentStepIndex, status);

            // スケールを書き込む。
            bitStream.WriteUInt(scale, BITS_OF_SCALE);

            // サンプルの量子化ビット数を書き込む。
            bitStream.WriteUInt(BitsDepthToBitsID(dstBitsPerSample), BITS_OF_BITS_PER_SAMPLE);

            // ブロックのすべてのサンプルをエンコード
            foreach (var sample in block)
            {
                int predicted = lmsFilter.Predict();
                int residual = sample - predicted;

                // 予測残差を量子化し、さらに逆量子化して、量子化された予測残差と、逆量子化された予測残差を取得する。
                int index = currentStepIndex;
                int quantizedResidual = QuantizeResidual(stepSizeTable, indexTable, ref currentStepIndex, scale, residual);
                int dequantizedResidual = DequantizeResidual(stepSizeTable, indexTable, index, scale, quantizedResidual);

                // 予測サンプルに逆量子化した予測残差を加算して出力サンプルを求め、
                // 次のサンプルの予測のため、出力サンプルと逆量子化済み予測残差を以て、予測器の更新を行う。
                int reconstructed = MathEx.Clamp(predicted + dequantizedResidual, short.MinValue, short.MaxValue);
                lmsFilter.Update(reconstructed, dequantizedResidual);

                // ストリームに書き込む。
                bitStream.WriteUInt(quantizedResidual, dstBitsPerSample);
            }
        }

        /// <summary>
        /// 与えられた1チャンネル分のサンプル配列をエンコードする。
        /// </summary>
        /// <param name="samples">1チャンネル分のサンプル配列</param>
        /// /// <param name="numBlocks"?1チャンネルあたりのブロック数
        /// <returns>エンコードされた1チャンネル分のサンプルのバイナリデータ</returns>
        private byte[] EncodeChannel(short[] samples, int srcSampleRate, int dstBitsPerSample, int blockSize, int numBlocks)
        {
            var result = new MemoryStream();
            var bitStream = new BitStream(result);
            int blockReadOffset = 0;
            var block = new short[blockSize];
            var lmsFilter = new Lms();
            var status = new Lms.LmsStatus();
            int currentStepIndex = 0;

            // 全てのブロックをエンコード
            for (int i = 0; i < numBlocks; ++i)
            {
                ReadBlock(samples, ref blockReadOffset, ref block);
                EncodeBlock(bitStream, lmsFilter, block, srcSampleRate, dstBitsPerSample, ref currentStepIndex, status);
            }

            // 書き込みの終了
            bitStream.EndWrite();

            return result.ToArray();
        }

        /// <summary>
        /// 与えられたサンプル配列をエンコードする。
        /// </summary>
        /// <param name="samples">複数チャンネルのサンプルを含むサンプル配列</param>
        public void Encode(short[] samples, int srcSampleRate, int dstBitsPerSample, int numChannels, int blockSize)
        {
            var channels = SplitChannels(samples, numChannels);
            int numBlocks = ComputeNumBlocks(channels[0].Length, blockSize);

            // チャンネルごとのエンコードタスクを生成。
            var tasks = new Task<byte[]>[numChannels];
            for (int i = 0; i < numChannels; ++i)
            {
                int a = i;
                tasks[a] = Task.Factory.StartNew(() => EncodeChannel(channels[a], srcSampleRate, dstBitsPerSample, blockSize, numBlocks));
            }

            // 全てのチャンネルのエンコードが終了するまで待機
            Task.WaitAll(tasks);

            // 1チャンネルあたりのブロック数を書き込む。
            this.OutputStream.Write(numBlocks);

            // エンコード済みのチャンネルデータをストリームに書き込む。
            for (int i = 0; i < tasks.Length; ++i)
            {
                this.OutputStream.Write(tasks[i].Result.Length);
                this.OutputStream.Write(tasks[i].Result);
            }
        }
    }
}
