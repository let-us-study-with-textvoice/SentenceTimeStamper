# SentenceTimeStamper リリースノート

### 0.07　(10.July.2024)
* NAudioライブラリをver2.2.1に、そしてNAudio.WaveFormRendererをver2.0に変更した。
* NAudio.WaveFormRendererがver2.0に変更されたことにより、.Net4系のサポートがなくなり、
* .NetStandard2.0に対応となったことに伴い、
* NAudio.WaveFormRendererに追加していたSentenceInfoクラスと
* WaveFormArrangementクラスに大きく変更を加えた。
* SentenceInfoクラスは元々PictureBoxクラスを敬称していたが継承をやめ、
* SentenceTimeStamper空間でPictureBoxWithPicBoxクラスにその機能と関連機能を移した。

### 0.01 (12.May.2020)
初リリース