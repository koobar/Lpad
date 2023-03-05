using System;
using System.IO;

namespace LibLpad.Streams
{
    public class BitStream
    {
        // 非公開定数
        private const int BUFFER_MAX = 32;

        // 非公開フィールド
        private readonly Stream BaseStream;
        private readonly byte[] readBufferBuffer;
        private int writeBuffer;
        private int readBuffer;
        private int usedWriteBufferCount;
        private int usedReadBufferCount;

        // コンストラクタ
        public BitStream(Stream stream)
        {
            this.BaseStream = stream;
            this.writeBuffer = 0;
            this.usedWriteBufferCount = 0;
            this.usedReadBufferCount = BUFFER_MAX;
            this.readBufferBuffer = new byte[BUFFER_MAX / 8];
        }

        #region 書き込み

        /// <summary>
        /// 書き込み用バッファをストリームに書き込む。
        /// </summary>
        private void WriteBuffer()
        {
            this.BaseStream.Write(BitConverter.GetBytes(this.writeBuffer), 0, 4);
        }

        /// <summary>
        /// 書き込みを終了する。
        /// </summary>
        public void EndWrite()
        {
            WriteBuffer();
        }

        /// <summary>
        /// 1ビット書き込む。
        /// </summary>
        /// <param name="bit"></param>
        public void WriteBit(int bit)
        {
            if (bit == 1)
            {
                this.writeBuffer |= 1 << this.usedWriteBufferCount++;
            }
            else
            {
                this.writeBuffer &= ~(1 << this.usedWriteBufferCount++);
            }


            if (this.usedWriteBufferCount == BUFFER_MAX)
            {
                // バッファをストリームに書き込む。
                WriteBuffer();

                // 後始末
                this.writeBuffer = 0;
                this.usedWriteBufferCount = 0;
            }
        }

        /// <summary>
        /// ストリームに指定された値を、指定されたビット数で書き込む。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bits"></param>
        public void WriteUInt(int value, int bits)
        {
            for (int i = 0; i < bits; i++)
            {
                WriteBit(value >> i & 1);
            }
        }

        #endregion

        #region 読み込み

        /// <summary>
        /// バッファを読み込む。
        /// </summary>
        private void ReadBuffer()
        {
            this.BaseStream.Read(this.readBufferBuffer, 0, this.readBufferBuffer.Length);
            this.readBuffer = BitConverter.ToInt32(this.readBufferBuffer, 0);
        }

        /// <summary>
        /// ストリームから1ビット読み込む。
        /// </summary>
        /// <returns></returns>
        public int ReadBit()
        {
            if (this.usedReadBufferCount == BUFFER_MAX)
            {
                ReadBuffer();

                // 後始末
                this.usedReadBufferCount = 0;
            }

            return this.readBuffer >> this.usedReadBufferCount++ & 1;
        }

        /// <summary>
        /// ストリームから指定されたビット数の整数を読み込む。
        /// </summary>
        /// <param name="bits"></param>
        /// <returns></returns>
        public int ReadUInt(int bits)
        {
            int n = 0;

            for (int i = 0; i < bits; ++i)
            {
                if (ReadBit() == 1)
                {
                    n |= 1 << i;
                }
                else
                {
                    n &= ~(1 << i);
                }
            }

            return n;
        }

        #endregion
    }
}
