using LibLpad.Streams;
using Lpad.Wav;
using System;
using System.IO;
using System.Text;

namespace Lpad
{
    internal class Program
    {
        // 非公開定数
        private const string MES_ENTER_TO_STOP = "Press ENTER key to stop.";

        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.Unicode;

            if (args.Length == 0)
            {
                WizardMode.Start();
            }
            else
            {
                CommandLineMode.Start(args);
            }
        }

        /// <summary>
        /// エンコード
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="destFilePath"></param>
        /// <param name="bitsPerResidual"></param>
        /// <param name="blockSize"></param>
        public static void Encode(
            string srcFilePath,
            string destFilePath,
            int bitsPerResidual,
            int blockSize)
        {
            // ソースファイルを読み込んでデコードする
            var wav_decoder = new WavDecoder(srcFilePath);
            var src = wav_decoder.ReadAllSamples();

            // エンコーダを設定
            var writer = new LpadStreamWriter(File.Create(destFilePath));
            writer.BitsPerSample = bitsPerResidual;
            writer.BlockSize = blockSize;
            writer.SampleRate = (int)wav_decoder.SampleRate;
            writer.NumChannels = (int)wav_decoder.Channels;

            // エンコードしてストリームに書き込む
            writer.Write(src);

            // 後始末
            writer.Dispose();
        }

        /// <summary>
        /// デコード
        /// </summary>
        /// <param name="srcFilePath"></param>
        /// <param name="destFilePath"></param>
        /// <param name="isPlayMode"></param>
        public static void Decode(string srcFilePath, string destFilePath, bool isPlayMode)
        {
            if (isPlayMode)
            {
                PlayFile(srcFilePath);
            }
            else
            {
                // ファイルを読み込んでデコード
                var reader = new LpadStreamReader(File.OpenRead(srcFilePath));
                var decoded = reader.ReadSamples();

                // デコードして得られたPCMサンプルをWAVファイルに保存する。
                var wav_encoder = new WavEncoder(destFilePath, (uint)reader.SampleRate, 16, (uint)reader.NumChannels);
                wav_encoder.WriteSamples(decoded);

                // フォーマット情報の表示
                PrintFormatInfo(reader);

                // 後始末
                wav_encoder.Dispose();
                reader.Dispose();
            }
        }

        /// <summary>
        /// 再生
        /// </summary>
        /// <param name="srcFilePath"></param>
        public static void PlayFile(string srcFilePath)
        {
            using (var reader = new LpadStreamReader(File.OpenRead(srcFilePath)))
            {
                // ファイル情報を表示
                PrintFormatInfo(reader);

                // デコードして得られたPCMサンプルを再生
                AudioPlayer.PlaySound(reader.ReadSamples(), (uint)reader.SampleRate, 16, (uint)reader.NumChannels);

                // ENTERキーを待機
                Console.WriteLine(MES_ENTER_TO_STOP);
                Console.ReadLine();

                // 停止
                AudioPlayer.StopSound();
            }
        }

        /// <summary>
        /// コンソールからファイル名の入力を取得する。
        /// </summary>
        /// <returns></returns>
        public static string ReadFilePath()
        {
            string path = Console.ReadLine();

            if (path.StartsWith("\"") && path.EndsWith("\""))
            {
                return path.Substring(1, path.Length - 2);
            }

            return path;
        }

        private static void PrintFormatInfo(LpadStreamReader reader)
        {
            Console.WriteLine();
            Console.WriteLine($"[Encoded Audio Format Information]\n" +
                $"Sample Rate\t:\t{reader.SampleRate}Hz\n" +
                $"Channels\t:\t{reader.NumChannels}Channels\n" +
                $"Bits Per Sample\t:\t{(reader.BitsPerSample == 0 ? "Variable" : $"{reader.BitsPerSample}Bits")}");

            Console.WriteLine();

            Console.WriteLine("[Decoded Audio Format Information]\n" +
                "Output Format\t:\tWAV(Linear PCM)\n" +
                $"Sample Rate\t:\t{reader.SampleRate}Hz\n" +
                $"Bits Per Sample\t:\t16Bits\n" +
                $"Channels\t:\t{reader.NumChannels}Channels\n" +
                $"Stereo Mode\t:\tL/R");

            Console.WriteLine();
        }
    }
}