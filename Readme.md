# KahaGameCore
## 這是拿來幹嘛的？
這是一個用於協助使用Unity3D引擎開發遊戲/應用程式的開源開發套件，它包含了兩個部分：

一、一個基本的Manager-View開發框架，遵循著相關的開發規則進行開發時，可以盡可能地減少無解的程式邏輯錯誤。

二、各種常用的開發工具（例如物件池、遊戲資料/資源管理器）。

關於Unity3D引擎，可以至他們的官網查看：https://unity.com/
## 關於Manager-View開發框架
### 如何使用
1. 下載package後import到遊戲內  
https://github.com/wannasaynone/KahaGameCore/tree/master/Release  

2. 透過繼承對應的Class製作自訂的遊戲物件、遊戲邏輯
- KahaGameCore.Interface.Manager  
通常用來撰寫遊戲邏輯，可以同時操作View、UIView。
- KahaGameCore.Interface.View  
只要是場景上運作的Component就可以繼承他，遊戲開始時就會自動註冊起來供Manager使用。
- KahaGameCore.Interface.UIView  
與View類似，唯一不同的是同個類別只能存在一個。
- KahaGameCore.Interface.StateBase  
用來撰寫有限狀態機FSM，可以用於遊戲流程、AI等任何須要使用FSM的情形。  
### 使用範例
接下來，利用官方的2D UFO Tutorial相關素材製作一個簡易的小遊戲，藉此介紹這個開發框架該如何應用。  
在這個遊戲中，包含以下規則：  
```
- 遊戲開始30秒後結束
- 每吃掉一個黃色團塊，獲得一分，並在隨機位置產生新的素材
- 遊戲結束後結算玩家分數，並顯示出來
```  
範例專案（含step by setp教學）：  
https://github.com/wannasaynone/KahaGameCoreExample
## 常用開發工具
### FlagChecker
結合enum與Bitflag使用的檢查工具，支援Int64格式。
### GameDataManager
配合IGameData介面與Json String使用。製作完Json檔案後，利用IGameData製作靜態遊戲資料格式。
```json
{
    "ID":0,
    "FloatData":3.14159,
    "StringData":"圓周率日快樂"
}
```
```C#
public class ExampleData : IGameData
{
    public int ID {get; private set;}
    public float FloatData {get; private set;}
    public string StringData {get; private set;}
}
```
再利用相關API取得需求資料。
```C#
ExampleData neededData = GameDataManager.LoadGameData<ExampleData>("data file path here", 0);
Debug.Log(neededData.FloatData);

output = 3.14159
```
此外也提供其他json相關存、讀、刪檔功能。
```json
{
    "ID":0,
    "FloatData":3.14159,
    "StringData":"圓周率日快樂"
}
```
```C#
public class PureJsonData
{
    public int ID {get; private set;}
    public float FloatData {get; private set;}
    public string StringData {get; private set;}
}
```
```C#
PureJsonData neededData = GameDataManager.LoadJsonData<PureJsonData>();
Debug.Log(neededData.FloatData);

output = 3.14159
```
```C#
PureJsonData[] datas;
// set data into datas...
GameDataManager.SaveData(datas);
```
```C#
GameDataManager.DeleteJsonData<PureJsonData>();
```
可填入自定義路徑，預設路徑為
```
資料夾路徑：Application.persistentDataPath + "/Resources/Datas/"
檔案路徑：資料夾路徑 + className.txt
```
預測
### GameObjectPoolManager
物件池管理器，支援所有MonoBehaviour，有使用於範例專案。
```C#
SomeMonoClass _clone = GameObjectPoolManager.GetUseableObject<SomeMonoClass>(prefabfileObject);
```
### InputDetecter2D
令觸控可以與場景的Collider2D互動撰寫的觸控偵測器，通常用於按鈕互動，因此只支援單點操作。
```C#
private void Update()
{
    if(InputDetecter2D.ClickUp(collider2D))
    {
        Debug.Log("Touch Up");
    }
}
```
### TimerManager
基於Unity Life Cycle，可以延時觸發方法的功能。
```C#
TimerManager.Schedule(1f, delegate{ Debug.Log("1 sec later") });
```
### Processer
有時會有需要連續處理多個相同行為後，接回遊戲流程的需求。例如執行遊戲事件，遊戲事件包含多個遊戲邏輯命令，逐一執行後接回遊戲事件結束流程。這時就可以用Processer，配合IProcessable處理。
```C#
public class GameCommand : IProcessable
{
    public void Process(System.Action onCompleted)
    {
        // ...game command logic
        if(onCompleted != null)
            onCompleted();
    }
}
```
```C#
public class GameEventManager 
{
     public void ProcessEvent(GameCommand[] commands, System.Action onCompleted)
     {
         Processer gameCommandProcesser = new Processer(commands);
         gameCommandProcesser.Start(onCompleted);
     }
}
```
## 為什麼需要這個開發框架
Unity3D是極其自由的遊戲引擎，它使用了組件式component-based的概念。只要編寫繼承了MonoBehaviour的C#程式腳本，就可以將其作為一個組件掛載於遊戲物件GameObject上被遊戲引擎驅動。而便利的同時也帶來了問題，每一個組件Component都相當於擁有自己的運作邏輯，加上每一禎每一個Component都有可能互相影響，在遊戲開發規模越來越大時，不同Component的程式邏輯在執行流程不易全盤掌握的情況下，很容易導致各種不可預期的結果。且通常在這種情況下，Component之間通常有彼此過度相依，導致邏輯問題不易解決的狀況。

因此，有的開發者會改採用單一接口，也就是類似MVC的方式開發以解決這個問題。

將UI（通常是其他Input）做為Controller，以場景上的Component做為View，將邏輯封裝在裡層做為Model。透過場景上用來當作進入點Entry Point的組件觸發Model的實例化、驅動。通常類似開發框架通常也會使用event做為Entry Point的接口，以便後續功能的擴充，甚至是各個Model之間彼此溝通的手段。但如此一來也會產生因為Model之間使用event溝通導致流程追蹤不易、開發時因為必須透過Entry Point才能順利驅動遊戲邏輯導致測試不易等問題。

而這個Manager-View開發框架就是為了同時解決上述兩者的問題而設計的。

在Manager-View開發框架中，為了保留Unity3D原先就有的組件式開發的好處，也為了解決流程追蹤的問題，將每一個Component視為Entry Point，其後不同的行為分別會實例化一個類似於Model的Manager負責邏輯運算，同時繼承了View的Component也會在遊戲開始執行時註冊到可供Manager使用的清單內，令Manager可以隨時調用需求的View，並操作之。而不必通過event進行註冊、溝通。又若有多個View必須共用同一個Manager時，則套用單例Singleton即可。

如此一來，在表現上有異常時，可以直接從異常的View中相關的方法method逐一向前追蹤每一個步驟，達成方便追蹤流程的目的。又因為每一個Component可以自己做為一個程式邏輯獨立運作，進而解決Entry Point才能順利驅動遊戲邏輯導致測試不易的問題。最後則因為View和View之間、Manager和Manager之間通常不會彼此相依，亦大大地減少流程彼此衝突，產生預期外結果的情況。
