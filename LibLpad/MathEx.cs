namespace LibLpad
{
    internal static class MathEx
    {
        /// <summary>
        /// 指定された値を、指定された範囲内に丸め込む。
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }

            if (value > max)
            {
                value = max;
            }

            return value;
        }

        /// <summary>
        /// 指定された値の最大値を返す。
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static int Max(params int[] values)
        {
            int max = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            return max;
        }
    }
}
