# Lms Predictive - Adaptive Differential pulse code modulation (LPAD) SPECIFICATION
koobar 著  
最終更新：2023年3月3日  

## はじめに
昨今、多くの場面で使用される非可逆圧縮コーデックは、[周波数領域](https://ja.wikipedia.org/wiki/%E5%91%A8%E6%B3%A2%E6%95%B0%E9%A0%98%E5%9F%9F)での処理や[心理音響モデル](https://ja.wikipedia.org/wiki/%E9%9F%B3%E9%9F%BF%E5%BF%83%E7%90%86%E5%AD%A6)、特に[聴覚マスキング](https://en.wikipedia.org/wiki/Auditory_masking)に基づいた情報の削減などの手法を駆使し、非常に高い圧縮率と音質を両立している。通常、時間領域の音声信号を周波数領域の信号に変換するために、[離散コサイン変換（DCT）](https://ja.wikipedia.org/wiki/%E9%9B%A2%E6%95%A3%E3%82%B3%E3%82%B5%E3%82%A4%E3%83%B3%E5%A4%89%E6%8F%9B)や、その一種である[修正離散コサイン変換（MDCT）](https://ja.wikipedia.org/wiki/%E4%BF%AE%E6%AD%A3%E9%9B%A2%E6%95%A3%E3%82%B3%E3%82%B5%E3%82%A4%E3%83%B3%E5%A4%89%E6%8F%9B)を用いる。しかし、心理音響モデルや修正離散コサイン変換、さらにはこれに必要となる[窓関数](https://ja.wikipedia.org/wiki/%E7%AA%93%E9%96%A2%E6%95%B0)などの概念は複雑であり、これらの技術を用いて、音楽を聞くに堪える音質で圧縮することのできる程度のコーデックを独自に作成することは困難であると言える。  

一方で、[時間領域](https://ja.wikipedia.org/wiki/%E6%99%82%E9%96%93%E9%A0%98%E5%9F%9F)での計算のみを用いたコーデックも数多く存在する。[DPCM](https://ja.wikipedia.org/wiki/%E5%B7%AE%E5%88%86%E3%83%91%E3%83%AB%E3%82%B9%E7%AC%A6%E5%8F%B7%E5%A4%89%E8%AA%BF)や[ADPCM](https://ja.wikipedia.org/wiki/%E9%81%A9%E5%BF%9C%E7%9A%84%E5%B7%AE%E5%88%86%E3%83%91%E3%83%AB%E3%82%B9%E7%AC%A6%E5%8F%B7%E5%A4%89%E8%AA%BF)は古典的な時間領域のコーデックと言うことができる。[IMA_ADPCM](https://wiki.multimedia.cx/index.php/IMA_ADPCM)は、ADPCMの中でも特によく使用されており、16ビットPCMを1/4程度の情報量に圧縮しつつ、音楽として聞くことができる程度の音質を保つことができる。ADPCMの大きな利点の1つとして、実装の容易さを挙げることができる。IMA_ADPCMを含む多くの古典的なADPCMのアルゴリズムは、周波数領域での処理を要するコーデックと比較して非常に単純であり、計算量がごく僅かである。そのため、非常に高速なエンコード/デコード処理を可能とするほか、プログラムの実装が容易である。  

また、[FLAC](https://xiph.org/flac/index.html)等の線形予測を用いた可逆圧縮コーデックもまた、時間領域での計算のみを用いたコーデックと言える。可逆圧縮コーデックは、非可逆圧縮コーデックと比較し、圧縮率は遠く及ばないものの、データの劣化が一切発生しない点が特徴である。  

## LPADについて
Lms Predictive - Adaptive Differential pulse code modulation (LPAD) は、実装の容易さ、圧縮率、音質の3つの観点に着目して設計したコーデックである。LPADは非可逆圧縮であるものの、多くのテストケースにおいて、ADPCMよりも優れた音質（※）を保ちつつ、FLAC等の可逆圧縮コーデックよりも遥かに優れた圧縮率（非可逆圧縮であるため当然）を発揮することができた。実装の容易さを重視するため、当然、時間領域での計算のみを用いている。  

※ 16ビットPCMを4ビットのIMA_ADPCMでエンコードしたもののデコード結果と、元のファイルのサンプルとの誤差の程度、および、LPADの4ビット（ブロックサイズは64サンプル）でエンコードしたファイルのデコード結果と元のファイルのサンプルとの誤差の程度で比較。

## 技術概要
LPADによる音声信号の圧縮は、時間領域での計算のみが用いられる。LPADで使用されるアルゴリズムの根幹は、LMSフィルタを利用したサンプルの予測であり、予測されたサンプルと実際のサンプルの予測残差を、少ないビット数で可能な限り正確に記録することで、圧縮を図る。この手法では、音質を保つ場合、おおむね1/4から1/5程度の圧縮率を、音質を重視せず、会話内容を聞き取ることができる程度の音質でよい場合、1/8程度の圧縮率を達成した。この手法では、周波数領域や心理音響モデルなどに基づいた処理を行う、MP3やVorbisといった既存の非可逆圧縮コーデックと比較し、圧縮率の観点では大幅に劣る。一方で、アルゴリズムが単純であるため、数百行程度のソースコードでエンコーダ、およびデコーダを実装することができるほか、デコードを高速に行うことが可能となる。  

LPADにおけるサンプルのエンコードは、おおむね次の手順で行われる。
- エンコーダに与えられたサンプルをチャンネルごとに分離（2チャンネル以上の音声信号の場合）
- エンコーダに与えられたサンプルをブロック単位に分割
- LMSフィルタによる予測サンプルを求める
- 実際のサンプルと予測サンプルの誤差（予測残差）を求める
- 予測残差を量子化する
- LMSフィルタの係数を更新する

## 符号化アルゴリズムの詳解
### チャンネル分離
エンコーダに与えられたサンプル配列が複数チャンネル（2チャンネル以上）のものである場合、各チャンネルごとのサンプルを分離する。ステレオ音声の場合、左側のスピーカーで再生されるサンプルと、右側のスピーカーで再生されるサンプルとが交互に記録されているため、左側のサンプルのみを取り出したサンプル配列と、右側のサンプルのみを取り出したサンプル配列に分離する。  

次に示すプログラムは、チャンネル分離処理を行うメソッドの実装例である。
```cs
/// <summary>
/// 指定されたサンプル配列をチャンネルごとに分割して返す。
/// </summary>
/// <param name="samples">サンプル配列</param>
/// <param name="channels">チャンネル数</param>
/// <returns>各チャンネルごとに分離されたサンプルの配列</returns>
private short[][] SplitChannels(short[] samples, int channels)
{
    var channelSize = samples.Length / channels;
    var separatedChannels = new short[channels][];
    var offsets = new int[channels];

    for (int i = 0; i < channels; ++i)
    {
        separatedChannels[i] = new short[channelSize];
    }

    for (int i = 0; i < samples.Length; i += channels)
    {
        for (int j = 0; j < channels; ++j)
        {
            separatedChannels[j][offsets[j]++] = samples[i + j];
        }
    }

    return separatedChannels;
}
```

### ブロック分割
チャンネル分離処理が行われたサンプル配列それぞれを、さらにブロックごとに分割する。ここで、ブロックとは、一定の量のサンプルの集まりであり、後のエンコード処理は、このブロック単位で行われる。ブロックのサイズは1サンプルから8192サンプルを想定しており、ユーザはこの範囲内のサイズを指定することが可能である。通常は32サンプル、または64サンプルのブロックサイズを指定することを想定している。  

ブロックのサイズは、音質と圧縮率に大きく影響を与える。ブロックのサイズを小さくすれば音質は大幅に向上するが、ブロックのヘッダの量も増加するため、圧縮率の低下を引き起こすため、適切なサイズを指定する必要がある。  

### LMSフィルタによる予測サンプルの算出
他の時間領域での処理を用いたコーデックと同様、LPADのアルゴリズムの根幹は、次のサンプルの予測である。LPADでは、[最小平均二乗(LMS)フィルタ](https://en.wikipedia.org/wiki/Least_mean_squares_filter)を用いて、予測サンプルを算出する。  

LPADでは、通常、16タップのLMSフィルタを使用する。LPADにおけるLMSフィルタの予測は、過去16サンプルのサンプルに係数を乗算することで算出される「重み」の合計を、8192.0で割った値を予測値とする。ここで、8192.0は、複数のテストデータを用いた古典的な試行錯誤において、2のべき乗の値でもっともよい結果となった値である。  

ここで、LMSフィルタを用いたサンプルの予測を行うプログラムを示す。
```cs
private const int LMS_TAP = 16;
private const double LMS_DIV = 8192.0;
private double[] coefficients = new double[LMS_TAP];    // 係数配列
private int[] history = new int[LMS_TAP];               // 過去サンプル配列

/// <summary>
/// LMSフィルタによる予測値を求める。
/// </summary>
/// <returns>予測された次のサンプル</returns>
public int Predict()
{
    double predicted = 0;

    for (int i = 0; i < LMS_TAP; ++i)
    {
        predicted += this.coefficients[i] * this.history[i];
    }

    return (int)(predicted / LMS_DIV);
}
```
なお、LMSフィルタそのものは、すべてのブロックで共通のものを使用する。これにより、ブロックの開始位置のサンプルの予測精度が低下する問題を解決することに成功した。

### 予測残差の算出
LMSフィルタにより予測されたサンプルと、実際のサンプルの誤差（予測残差）を求める。LPADにおける予測残差の算出方法は、単純に、実際のサンプルから予測サンプルを減算した結果である。  

### 予測残差の量子化
予測残差の量子化は、エンコード後の音質を決定するもっとも重要な処理である。LPADでは、予測残差の量子化に2種類のテーブルが使用される。ステップサイズテーブルと、インデックス変化量テーブルである。  

#### ステップサイズテーブル
ステップサイズテーブルは、1から32767までの値を256段階で非線形的に増加させた値を格納する配列である。ステップサイズテーブルは、次のプログラムにより生成される。  
```cs
/// <summary>
/// ステップサイズテーブルを生成する。
/// </summary>
/// <returns></returns>
private static int[] CreateStepSizeTable()
{
    int[] result = new int[256];
    result[0] = 1;

    for (int i = 1; i < result.Length; ++i)
    {
        result[i] = (int)(result[i - 1] + ((result[i - 1] * 0.02954) + 1));
        result[i] = Clamp(result[i], 0, 32767);
    }

    return result;
}
```
#### インデックス変化量テーブル
インデックス変化量テーブルは、ステップサイズテーブルの値を参照するためのインデックスを増減させるための値を格納した配列である。ステップサイズテーブルのサイズは256であり、ステップサイズテーブルから値を参照するためのインデックスをそのまま記録すると、8ビットが必要になる。しかし、これでは、16ビットのPCMで記録された音声信号の1/2程度の圧縮効果しか得ることができない。そこで、より小さいサイズのインデックス変化量テーブルを予め用意しておき、直前のインデックスから目的のインデックスまでの変化量（実変化量）を求め、インデックス変化量テーブルに格納されたインデックスの変化量のうち、実変化量に最も近い変化量のインデックスで記録する方法をとる。これにより、聞くに堪える音質を保ちつつ、1/4から1/5程度の圧縮効果を得ることができる。  

インデックス変化量テーブルは、予測残差の量子化ビット数ごとに異なるテーブルを使用する。以下に、2ビットから6ビットまでのインデックス変化量テーブルを示す。   
```cs
// 各インデックス変化量テーブルのサイズは、2^(予測残差の量子化ビット数 - 1) + 1 で計算できる。
// 実際には、1ビットが符号で使用されるため、インデックス変化量テーブルのサイズは、
// 2の予測残差の量子化ビット数-符号分(1ビット)乗である。

// 2ビット用
private static readonly int[] indexTable2Bits = new int[2]
{
    -1, 3
};

// 3ビット用
private static readonly int[] indexTable3Bits = new int[4]
{
    -3, -1, 2, 4
};

// 4ビット用
private static readonly int[] indexTable4Bits = new int[8]
{
    -5, -4, -2, -1, 1, 2, 4, 6
};

// 5ビット用
private static readonly int[] indexTable5Bits = new int[16]
{
    -7, -6, -5, -4, -3, -2, -1, 0,
    1, 2, 3, 4, 5, 6, 7, 8
};

// 6ビット用
private static readonly int[] indexTable6Bits = new int[32]
{
    -16, -15, -14, -13, -12, -11, -10, -9,
    -7, -6, -5, -4, -3, -2, -1, 0,
    1, 2, 3, 4, 5, 6, 7, 8,
    9, 10, 11, 12, 13, 14, 15, 16
};
```
2ビットから6ビットまでのインデックス変化量テーブルは、配列に含まれる値をハードコーディングしている。これは、値を計算するアルゴリズムを特に決定していないためである。特に2ビットから4ビットといった、非常に限られた数の値しか扱うことのできないビット数においては、格納するインデックス変化量を、より音質の劣化が少なくなる値にすることが重要である。そこで、開発段階で用意したいくつかのテストケースで、最も誤差が少なくなる値を、古典的な試行錯誤により求め、その結果をハードコーディングした。   

一方で、7ビットから9ビット用のインデックス変化量テーブルは、次のプログラムにより生成可能である。
```cs
/// <summary>
/// 高ビット用のインデックス変化量テーブルを生成する。
/// </summary>
/// <param name="size">テーブルサイズ</param>
/// <returns></returns>
private static int[] CreateHighBitsIndexTable(int size)
{
    int[] result = new int[size];
    int k = size / 2;

    for (int i = 0; i < size; ++i)
    {
        result[i] = i - k;
    }

    return result;
}
```
上記のプログラムでは、-(size / 2)からsize / 2までの範囲の値を線形的に増加させた値が格納された配列を返す。7ビット以上といった高い量子化ビット数では、線形的に増加させた、特に工夫のないテーブルであっても、ほぼ区別のつかない程度の劣化に抑えることが可能であった。このことから、アルゴリズムの単純化のため、高い量子化ビット数では、線形的に増加させたインデックス変化量テーブルを使用することにした。

#### スケーリング
インデックス変化量テーブルは、限られた範囲内のインデックス変化量のみを格納し、その範囲は小さい。言い換えれば、単にインデックス変化量テーブルに含まれるインデックスの変化量のみを用いた方法では、インデックス変化量テーブルの範囲から大幅に外れる変化量が必要となる場合に、著しい音質の劣化が発生する。そこで、LPADでは、各ブロックごとに最適なスケールを計算し、インデックス変化量テーブルから参照した変化量に乗算する。  

1から16のスケールを用いて、ブロックに含まれる全てのサンプルに対する予測残差を量子化し、実際の予測残差と、量子化された予測残差を逆量子化することで得られる予測残差（逆量子化予測残差）の、差の絶対値の合計を求める。実際の予測残差と、逆量子化予測残差の差の絶対値の合計が最も小さくなるスケールを選択することで、効果的なスケール算出可能である。この方法は、非常に効率が悪いことは確かであるが、単純明解である。  

なお、効果的なスケールの算出方法を工夫することで、より高い音質を得ることができると考えられるが、現在は、ここに記述した方法をエンコーダに用いている。なお、エンコーダがスケールを算出する方法が変更されたとしても、スケールの適用方法や値の範囲等に変更がなければ、後方互換性を保つことができる。

#### 予測残差の量子化手順
ここまでで解説した、ステップサイズテーブル、インデックス変化量テーブル、スケーリングを用いた、予測残差の量子化手順について解説する。  

LPADでは、ステップサイズテーブルに格納された値の中で、予測残差に最も近い値のインデックスを効率的に記録することで、情報量の削減を図る。インデックスを効率的に符号化するため、インデックス変化量テーブルと、スケーリングにより求められた、ブロックごとに決定されるスケールを用いる。  

インデックスを効率的に符号化するための最初の手段は、インデックスの起点を0とするのではなく、直前のサンプルをエンコード/デコードした際のインデックスを起点とすることである。つまり、インデックスを、インデックスの変化量で表現することである。こうすることで、全体として出現する値を小さくすることができる。  

インデックスをそのまま記録する場合
| インデックス0 | インデックス1 | インデックス2 | インデックス3 |
|      --      |      --      |      --      |      --     |
|      0       |      1       |       2      |      4      |

インデックスを変化量で記録する場合
| インデックス0 | インデックス1 | インデックス2 | インデックス3 |
|      --      |      --      |      --      |      --     |
|      0       |      +1      |      +1      |      +2     |

インデックスをその変化量で記録することで、出現する値を小さくすることができたが、大きな変化が連続する場合には、当然、インデックスの変化量も大きくなるため、情報量の削減効果が薄れる。また、インデックスの変化量が負数になると、後述するビットストリームを用いた値の書き込みが行えない。そのため、インデックスの変化量を、予め用意されたインデックス変化量テーブルを用いて符号化する。  

インデックス変化量テーブルを用いた、インデックスの変化量を符号化する方法は、インデックス変化量テーブルに格納された値の中で、実際のインデックスの変化量に最も近い値のインデックスを記録することである。例えば、変化量が+8で、インデックス変化量テーブルの3番目の要素に+8が含まれている場合、符号化されたインデックスの変化量は3である。  

インデックス変化量テーブル
| インデックス          | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
| -------------------- |  - |  - |  - |  - |  - |  - |  - |  - |
| テーブルに格納された値 | -5 | -4 | -2 | -1 | +1 | +2 | +4 | +6 |

符号化するインデックスの変化量
| 符号化する変化量 | -2 | -8 | +4 | +6 | +8 | -4 | -3 | -2 |
| --------------- |  - |  - |  - |  - |  - |  - | - | - |

符号化されたインデックスの変化量
| 符号化された変化量 | 2 | 0 | 6 | 7 | 7 | 1 | 2 | 2 |
| ---------------- | - | - | - | - | - | - | - | - |

インデックス変化量テーブルに負数の変化量を格納することで、実際に記録される符号化されたインデックスの変化量を、非負の値のみで表現することができる。しかし、インデックス変化量テーブルに格納された変化量の範囲から大きく外れる変化があった場合には、情報の正確性が著しく低下する。これを防ぐため、インデックス変化量テーブルに格納された値に対して、スケールを乗算する。  

例えば、上記の表に示したインデックス変化量テーブルの各値に、スケール4を乗算すると、次のようなインデックス変化量テーブルを得ることができる。
| インデックス          | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
| -------------------- |  - |  - |  - |  - |  - |  - |  - |  - |
| テーブルに格納された値 | -20 | -16 | -8 | -4 | +4 | +8 | +16 | +24 |

インデックス変化量テーブルに格納された値が、すべて4倍になったことが分かる。LPADでは、ブロックごとに最適なスケールを1から16の範囲内で選択する。  

これらの符号化処理を介して、非常に大きなインデックスの変化量であっても、少ない範囲の非負整数のみで表現することを実現した。

ここで、LPADでの予測残差の量子化を行うプログラムの単純な実装例を以下に示す。なお、このプログラムでは、最適なスケールの算出を実装していない。
```cs
using System;

/// <summary>
/// 指定されたテーブルとインデックスを基に、与えられた予測残差を量子化する。
/// また、エンコード時の予測器の更新に必要となる、逆量子化された予測残差を計算する。
/// </summary>
/// <param name="stepSizeTable">ステップサイズテーブル</param>
/// <param name="indexTable">インデックス変化量テーブル</param>
/// <param name="currentStepIndex">現在のステップサイズ参照用インデックス</param>
/// <param name="scale">スケール</param>
/// <param name="residual">量子化する予測残差</param>
/// <returns>量子化された予測残差</returns>
private int QuantizeResidual(
    int[] stepSizeTable,
    int[] indexTable,
    ref int currentStepIndex,
    int scale,
    int residual)
{
    int minimumError = int.MaxValue;
    int defaultStepIndex = currentStepIndex;
    int quantizedResidual = 0;
    int absoluteResidual = Math.Abs(residual);

    // 0から15の範囲で表現されるスケールを、1から16の範囲での表現に変換
    scale += 1;

    // 力ずくで最も量子化誤差が小さくなる量子化予測残差を求める。
    for (int q = 0; q < indexTable.Length; q++)
    {
        // 試しにqを量子化された予測残差の絶対値とみなし、逆量子化する。
        int change = indexTable[q] * scale;
        int index = MathEx.Clamp(defaultStepIndex + change, 0, stepSizeTable.Length - 1);
        int dequantizedAbsoluteResidual = stepSizeTable[index];

        // 実際の予測残差と、逆量子化予測残差の差の絶対値を求める。
        int error = Math.Abs(dequantizedAbsoluteResidual - absoluteResidual);

        // 差が小さくなったか？
        if (error < minimumError)
        {
            minimumError = error;
            quantizedResidual = q;
            currentStepIndex = index;
        }
    }

    // 符号情報を付加
    int b = residual < 0 ? indexTable.Length : 0;
    quantizedResidual += b;

    return quantizedResidual;
}
```

### 予測残差の逆量子化手順
予測残差の量子化の段階で、逆量子化処理が必要となる。予測残差を逆量子化する手順は、量子化された予測残差から符号情報と、インデックス変化量テーブルを参照するためのインデックスを取得する。次に、直前のサンプルで使用されたステップサイズ参照用インデックスに、インデックス変化量テーブルから取得した値にスケールを乗算した変化量を加算し、ステップサイズ参照用インデックスを更新する。ステップサイズテーブルから、更新されたステップサイズ参照用インデックスに対応する要素を取得することで、予測残差の絶対値を得ることができる。これに符号情報を反映する。具体的には、符号情報が減算を示す場合、予測残差の絶対値に-1を乗算することで、符号情報を反映することができる。

次に示すプログラムは、量子化された予測残差を逆量子化するメソッドである。
```cs
/// <summary>
/// 量子化された予測残差を逆量子化する。
/// </summary>
/// <param name="stepSizeTable">ステップサイズテーブル</param>
/// <param name="indexTable">インデックス変化量テーブル</param>
/// <param name="currentStepIndex">現在のステップサイズ参照用インデックス</param>
/// <param name="scale">スケール</param>
/// <param name="quantizedResidual">量子化された予測残差</param>
/// <returns>逆量子化された予測残差</returns>
private int DequantizeResidual(
    int[] stepSizeTable,
    int[] indexTable,
    ref int currentStepIndex,
    int scale,
    int quantizedResidual)
{
    // 0から15の範囲で表現されるスケールを、1から16の範囲での表現に変換
    scale += 1;

    // 符号情報（ステップサイズを加算するのか、減算するのか）を取得する。
    int a = indexTable.Length;
    int sign = quantizedResidual >= a ? -1 : 1;

    // インデックス変化量テーブルを参照するためのインデックスを取得する。
    int index = sign == 1 ? quantizedResidual : quantizedResidual - a;

    // ステップサイズテーブルの参照用インデックスを求める
    int change = indexTable[index] * scale;
    currentStepIndex = MathEx.Clamp(currentStepIndex + change, 0, stepSizeTable.Length - 1);

    // ステップサイズを取得
    int dequantizedAbsoluteResidual = stepSizeTable[currentStepIndex];

    // ステップサイズに符号を反映し、逆量子化予測残差を求める。
    return sign * dequantizedAbsoluteResidual;
}
```

### LMSフィルタの係数更新
LMSフィルタの係数を更新する際には、予測残差が必要となる。しかし、実際の予測残差と、量子化された予測残差を逆量子化して得られる値は、同一になる可能性は低い。そこで、エンコーダの係数更新においても、逆量子化された予測残差を用いる。これにより、エンコーダとデコーダで全く同一の値による係数の更新が可能となるため、LPADのような非可逆圧縮コーデックにおいても、サンプルの予測を用いた処理を用いることが可能となる。

```cs
private const int LMS_TAP = 16;
private const double LMS_DELTA_COEFF = 0.0025;
private const int LMS_COEFF_DELTA_ROUND_DIGITS = 4;
private readonly int[] history;
private readonly double[] coefficients;

/// <summary>
/// LMSフィルタの係数を更新する。
/// </summary>
/// <param name="sample">デコードして得られるサンプル</param>
/// <param name="residual">逆量子化された予測残差</param>
public void Update(int sample, int residual)
{
    // Δを計算する。その際、浮動小数点数演算の誤差による影響を考慮し、丸めておく。
    double delta = Math.Round(residual * LMS_DELTA_COEFF, LMS_COEFF_DELTA_ROUND_DIGITS);

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
```
## ビットストリーム
符号化された音声信号は、ビットストリームを用いてファイルに書き込まれる。LPADでは、1サンプルあたり2ビットから9ビットまでの範囲内の量子化ビット数でサンプルが量子化される。しかし、C#のストリームは、通常、最小でも1バイト（8ビット）単位での読み書きを行うことが前提となっている。そこで、1ビット単位での読み書きを可能とするストリームを作成することで、冗長なビットを含めることなく、少ないビット数の値を扱うことを実現した。  

1ビット単位でストリームへの読み書きを行うプログラムの実装例を次に示す。
```cs
using System;           // 必須
using System.IO;        // 必須
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
```
なお、LPADの符号化方式では、量子化されたサンプルが負数になることは無いため、ストリームへの書き込みの際に負数を考慮する必要はない。

## 本ドキュメントのソースコードにおいて使用しているメソッドについて
本ドキュメントに含まれるソースコードでは、C#標準で使用できないメソッドを使用している。ここでは、これらのメソッドの実装を示す。   

Clamp:  nをmin以上max以下の範囲内に丸め込む。
```cs
// nをmin以上max以下の範囲内に丸め込む。
public int Clamp(int n, int min, int max)
{
    if (n < min)
    {
        return min;
    }
    else if (n > max)
    {
        return max;
    }

    return n;
}
```
## 最後に
本ドキュメントに記載されている内容は、今後の仕様変更などにより古くなる可能性がある。また、更新日時が変更されていたとしても、ドキュメントに変更点を反映することを忘れている可能性があるため、最新の正確な仕様を把握するためには、リファレンス実装のソースコードを参照することを推奨する。  
また、本ドキュメントに含まれるソースコードによる実装例などは、すべてC#を用いて記述している。  
なお、本ドキュメントに含まれるソースコードと、リファレンス実装において実際に使用されているソースコードやそれに含まれる定数等の値は、同じ演算処理を行っている場合であっても、異なる場合がある。

## About translation
The developer is a native Japanese speaker and this document, which provides more technical details, is written in Japanese. I may translate it into English if I feel like it, but it will take time. Until the translation is complete, please use a translation service such as [Google Translate](https://translate.google.co.jp/?hl=ja&sl=ja&tl=en&op=translate) or [DeepL](https://www.deepl.com/en/translator) Translate, or learn Japanese.