using LibLpad.Codec;
using System;
using System.IO;

namespace LibLpad.Streams
{
    public class LpadStreamReader : IDisposable
    {
        // 非公開フィールド
        private readonly BinaryReader InputStream;

        // コンストラクタ
        public LpadStreamReader(Stream stream)
        {
            this.InputStream = new BinaryReader(stream);

            if (CheckMagicNumber())
            {
                ReadHeader();
            }
            else
            {
                throw new Exception("Unsupported file format.");
            }
        }

        #region プロパティ

        /// <summary>
        /// フォーマットのバージョン
        /// </summary>
        public byte FormatVersion { private set; get; }

        /// <summary>
        /// エンコードに使用されたエンコーダのバージョン
        /// </summary>
        public byte CodecVersion { private set; get; }

        /// <summary>
        /// サンプルレート
        /// </summary>
        public int SampleRate { private set; get; }

        /// <summary>
        /// チャンネル数
        /// </summary>
        public int NumChannels { private set; get; }

        /// <summary>
        /// サンプルの量子化ビット数
        /// </summary>
        public int BitsPerSample { private set; get; }

        /// <summary>
        /// ブロックのサイズ
        /// </summary>
        public int BlockSize { private set; get; }

        /// <summary>
        /// 長さ
        /// </summary>
        public long Length
        {
            get
            {
                return this.InputStream.BaseStream.Length;
            }
        }

        #endregion

        /// <summary>
        /// ストリームを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.InputStream.Dispose();
        }

        /// <summary>
        /// ヘッダ情報を読み込む。
        /// </summary>
        private void ReadHeader()
        {
            this.FormatVersion = this.InputStream.ReadByte();
            this.CodecVersion = this.InputStream.ReadByte();
            this.SampleRate = this.InputStream.ReadInt32();
            this.NumChannels = this.InputStream.ReadByte();
            this.BitsPerSample = this.InputStream.ReadByte();
            this.BlockSize = this.InputStream.ReadInt32();
        }

        /// <summary>
        /// サンプルを読み込む。
        /// </summary>
        /// <returns></returns>
        public short[] ReadSamples()
        {
            // デコーダを生成
            var decoder = new LpadDecoder(this.InputStream);
            decoder.DecodeWithMultithread = Environment.ProcessorCount >= this.NumChannels;

            // デコードして返す。
            return decoder.Decode(this.NumChannels, this.BlockSize);
        }

        /// <summary>
        /// マジックナンバー部を読み込み、正しいのものであるか判定する。
        /// </summary>
        /// <returns></returns>
        private bool CheckMagicNumber()
        {
            if (this.InputStream.ReadByte() == 0x89 &&
                this.InputStream.ReadChar() == 'L' &&
                this.InputStream.ReadChar() == 'P' &&
                this.InputStream.ReadChar() == 'A' &&
                this.InputStream.ReadChar() == 'D')
            {
                return true;
            }

            return false;
        }
    }
}
