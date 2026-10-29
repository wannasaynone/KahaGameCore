# Interaction

「站進範圍、按下按鍵」的共用元件。`Interactable2D`（Collider2D）與 `Interactable3D`（Collider）共用 `InteractableBase<T>` 的邏輯，只負責轉發觸發器事件。

## 用法

1. 在有 trigger collider 的 GameObject 上加 `Interactable2D`。
2. `activatorLayers` 指定誰能觸發（通常是主角的 layer）；留空時會在 Awake 警告。
3. `bindings` 每一列是一個按鍵：`input`（InputActionReference）、`prompt`（可按時顯示的物件）、`enabled`。
4. 訂閱事件：
   - `Entered` / `Exited(activator)`：任何符合 layer 的對象進出時都會發出，不受焦點影響。沒有 binding 的「一進入就觸發」用法只用這兩個。
   - `Interacted(bindingIndex, activator)`：有焦點時按下該 binding 的鍵。

## 規則

- **焦點**：同一個 activator 同時在多個 interactable 範圍內時，只有最近的那個顯示提示、回應按鍵；距離相同時，最後進入的優先。
- **輸入**：binding 只在它的 InputAction 啟用時回應。暫停玩法輸入的方式是停用整個 action map；本模組不自行 Enable／Disable 任何 action，由專案負責啟用。
- 每個 interactable 每幀最多發出一次 `Interacted`。
- 停用元件時會清掉範圍內的對象並隱藏提示（Unity 停用時不會送 exit，重新啟用會再送 enter）。
- 執行中切換按鍵用 `SetBindingEnabled(index, value)`；程式組裝用 `Configure(layers, bindings)`。
