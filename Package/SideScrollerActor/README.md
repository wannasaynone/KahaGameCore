# SideScrollerActor 套件文檔

## 目錄
1. [概述](#概述)
2. [核心組件](#核心組件)
3. [系統架構](#系統架構)
4. [使用指南](#使用指南)
5. [API 參考](#api-參考)

## 概述

SideScrollerActor 是一個用於 Unity 的綜合性套件，專為開發 2D 側捲式動作遊戲而設計。它提供了一套完整的工具和組件，用於創建具有豐富互動性的側捲式遊戲體驗，包括角色控制、關卡管理、過場動畫、對話系統整合等功能。

### 主要特點

- **完整的角色系統**：提供角色移動、跳躍、衝刺和攻擊等基本動作
- **武器系統**：支持近戰和遠程武器，可擴展的武器切換機制
- **關卡管理**：處理關卡初始化、敵人生成和遊戲結束條件
- **過場動畫系統**：支持各種過場動畫命令，如角色移動、相機控制、對話觸發等
- **互動物件**：可自定義的互動物件系統，用於創建可收集物品、觸發器等
- **事件系統**：基於事件的通信系統，用於組件間的解耦合交互
- **多語言支持**：內建多語言支持，可輕鬆切換不同語言

## 核心組件

### Actor（角色）

Actor 是遊戲中的主要角色實體，包括玩家角色和敵人。它處理角色的狀態、移動、攻擊和互動等功能。

#### 主要功能

- **移動系統**：提供基本的左右移動、跑步、衝刺和跳躍功能
- **狀態管理**：處理角色的各種狀態（正常、跑步、衝刺、跳躍、攻擊等）
- **戰鬥系統**：支持不同類型的攻擊和武器
- **生命值和耐力系統**：管理角色的生命值和耐力
- **互動能力**：與遊戲世界中的物件互動

### ActorController（角色控制器）

ActorController 負責將玩家輸入轉換為角色動作。它使用命令模式將輸入與實際執行的動作分離。

### LevelManager（關卡管理器）

LevelManager 管理遊戲關卡的初始化、運行和結束。它處理敵人生成、物品收集、關卡狀態和遊戲結束條件。

#### 主要功能

- **關卡初始化**：設置角色、武器和敵人
- **遊戲流程控制**：開始、暫停和恢復遊戲
- **物品管理**：處理物品收集和記錄
- **敵人管理**：生成和控制敵人
- **遊戲結束條件**：檢查遊戲結束條件（如所有敵人死亡）

### GameManager（遊戲管理器）

GameManager 是遊戲的主要控制器，負責初始化遊戲、管理遊戲狀態和處理遊戲流程。

#### 主要功能

- **遊戲初始化**：加載遊戲數據和初始化系統
- **遊戲狀態管理**：管理不同的遊戲狀態（標題、戰鬥等）
- **UI 管理**：控制不同的 UI 視圖
- **多語言支持**：處理語言切換

### CutscenePlayer（過場動畫播放器）

CutscenePlayer 負責播放過場動畫，它支持各種命令來控制角色、相機、對話和特效。

#### 主要功能

- **命令系統**：支持多種過場動畫命令
- **相機控制**：控制相機移動和跟隨目標
- **角色控制**：移動和設置角色位置
- **對話觸發**：觸發對話系統
- **特效控制**：控制淡入淡出、聲音等特效

### InteractableObject（互動物件）

InteractableObject 是遊戲中可互動的物件基類，提供了基本的碰撞檢測和互動邏輯。

## 系統架構

SideScrollerActor 套件使用模組化設計，各組件通過事件系統進行通信，實現低耦合的架構。

```
GameManager
    |
    ├── GameState_Combat
    |       |
    |       ├── CombatState_GameStartFlowController
    |       ├── CombatState_LevelController
    |       └── CombatState_GameEndFlowController
    |
    ├── LevelManager
    |       |
    |       ├── Actor (Hero)
    |       |     |
    |       |     └── WeaponSwitcher
    |       |           |
    |       |           └── Weapons (MeleeWeapon, RangeWeapon)
    |       |
    |       ├── Actors (Monsters)
    |       └── InteractableObjects
    |
    ├── CutscenePlayer
    |       |
    |       └── CutsceneCommands
    |
    └── Views (UI)
            |
            ├── BootView
            ├── TitleView
            ├── InGameView
            └── GameEndView
```

## 使用指南

### 創建角色

1. 創建一個包含必要組件的 GameObject：
   - Animator
   - SpriteRenderer
   - Rigidbody2D
   - BoxCollider2D
   - Actor 腳本

2. 設置角色屬性：
   - 生命值和耐力
   - 移動速度和跳躍參數
   - 動畫參數

3. 添加武器：
   - 為角色添加 WeaponSwitcher 組件
   - 創建並添加武器（近戰或遠程）

### 創建關卡

1. 設置 LevelManager：
   - 指定英雄角色
   - 設置起始房間
   - 配置遊戲結束條件

2. 添加互動物件：
   - 繼承 InteractableObject 類創建自定義互動物件
   - 實現 Interact() 和 Exit() 方法

3. 設置敵人：
   - 創建敵人角色（與玩家角色類似）
   - 添加 AI 控制器（ActorTickerBase 的子類）

### 創建過場動畫

1. 在 Resources/Data/Cutscene 中定義過場動畫數據

2. 使用 CutscenePlayer 播放過場動畫：
   ```csharp
   CutscenePlayer.Instance.Play("過場動畫ID", () => {
       // 過場動畫結束後的回調
   });
   ```

3. 支持的過場動畫命令：
   - MoveActor：移動角色
   - MoveCamera：移動相機
   - PlayAnimation：播放動畫
   - SetCamera：設置相機位置
   - TriggerDialogue：觸發對話
   - 等等

## API 參考

### Actor 類

```csharp
// 移動方法
public void MoveRight();
public void MoveLeft();
public void RunRight();
public void RunLeft();
public void DashRight();
public void DashLeft();
public void SimpleJumpUp();
public void JumpTo(JumpInfo jumpInfo);
public void PauseMove(bool pause);

// 狀態方法
public void SetToIdle(bool forceStop = false);
public void SetForceInvincible(float time);
public void SetWaitingInteractObject(IInteractableObject interactableObject);
public void Interact();
```

### LevelManager 類

```csharp
// 靜態方法
public static int GetItemCount(int itemID);
public static void Pause();
public static void Resume();
public static GameObject GetSpecialGameObjectByName(string name);

// 實例方法
public void Initialize(List<OnItemRecordChanged> itemRecords, GameStaticDataManager gameStaticDataManager, int heroHealth, float heroStamina);
public void SimpleAddActorTicker<T>() where T : ActorTickerBase;
public void StartGame();
```

### CutscenePlayer 類

```csharp
// 靜態方法
public static void Initialize(IDialogueView dialogueView);

// 實例方法
public void Play(string blockID, Action onCompleted);
```

### InteractableObject 類

```csharp
// 抽象方法（需要子類實現）
protected abstract void Interact();
protected abstract void Exit();
```

### GameManager 類

```csharp
// 私有方法（內部使用）
private void OnGameInitialized();
private void StartGame(string startSequenceName);
private void OnGameEnded();
```

---

本文檔提供了 SideScrollerActor 套件的基本概述和使用指南。對於更詳細的實現細節，請參考源代碼和示例場景。
