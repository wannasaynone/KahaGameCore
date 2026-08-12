# Audio

## 用途

Audio 提供場景型 `AudioManager`，集中控制 BGM、SFX、距離衰減音效、白噪音，以及 Master／BGM／SFX 三層音量。需要在多個 gameplay 系統共用同一套音量與播放入口時使用。

## 第一次使用：播放一個音效

開始前：呼叫端 asmdef 引用 `KahaGameCore.Audio`，並準備一個 `AudioClip`。

1. 在啟動場景建立 `AudioManager` GameObject，加入 `AudioManager` component。
2. 建立一個含 `AudioSource` 的 prefab，指定給 `Sound Effect Source Prefab`。
3. 再建立一個場景內 `AudioSource`，指定給 `Bgm Audio Source`。
4. 進入 Play Mode 後呼叫：

```csharp
using KahaGameCore.Audio;

AudioManager.Instance.PlaySound(clickClip);
```

預期結果：`AudioManager` 以 SFX prefab 建立播放來源並播放 `clickClip`。若 `Instance` 為 null，表示場景中的 manager 尚未執行 `Awake`。

## 常用操作

```csharp
AudioManager.Instance.MasterVolume = 0.8f;
AudioManager.Instance.BGMVolume = 0.5f;
AudioManager.Instance.SFXVolume = 1f;

AudioManager.Instance.PlayBGM(bgmClip);
AudioManager.Instance.PlaySoundWithDistance(impactClip, impactOrigin, listener);

AudioManager.Instance.OnVolumeChanged += snapshot =>
    SaveAudioSettings(snapshot);
```

需要白噪音時才指定 `White Noise Audio`。`ForcePlaySound` 用於確定允許同一 clip 重疊的情況。

## 生命週期與限制

- `AudioManager.Instance` 在 `Awake` 才建立；呼叫前必須先讓場景物件初始化。
- BGM fade 使用 DOTween，assembly 必須引用專案的 DOTween runtime。
- `PlaySound` 會抑制同 clip 的高音量重疊。
- Manager 不會自行 `DontDestroyOnLoad`，跨場景生命週期由專案 composition root 決定。
