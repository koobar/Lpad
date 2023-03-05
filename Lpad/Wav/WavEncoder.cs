using System;
using System.IO;

namespace Lpad.Wav
{
    public class WavEncoder : IDisposable
    {
        // 非公開フィールド
        private readonly BinaryWriter outputStream;

        #region コンストラクタ

        public WavEncoder(Stream stream, uint sampleRate, uint bitsPerSample, uint channels)
        {
            this.outputStream = new BinaryWriter(stream);
            this.SampleRate = sampleRate;
            this.BitsPerSample = bitsPerSample;
            this.Channels = channels;
        }

        public WavEncoder(string path, uint sampleRate, uint bitsPerSample, uint channels)
        {
            this.outputStream = new BinaryWriter(File.Create(path));
            this.SampleRate = sampleRate;
            this.BitsPerSample = bitsPerSample;
            this.Channels = channels;
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// サンプルレート
        /// </summary>
        public uint SampleRate { set; get; }

        /// <summary>
        /// 量子化ビット数
        /// </summary>
        public uint BitsPerSample { set; get; }

        /// <summary>
        /// チャンネル数
        /// </summary>
        public uint Channels { set; get; }

        /// <summary>
        /// 長さ
        /// </summary>
        public long Length
        {
            get
            {
                return this.outputStream.BaseStream.Length;
            }
        }

        #endregion

        #region IDisposableの実装

        /// <summary>
        /// ストリームを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.outputStream.Dispose();
        }

        #endregion

        #region ストリームへデータを書き込むためのメソッド

        /// <summary>
        /// 指定されたストリームに指定されたバイナリデータを書き込む。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bytes"></param>
        private void WriteBytes(Stream stream, byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                stream.WriteByte(b);
            }
        }

        /// <summary>
        /// Write 16-bits unsigned integer to stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        private void WriteUInt16(Stream stream, ushort value)
        {
            WriteBytes(stream, BitConverter.GetBytes(value));
        }

        /// <summary>
        /// Write 32-bits unsigned integer to stream.
        /// </summary>
        /// <param name="value"></param>
        private void WriteUInt32(Stream stream, uint value)
        {
            WriteBytes(stream, BitConverter.GetBytes(value));
        }

        #endregion

        /// <summary>
        /// WAVEファイルに、指定された符号付き16ビット整数のサンプルを書き込む。
        /// </summary>
        /// <param name="samples"></param>
        public void WriteSamples(short[] samples)
        {
            // チャンクサイズを計算
            uint chunkSize = ((uint)samples.LongLength * 2) + 38;

            WriteHeader(chunkSize);
            WriteFormatChunk();
            WriteDataChunk(samples);
        }

        #region RIFFフォーマットでの書き込みに使用するメソッドの実装

        /// <summary>
        /// RIFFのマジックナンバーを書き込む。
        /// </summary>
        private void WriteMagicNumbers()
        {
            this.outputStream.Write((byte)0x52);
            this.outputStream.Write((byte)0x49);
            this.outputStream.Write((byte)0x46);
            this.outputStream.Write((byte)0x46);
        }

        /// <summary>
        /// ヘッダを書き込む。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="chunkSize"></param>
        private void WriteHeader(uint chunkSize)
        {
            WriteMagicNumbers();

            // チャンクサイズを書き込む。
            this.outputStream.Write(chunkSize);

            // 'WAVE' をASCIIコードで書き込む。
            this.outputStream.Write((byte)0x57);
            this.outputStream.Write((byte)0x41);
            this.outputStream.Write((byte)0x56);
            this.outputStream.Write((byte)0x45);
        }

        /// <summary>
        /// fmt チャンクを書き込む。
        /// </summary>
        /// <param name="stream"></param>
        private void WriteFormatChunk()
        {
            // 'fmt ' をASCIIコードで書き込む。
            this.outputStream.Write((byte)0x66);
            this.outputStream.Write((byte)0x6D);
            this.outputStream.Write((byte)0x74);
            this.outputStream.Write((byte)0x20);

            // fmt チャンクのバイト数を書き込む。
            this.outputStream.Write((uint)18);

            // オーディオフォーマットを書き込む。
            this.outputStream.Write((ushort)0x0001);

            // チャンネル数を書き込む。
            this.outputStream.Write((ushort)this.Channels);

            // サンプルレートを書き込む。
            this.outputStream.Write(this.SampleRate);

            // 1秒あたりの平均バイト数を書き込む。
            uint avrBytes = this.SampleRate * (this.BitsPerSample / 8) * this.Channels;
            this.outputStream.Write(avrBytes);

            // ブロックのサイズを書き込む。
            uint blockSize = this.Channels * this.BitsPerSample / 8;
            this.outputStream.Write((ushort)blockSize);

            // 量子化ビット数を書き込む。
            this.outputStream.Write((ushort)this.BitsPerSample);

            // ダミーの2バイトを書き込む。
            this.outputStream.Write((ushort)0);
        }

        /// <summary>
        /// 'data'チャンクを書き込む。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="sampleData"></param>
        private void WriteDataChunk(short[] samples)
        {
            // 'data' をASCIIコードで書き込む。
            this.outputStream.Write((byte)0x64);
            this.outputStream.Write((byte)0x61);
            this.outputStream.Write((byte)0x74);
            this.outputStream.Write((byte)0x61);

            // チャンクサイズを書き込む。
            this.outputStream.Write((uint)samples.LongLength * 2);

            // サンプルを書き込む。
            foreach (var sample in samples)
            {
                this.outputStream.Write(sample);
            }
        }

        #endregion
    }
}
