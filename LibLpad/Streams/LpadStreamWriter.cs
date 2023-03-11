using LibLpad.Codec;
using System.IO;

namespace LibLpad.Streams
{
    public class LpadStreamWriter
    {
        // 非公開フィールド
        private readonly BinaryWriter OutputStream;

        // コンストラクタ
        public LpadStreamWriter(Stream stream)
        {
            this.OutputStream = new BinaryWriter(stream);
        }

        #region プロパティ

        /// <summary>
        /// サンプルレート
        /// </summary>
        public int SampleRate { set; get; }

        /// <summary>
        /// チャンネル数
        /// </summary>
        public int NumChannels { set; get; }

        /// <summary>
        /// サンプルの量子化ビット数
        /// </summary>
        public int BitsPerSample { set; get; }

        /// <summary>
        /// ブロックのサイズ
        /// </summary>
        public int BlockSize { set; get; }

        #endregion

        /// <summary>
        /// マジックナンバーを書き込む。
        /// </summary>
        private void WriteMagicNumber()
        {
            this.OutputStream.Write((byte)0x89);
            this.OutputStream.Write((byte)'L');
            this.OutputStream.Write((byte)'P');
            this.OutputStream.Write((byte)'A');
            this.OutputStream.Write((byte)'D');
        }

        /// <summary>
        /// ヘッダ部を書き込む。
        /// </summary>
        private void WriteHeader()
        {
            // フォーマットのバージョン
            this.OutputStream.Write(CodecInformation.FormatVersionId);

            // エンコーダのバージョン
            this.OutputStream.Write(CodecInformation.CodecVersionId);

            // サンプルレート
            this.OutputStream.Write(this.SampleRate);

            // チャンネル数
            this.OutputStream.Write((byte)this.NumChannels);

            // 予測残差の量子化ビット数
            this.OutputStream.Write((byte)this.BitsPerSample);

            // ブロックのサイズ
            this.OutputStream.Write(this.BlockSize);
        }

        /// <summary>
        /// サンプルをエンコードしてストリームに書き込む。
        /// </summary>
        /// <param name="samples"></param>
        private void WriteSamples(short[] samples)
        {
            // エンコーダを生成
            var encoder = new LpadEncoder(this.OutputStream);
            
            // エンコード開始
            encoder.Encode(samples, this.SampleRate, this.BitsPerSample, this.NumChannels, this.BlockSize);
        }

        /// <summary>
        /// ストリームを破棄する。
        /// </summary>
        public void Dispose()
        {
            this.OutputStream.Dispose();
        }

        /// <summary>
        /// ストリームに指定されたサンプルをエンコードして書き込む。
        /// </summary>
        /// <param name="samples"></param>
        public void Write(short[] samples)
        {
            WriteMagicNumber();
            WriteHeader();
            WriteSamples(samples);
        }
    }
}
