# StateMachine 套件文檔

## 目錄
1. [概述](#概述)
2. [核心組件](#核心組件)
3. [系統架構](#系統架構)
4. [使用指南](#使用指南)
5. [API 參考](#api-參考)

## 概述

StateMachine 是一個用於 Unity 的視覺化狀態機系統，專為開發具有複雜狀態轉換邏輯的遊戲而設計。它提供了一套完整的工具和組件，用於創建、編輯和管理狀態機，包括狀態、轉換和條件等。

### 主要特點

- **視覺化編輯器**：直觀的狀態機編輯器，支持拖放操作和視覺化連接
- **基於 ScriptableObject**：所有狀態機組件都是 ScriptableObject，便於保存和重用
- **條件系統**：強大的條件系統，支持複雜的轉換邏輯
- **行為定義**：可自定義的進入、更新和退出行為
- **AnyState 支持**：特殊的 AnyState 節點，用於定義全局轉換
- **整合 EffectProcessor**：使用 EffectProcessor 執行狀態行為

## 核心組件

### StateMachineDefinition（狀態機定義）

StateMachineDefinition 是狀態機的主要容器，包含所有狀態和全局設置。

#### 主要功能

- **狀態管理**：管理狀態集合和默認狀態
- **AnyState**：支持特殊的 AnyState 節點
- **狀態查找**：通過 ID 快速查找狀態

### StateDefinition（狀態定義）

StateDefinition 定義了狀態機中的單個狀態，包括其行為和可能的轉換。

#### 主要功能

- **狀態標識**：唯一的狀態 ID 和名稱
- **行為定義**：進入、更新和退出行為
- **轉換集合**：從該狀態可能的所有轉換
- **編輯器位置**：在編輯器中的視覺位置

### TransitionDefinition（轉換定義）

TransitionDefinition 定義了從一個狀態到另一個狀態的轉換，包括轉換條件。

#### 主要功能

- **目標狀態**：轉換的目標狀態 ID
- **條件集合**：觸發轉換的條件
- **視覺設置**：轉換線的顏色

### ConditionDefinition（條件定義）

ConditionDefinition 定義了觸發轉換的條件，使用表達式語法。

#### 主要功能

- **條件表達式**：使用簡單的表達式語法定義條件
- **值比較**：支持多種比較操作（>, <, >=, <=, ==, !=）
- **值獲取**：從不同來源獲取值進行比較

### StateBehaviourDefinition（狀態行為定義）

StateBehaviourDefinition 定義了狀態的行為，使用 EffectProcessor 執行。

#### 主要功能

- **行為內容**：使用命令語法定義行為
- **異步執行**：使用 UniTask 異步執行行為
- **效果處理**：使用 EffectProcessor 處理效果命令

## 系統架構

StateMachine 套件使用 ScriptableObject 和編輯器擴展實現視覺化狀態機系統。

```
StateMachineDefinition
    |
    ├── StateDefinition (多個狀態)
    |       |
    |       ├── StateBehaviourDefinition (進入行為)
    |       ├── StateBehaviourDefinition (更新行為)
    |       ├── StateBehaviourDefinition (退出行為)
    |       └── TransitionDefinition (多個轉換)
    |               |
    |               └── ConditionDefinition (多個條件)
    |
    └── StateDefinition (AnyState)
            |
            └── TransitionDefinition (全局轉換)
                    |
                    └── ConditionDefinition (轉換條件)
```

## 使用指南

### 創建狀態機

1. 使用編輯器工具創建狀態機：
   - 在 Unity 菜單中選擇 `Tools > State Machine Editor`
   - 點擊 `New` 創建新的狀態機
   - 設置狀態機名稱並保存

2. 添加狀態：
   - 在編輯器中點擊 `Add State` 或右鍵點擊空白處選擇 `Add State`
   - 設置狀態名稱並保存
   - 第一個創建的狀態會自動設為默認狀態

3. 添加 AnyState（可選）：
   - 點擊 `Add AnyState` 添加特殊的 AnyState 節點
   - AnyState 的轉換會覆蓋普通狀態的轉換

### 創建轉換

1. 在編輯器中創建轉換：
   - 右鍵點擊源狀態，選擇 `Start Transition`
   - 點擊目標狀態完成轉換創建
   - 設置轉換名稱並保存

2. 添加條件：
   - 在轉換檢視器中點擊 `Add Condition`
   - 設置條件名稱和條件表達式
   - 條件表達式使用以下格式：
     ```
     Self.Health > 0;
     StateTime >= 2.0;
     Hero.Stamina <= 10;
     ```

### 定義狀態行為

1. 創建進入行為：
   - 在狀態檢視器中點擊 `Create Enter Behaviour`
   - 設置行為名稱和行為內容
   - 行為內容使用 EffectProcessor 命令語法

2. 創建更新行為：
   - 在狀態檢視器中點擊 `Create Update Behaviour`
   - 設置行為名稱和行為內容

3. 創建退出行為：
   - 在狀態檢視器中點擊 `Create Exit Behaviour`
   - 設置行為名稱和行為內容

### 使用狀態機

```csharp
// Load state machine definition
StateMachineDefinition stateMachine = Resources.Load<StateMachineDefinition>("Path/To/StateMachine");

// Initialize state machine
// Implementation depends on your specific runtime system
MyStateMachineRunner runner = new MyStateMachineRunner(stateMachine);

// Start state machine
runner.Start();

// Update state machine
void Update() {
    runner.Update();
}
```

## 條件表達式語法

條件表達式使用簡單的比較語法，支持以下操作：

- **比較操作符**：`>`, `<`, `>=`, `<=`, `==`, `!=`
- **值來源**：
  - `Self.PropertyName`：自身屬性
  - `Hero.PropertyName`：英雄屬性
  - `StateTime`：當前狀態持續時間
  - 數字常量：如 `0`, `10.5`
- **多條件**：使用分號 `;` 分隔多個條件（邏輯與）

示例：
```
Self.Health > 0; StateTime >= 2.0; Hero.Stamina <= 10
```

## 行為內容語法

行為內容使用 EffectProcessor 命令語法，具體取決於已註冊的效果命令。

示例：
```
PlayAnimation(Idle);
SetValue(Self.Speed, 5);
Wait(2.0);
```

## API 參考

### StateMachineDefinition 類

```csharp
// 屬性
public string stateMachineName;
public StateDefinition defaultState;
public List<StateDefinition> states;
public StateDefinition anyState;
public Dictionary<string, StateDefinition> statesByID;

// 方法
public void OnValidate();
```

### StateDefinition 類

```csharp
// 屬性
public string stateID;
public string stateName;
public Vector2 editorPosition;
public List<TransitionDefinition> transitions;
public static float stateTimer;
public StateBehaviourDefinition enterBehaviour;
public StateBehaviourDefinition exitBehaviour;
public StateBehaviourDefinition updateBehaviour;
```

### TransitionDefinition 類

```csharp
// 屬性
public string transitionID;
public string targetStateID;
public List<ConditionDefinition> conditions;
public Color lineColor;
```

### ConditionDefinition 類

```csharp
// 屬性
public string conditionID;
public string conditionName;
public string conditionContent;

// 方法
public bool Evaluate(IValueContainer self, List<IValueContainer> targets);
```

### StateBehaviourDefinition 類

```csharp
// 屬性
public string behaviourID;
public string behaviourName;
public string behaviourContent;

// 方法
public async UniTask Execute(EffectCommandFactoryContainer effectCommandFactoryContainer, 
                            IValueContainer caster, List<IValueContainer> targets);
```

---

本文檔提供了 StateMachine 套件的基本概述和使用指南。對於更詳細的實現細節，請參考源代碼和示例場景。
