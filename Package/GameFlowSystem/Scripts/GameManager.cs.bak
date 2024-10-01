using System;
using KahaGameCore.Actor;
using KahaGameCore.Processor;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem
{
    public class GameManager
    {
        public static GameManager Insatance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("GameManager is not initialized. Please call GameManager.Initialize() first.");
                    return null;
                }
                return instance;
            }
        }
        private static GameManager instance;

        public static void Initialize(InitializeFlowBase[] initializeFlowBases, Action onComplete)
        {
            if (instance != null)
            {
                Debug.LogError("GameManager is already initialized.");
                return;
            }

            instance = new GameManager();
            Processor<InitializeFlowBase> initializeProcessor = new Processor<InitializeFlowBase>(initializeFlowBases);
            initializeProcessor.Start(onComplete, instance.OnInitializeForceQuit);
        }

        private void OnInitializeForceQuit()
        {
            Debug.LogError("GameManager Initialize Force Quit");
        }

        public void LoadSave(int saveIndex)
        {
            GameData.Implemented.GameStaticDataDeserializer gameStaticDataDeserializer = new GameData.Implemented.GameStaticDataDeserializer();
            GameData.Implemented.GameStaticDataSerializer gameStaticDataSerializer = new GameData.Implemented.GameStaticDataSerializer();
            GameData.Implemented.JsonSaveDataHandler jsonSaveDataHandler = new GameData.Implemented.JsonSaveDataHandler(gameStaticDataSerializer, gameStaticDataDeserializer);

            GeneralValueContainer.SavableObject loadedSave = jsonSaveDataHandler.LoadSave<GeneralValueContainer.SavableObject>(saveIndex);
            if (loadedSave == default)
            {
                SharedRepoditory.playerInstance = new GeneralActor();
                jsonSaveDataHandler.Save(SharedRepoditory.playerInstance.GetSavableObject(), saveIndex);
            }
            else
            {
                SharedRepoditory.playerInstance = new GeneralActor();
                SharedRepoditory.playerInstance.Load(loadedSave);
            }
        }

        public void Save(int saveIndex)
        {
            if (SharedRepoditory.playerInstance == null)
            {
                Debug.LogError("SharedRepoditory.playerInstance is null.");
                return;
            }

            GameData.Implemented.GameStaticDataSerializer gameStaticDataSerializer = new GameData.Implemented.GameStaticDataSerializer();
            GameData.Implemented.JsonSaveDataHandler jsonSaveDataHandler = new GameData.Implemented.JsonSaveDataHandler(gameStaticDataSerializer, null);
            jsonSaveDataHandler.Save(SharedRepoditory.playerInstance.GetSavableObject(), saveIndex);
        }
    }
}