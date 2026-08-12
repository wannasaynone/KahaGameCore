# Audio

## 目的

Audio 提供場景型 `AudioManager`，集中控制 BGM、SFX、距離衰減音效、白噪音，以及 Master／BGM／SFX 三層音量。

## 快速開始

1. 在啟動場景建立一個 GameObject 並加入 `AudioManager`。
2. 指定 `soundEffectSourcePrefab`、`bgmAudioSource`；需要白噪音時再指定 `whiteNoiseAudio`。
3. 確保場景只有一個 manager，然後呼叫：

```csharp
AudioManager.Instance.MasterVolume = 0.8f;
AudioManager.Instance.BGMVolume = 0.5f;
AudioManager.Instance.SFXVolume = 1f;

AudioManager.Instance.PlayBGM(bgmClip);
AudioManager.Instance.PlaySound(clickClip);
AudioManager.Instance.PlaySoundWithDistance(
    impactClip,
    impactOrigin,
    listener);
```

訂閱音量變更：

```csharp
AudioManager.Instance.OnVolumeChanged += snapshot =>
    SaveAudioSettings(snapshot);
```

## 注意事項

- `AudioManager.Instance` 在 `Awake` 才建立；呼叫前必須先讓場景物件初始化。
- BGM fade 使用 DOTween，assembly 必須能引用現有 DOTween runtime。
- `PlaySound` 會抑制同 clip 的高音量重疊；確定要重疊時用 `ForcePlaySound`。
- Manager 不會自行 `DontDestroyOnLoad`，跨場景生命週期由專案 composition root 決定。

