using Lpad.Wav;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Lpad
{
    internal static class AudioPlayer
    {
        [Flags]
        public enum PlaySoundFlags : int
        {
            SND_SYNC = 0x0000,
            SND_ASYNC = 0x0001,
            SND_NODEFAULT = 0x0002,
            SND_MEMORY = 0x0004,
            SND_LOOP = 0x0008,
            SND_NOSTOP = 0x0010,
            SND_NOWAIT = 0x00002000,
            SND_ALIAS = 0x00010000,
            SND_ALIAS_ID = 0x00110000,
            SND_FILENAME = 0x00020000,
            SND_RESOURCE = 0x00040004,
            SND_PURGE = 0x0040,
            SND_APPLICATION = 0x0080
        }

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PlaySound(byte[] pszSound, IntPtr hmod, PlaySoundFlags fdwSound);

        /// <summary>
        /// 与えられたサンプルを再生する。
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitsPerSample"></param>
        /// <param name="numChannels"></param>
        public static void PlaySound(short[] samples, uint sampleRate, uint bitsPerSample, uint numChannels)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                using (var mem = new MemoryStream())
                {
                    var wav_enc = new WavEncoder(mem, sampleRate, bitsPerSample, numChannels);

                    // WAVとしてエンコード
                    wav_enc.WriteSamples(samples);
                    wav_enc.Dispose();

                    // 再生
                    PlaySound(mem.ToArray(), IntPtr.Zero, PlaySoundFlags.SND_MEMORY | PlaySoundFlags.SND_ASYNC);
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// 再生中のサウンドを停止する。
        /// </summary>
        public static void StopSound()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // 停止
                PlaySound(null, IntPtr.Zero, PlaySoundFlags.SND_SYNC);

                // リソースを解放
                GC.Collect();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
