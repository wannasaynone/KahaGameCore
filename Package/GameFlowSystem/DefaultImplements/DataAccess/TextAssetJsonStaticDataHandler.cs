using System.Collections.Generic;
using System.Threading.Tasks;
using KahaGameCore.GameData;
using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.DataAccess
{
    /// <summary>
    /// 從手動指定的 TextAsset 讀取 JSON 陣列表格（Inspector 拖入，不依賴 Resources 路徑）。
    /// 以「檔名 = 資料型別名稱」對應（如 TimePhaseData.txt ↔ TimePhaseData）。
    /// </summary>
    public class TextAssetJsonStaticDataHandler : IGameStaticDataHandler
    {
        private readonly Dictionary<string, TextAsset> typeNameToAsset = new Dictionary<string, TextAsset>();
        private readonly IJsonReader jsonReader = new GameStaticDataDeserializer();

        public TextAssetJsonStaticDataHandler(params TextAsset[] tables)
        {
            if (tables == null)
            {
                return;
            }

            foreach (TextAsset table in tables)
            {
                if (table == null)
                {
                    continue;
                }
                typeNameToAsset[table.name] = table;
            }
        }

        public T[] Load<T>() where T : IGameData
        {
            string typeName = typeof(T).Name;
            if (!typeNameToAsset.TryGetValue(typeName, out TextAsset textAsset))
            {
                Debug.LogError($"[TextAssetJsonStaticDataHandler] 未指定 {typeName} 的表格 TextAsset（檔名需與型別名稱一致）。");
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
