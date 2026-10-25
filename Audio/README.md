# Audio

## 用途

Audio 是遊戲播放聲音的唯一入口：BGM、一次性音效、循環音效，以及 Master／BGM／SFX 三層音量。`AudioManager` 是靜態類別，第一次呼叫時自己建立一個 `DontDestroyOnLoad` 的 `[AudioManager]` 物件，所以任何場景都能直接播放，不需要在場景放任何東西、也不需要在 Inspector 指定欄位。BGM 跨場景接續播放。

asmdef 為 `KahaGameCore.Audio`，不依賴其他 assembly。

## 第一次使用：播放一個音效

開始前：呼叫端 asmdef 引用 `KahaGameCore.Audio`，並準備一個 `AudioClip`。

```csharp
using KahaGameCore.Audio;

AudioManager.PlaySfx(clickClip);
```

預期結果：在 Play Mode 中聽到 `clickClip`，Hierarchy 的 DontDestroyOnLoad 底下出現 `[AudioManager]`。

## 常用操作

```csharp
// BGM：換歌時兩首 crossfade；要求正在播的同一首則繼續播，不重頭開始。
AudioManager.PlayBgm(bgmClip, fadeSeconds: 1f);
AudioManager.StopBgm(fadeSeconds: 1f);

// 一次性音效：2D（UI、受擊回饋），或在世界座標播放。
AudioManager.PlaySfx(hitClip);
AudioManager.PlaySfx(explosionClip, transform.position);

// 循環音效：掛在 owner 底下，跟著 owner 移動，owner 被刪除（含換場景）時一起結束。
AudioLoop engine = AudioManager.PlayLoop(engineClip, transform);
engine.Stop();

// 開新遊戲、回標題：停掉所有聲音，音量保留。
AudioManager.Reset();
```

## 音量

```csharp
AudioManager.SetVolume(AudioChannel.Bgm, 0.7f);
float bgm = AudioManager.GetVolume(AudioChannel.Bgm);
AudioManager.VolumeChanged += (channel, volume) => { /* 更新音量條 */ };
```

- 音量是玩家設定：自動存進 `PlayerPrefs`，下次啟動讀回來；`Reset()` 不會清掉。呼叫端不需要知道 PlayerPrefs key。
- 實際音量 = 該 channel 音量 × Master。改音量立刻套用到正在播的 BGM、音效和循環音效，包括 BGM fade 進行中。
- `VolumeChanged` 只帶「自己的值有變」的 channel：改 Master 只會收到 `Master`。

## 生命週期與限制

- 手動放在場景或 prefab 上的 `AudioSource` 不受音量設定控制。需要循環或跟隨物件的聲音請用 `PlayLoop`。
- 同一個 clip 在 0.05 秒內重複呼叫 `PlaySfx` 會被丟棄，避免疊音爆音。
- 位置音效使用線性衰減，距離由 `AudioManager.SpatialMinDistance`（預設 10）與 `SpatialMaxDistance`（預設 25）決定。預設值考慮 2D 攝影機在 z = -10、離場景 10 單位的情況；攝影機距離不同時請調整。
- fade 用 unscaled time，暫停遊戲（`Time.timeScale = 0`）時仍會進行。
- 關閉 domain reload 進入 Play Mode 時，靜態狀態會在 `SubsystemRegistration` 重置。
