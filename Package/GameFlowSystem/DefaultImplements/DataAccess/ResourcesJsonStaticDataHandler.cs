using System.Threading.Tasks;
using KahaGameCore.GameData;
using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.DataAccess
{
    /// <summary>
    /// 從 Resources/GameData/{型別名稱}.txt 讀取 Google Sheet 匯出的 JSON 陣列。
    /// 之後若改為線上下載或 Addressables，只需另外實作 IGameStaticDataHandler 並於組裝處替換。
    /// </summary>
    public class ResourcesJsonStaticDataHandler : IGameStaticDataHandler
    {
        private const string ROOT_PATH = "GameData/";

        private readonly IJsonReader jsonReader = new GameStaticDataDeserializer();

        public T[] Load<T>() where T : IGameData
        {
            string resourcePath = ROOT_PATH + typeof(T).Name;
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"[ResourcesJsonStaticDataHandler] 找不到資料檔 Resources/{resourcePath}.txt");
                return new T[0];
            }

            return jsonReader.Read<T[]>(textAsset.text);
        }

        public Task<T[]> LoadAsync<T>() where T : IGameData
        {
            return Task.FromResult(Load<T>());
        }
    }
}
