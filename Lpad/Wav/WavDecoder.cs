using System;
using System.IO;
using System.Text;

namespace Lpad.Wav
{
    public class WavDecoder : IDisposable
    {
        // 非公開フィールド
        private readonly BinaryReader InputStream;

        #region コンストラクタ

        public WavDecoder(Stream stream)
        {
            this.InputStream = new BinaryReader(stream);

            ReadAudioInfo();
        }

        public WavDecoder(string path)
        {
            this.InputStream = new BinaryReader(File.OpenRead(path));

            ReadAudioInfo();
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// サンプルレート
        /// </summary>
        public uint SampleRate { private set; get; }

        /// <summary>
        /// チャンネル数
        /// </summary>
        public uint Channels { private set; get; }

        /// <summary>
        /// 量子化ビット数
        /// </summary>
        public uint BitsPerSample { private set; get; }

        /// <summary>
        /// 1秒あたりの平均バイト数
        /// </summary>
        public uint AverageBytesPerSecond { private set; get; }

        /// <summary>
        /// ブロックサイズ
        /// </summary>
        public uint BlockSize { private set; get; }

        /// <summary>
        /// チャンクサイズ
        /// </summary>
        public uint ChunkSize { private set; get; }

        /// <summary>
        /// fmtチャンクのサイズ
        /// </summary>
        public uint FmtChunkSize { private set; get; }

        /// <summary>
        /// fmtチャンクのうち、拡張情報部分のサイズ
        /// </summary>
        public uint ExtraFormatInfoSize { private set; get; }

        #endregion

        #region IDisposableの実装

        public void Dispose()
        {
            this.InputStream.Dispose();
        }

        #endregion

        #region デコード

        /// <summary>
        /// すべてのサンプルを読み込み、16ビット符号付整数のサンプル配列として返す。
        /// </summary>
        /// <returns></returns>
        public short[] ReadAllSamples()
        {
            // dataチャンクの開始位置に移動する。
            MoveToChunk(this.InputStream, "data", true);

            const int sizeOfSample = 2;
            uint size = this.InputStream.ReadUInt32();
            var samples = new short[size / sizeOfSample];

            for (uint i = 0; i < samples.Length; ++i)
            {
                samples[i] = this.InputStream.ReadInt16();
            }

            return samples;
        }

        #endregion

        #region RIFFフォーマットのデータを読み込むためのメソッドの実装

        /// <summary>
        /// フォーマット情報を読み込む。
        /// </summary>
        private void ReadAudioInfo()
        {
            // ヘッダを読み込む。
            ReadHeader();

            // 'fmt 'チャンクの開始位置に移動
            if (MoveToChunk(this.InputStream, "fmt "))
            {
                // 'fmt 'チャンクを読み込む。
                ReadFmtChunk();
            }
            else
            {
                throw new Exception("fmt chunk not found.");
            }
        }

        /// <summary>
        /// 入力ストリームの現在の位置から続くデータが、指定されたバイナリデータと一致するか判定する。
        /// </summary>
        /// <param name="correctData"></param>
        /// <returns></returns>
        private bool CheckBytes(params byte[] correctData)
        {
            for(int i = 0; i < correctData.Length; ++i)
            {
                byte b = this.InputStream.ReadByte();

                if(b != correctData[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 入力ストリームの現在の位置から4バイト読み込み、マジックナンバーがRIFFのものであるか判定する。
        /// </summary>
        /// <returns></returns>
        private bool CheckMagicNumber()
        {
            return CheckBytes(0x52, 0x49, 0x46, 0x46);
        }

        /// <summary>
        /// 入力ストリームの現在の位置から4バイト読み込み、続くデータがASCIIコードで'WAVE'であるか判定する。
        /// </summary>
        /// <returns></returns>
        private bool IsWaveFormat()
        {
            return CheckBytes(0x57, 0x41, 0x56, 0x45);
        }

        /// <summary>
        /// 指定された名前のチャンクの開始位置に移動する。
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="chunkName"></param>
        /// <param name="findFromBegin"></param>
        /// <returns></returns>
        private bool MoveToChunk(BinaryReader stream, string chunkName, bool findFromBegin = false)
        {
            if (findFromBegin)
            {
                stream.BaseStream.Position = 0;
            }

            // チャンク名のASCIIコードを取得
            byte c1 = Encoding.ASCII.GetBytes(chunkName[0].ToString())[0];
            byte c2 = Encoding.ASCII.GetBytes(chunkName[1].ToString())[0];
            byte c3 = Encoding.ASCII.GetBytes(chunkName[2].ToString())[0];
            byte c4 = Encoding.ASCII.GetBytes(chunkName[3].ToString())[0];

        Find:
            if(!CheckBytes(c1, c2, c3, c4))
            {
                if(stream.BaseStream.Position >= stream.BaseStream.Length)
                {
                    return false;
                }

                goto Find;
            }

            return true;
        }

        /// <summary>
        /// 入力ストリームの現在の位置から、'fmt 'チャンクを読み込む。
        /// </summary>
        private void ReadFmtChunk()
        {
            // チャンクサイズを取得
            this.FmtChunkSize = this.InputStream.ReadUInt32();

            // リニアPCMか？
            if (this.InputStream.ReadUInt16() == 0x0001)
            {
                // 各種フォーマット情報を読み込む。
                this.Channels = this.InputStream.ReadUInt16();
                this.SampleRate = this.InputStream.ReadUInt32();
                this.AverageBytesPerSecond = this.InputStream.ReadUInt32();
                this.BlockSize = this.InputStream.ReadUInt16();
                this.BitsPerSample = this.InputStream.ReadUInt16();

                // 拡張情報分のサイズを読み込む。
                this.ExtraFormatInfoSize = this.InputStream.ReadUInt16();

                // 拡張情報分のサイズだけストリームを読み飛ばす。
                // ※このデコーダはリニアPCMしかサポートしないため、拡張情報を読み込む必要はない。
                this.InputStream.BaseStream.Position += this.ExtraFormatInfoSize;
            }
            else
            {
                throw new InvalidDataException("Unsupported audio data format.");
            }
        }

        /// <summary>
        /// 入力ストリームの現在の位置から、ヘッダ情報を読み込む。
        /// </summary>
        private void ReadHeader()
        {
            // マジックナンバーがRIFFのものであるか確認する。
            if (CheckMagicNumber())
            {
                // チャンクサイズを取得。
                this.ChunkSize = this.InputStream.ReadUInt32();

                // 次に続く4バイトがASCIIコードで'WAVE'であるか判定する。
                if (!IsWaveFormat())
                {
                    throw new InvalidDataException("Unsupported data format.");
                }
            }
            else
            {
                throw new InvalidDataException("Unsupported data format.");
            }
        }

        #endregion
    }
}
