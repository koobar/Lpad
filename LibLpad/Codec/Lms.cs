using System;

namespace LibLpad.Codec
{
    internal class Lms
    {
        // 非公開定数
        private const int LMS_TAP = 16;
        private const int LMS_PREDICTED_VAL_SHIFT = 14;
        private const int LMS_DELTA_SHIFT = 8;

        // 非公開フィールド
        private readonly int[] history;
        private readonly int[] coefficients;

        // コンストラクタ
        public Lms()
        {
            this.history = new int[LMS_TAP];
            this.coefficients = new int[LMS_TAP];
        }

        public class LmsStatus
        {
            // 非公開フィールド
            private int[] history;
            private int[] coefficients;

            /// <summary>
            /// 過去サンプル
            /// </summary>
            public int[] History
            {
                get
                {
                    return this.history;
                }
            }

            /// <summary>
            /// 係数
            /// </summary>
            public int[] Coefficients
            {
                get
                {
                    return this.coefficients;
                }
            }

            /// <summary>
            /// 指定されたLMSフィルタから状態を取得して保持する。
            /// </summary>
            /// <param name="lms"></param>
            public void LoadFrom(Lms lms)
            {
                if (this.history == null || this.history.Length != lms.history.Length)
                {
                    this.history = new int[lms.history.Length];
                }

                if (this.coefficients == null || this.coefficients.Length != lms.coefficients.Length)
                {
                    this.coefficients = new int[lms.coefficients.Length];
                }

                Array.Copy(lms.history, this.history, lms.history.Length);
                Array.Copy(lms.coefficients, this.coefficients, lms.coefficients.Length);
            }
        }

        /// <summary>
        /// LMSフィルタの状態を設定する。
        /// </summary>
        /// <param name="status"></param>
        public void SetLmsStatus(LmsStatus status)
        {
            if (status.History.Length == this.history.Length && status.Coefficients.Length == this.coefficients.Length)
            {
                Array.Copy(status.History, this.history, status.History.Length);
                Array.Copy(status.Coefficients, this.coefficients, status.Coefficients.Length);
            }
        }

        /// <summary>
        /// LMSフィルタによる予測値を求める。
        /// </summary>
        /// <returns>予測された次のサンプル</returns>
        public int Predict()
        {
            int predicted = 0;

            for (int i = 0; i < LMS_TAP; ++i)
            {
                predicted += this.coefficients[i] * this.history[i];
            }

            return predicted >> LMS_PREDICTED_VAL_SHIFT;
        }

        /// <summary>
        /// LMSフィルタの係数を更新する。
        /// </summary>
        /// <param name="sample">デコードして得られるサンプル</param>
        /// <param name="residual">逆量子化された予測残差</param>
        public void Update(int sample, int residual)
        {
            // Δを計算する。
            int delta = residual >> LMS_DELTA_SHIFT;

            // 係数を更新
            for (int i = 0; i < LMS_TAP; ++i)
            {
                this.coefficients[i] += this.history[i] < 0 ? -delta : delta;
            }

            // 過去サンプルを更新
            for (int i = 0; i < LMS_TAP - 1; ++i)
            {
                this.history[i] = this.history[i + 1];
            }
            this.history[LMS_TAP - 1] = sample;
        }
    }
}
