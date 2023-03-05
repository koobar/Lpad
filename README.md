# LPAD - Lms Predictive Adaptive Differential pulse code modulation -  
LPADは、実装の容易さ、音質、圧縮率を重視して設計された、時間領域での計算のみを使用したオーディオ用非可逆圧縮コーデックです。ADPCMでの符号化方法を参考に設計されており、同程度の圧縮率において、IMA_ADPCMと同等かそれ以上の音質（※1, ※2, ※3）を維持することができます。  

また、LPADでは、16ビットのリニアPCM音源を、最小2ビットから最大9ビットで量子化することができ、用途にそった使い方をできます。

※1 LPAD、IMA_ADPCM共に、48khz, 16bitsのリニアPCMを4bitsに圧縮した波形と、変換元ファイルの波形の誤差の平均を基に評価しています。  
※2 LPADのブロックサイズには、64（推奨値）を指定して評価しています。
※3 IMA_ADPCMへの変換には、[Audacity](https://www.audacityteam.org/)を、波形の比較には、[PCMDiff](https://www.vector.co.jp/soft/dl/win95/art/se392600.html)を使用させていただきました。

## ライセンス
LPADの仕様はパブリックドメインです。また、LPADのエンコーダ、およびデコーダのリファレンスコード（このプロジェクト）には、[MITライセンス](./LICENSE.md)が適用されます。なお、MITライセンスの日本語訳は、[こちら](https://licenses.opensource.jp/MIT/MIT.html)から閲覧いただけます。