using Lpad.Wav;
using System;
using System.IO;

namespace Lpad
{
    internal static class AudioUtils
    {
        #region 再生(Windowsのみ)

        /// <summary>
        /// 与えられたサンプルを再生する。
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="channels"></param>
        public static void PlaySamples(short[] samples, uint sampleRate, uint bitsPerSample, uint channels)
        {
            AudioPlayer.PlaySound(samples, sampleRate, bitsPerSample, channels);

            // 停止待機
            Console.WriteLine("Press ENTER key to stop playing music.");
            Console.ReadLine();

            AudioPlayer.StopSound();
        }

        #endregion

        /// <summary>
        /// WAVファイルを読み込み、符号付き16ビットの整数のサンプル配列を返す。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="sampleRate"></param>
        /// <param name="channels"></param>
        /// <param name="bits"></param>
        /// <returns></returns>
        public static short[] ReadWAVFile(string path, out uint sampleRate, out uint channels, out uint bits)
        {
            var wav_dec = new WavDecoder(File.OpenRead(path));
            sampleRate = wav_dec.SampleRate;
            channels = wav_dec.Channels;
            bits = wav_dec.BitsPerSample;

            var ret = wav_dec.ReadAllSamples();
            
            // 後始末
            wav_dec.Dispose();

            return ret;
        }

        /// <summary>
        /// WAVファイルにサンプルを書き込む。
        /// </summary>
        /// <param name="output"></param>
        /// <param name="samples"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="channels"></param>
        public static void WriteWAVFile(string output, short[] samples, uint sampleRate, uint bitsPerSample, uint channels)
        {
            var wav_enc = new WavEncoder(File.Create(output), sampleRate, bitsPerSample, channels);

            wav_enc.WriteSamples(samples);
            wav_enc.Dispose();
        }
    }
}
