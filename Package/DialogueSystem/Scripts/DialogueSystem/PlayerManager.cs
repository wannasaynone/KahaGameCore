using System.Collections;
using System.Collections.Generic;
using KahaGameCore.GameData.Implemented;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class PlayerManager
    {
        public static PlayerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new System.Exception("PlayerManager is not initialized, use Initialize() first.");
                }
                return instance;
            }
        }

        private static PlayerManager instance;

        public static void Initialize()
        {
            instance = new PlayerManager();
            instance.player = instance.LoadPlayer();
        }

        public Player Player => player;
        private Player player;

        public void SavePlayer()
        {
            GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
            GameStaticDataSerializer gameStaticDataSerializer = new GameStaticDataSerializer();
            JsonSaveDataHandler jsonSaveDataHandler = new JsonSaveDataHandler(gameStaticDataSerializer, gameStaticDataDeserializer);
            jsonSaveDataHandler.Save(player, 0);
        }

        private Player LoadPlayer()
        {
            GameStaticDataDeserializer gameStaticDataDeserializer = new GameStaticDataDeserializer();
            GameStaticDataSerializer gameStaticDataSerializer = new GameStaticDataSerializer();
            JsonSaveDataHandler jsonSaveDataHandler = new JsonSaveDataHandler(gameStaticDataSerializer, gameStaticDataDeserializer);
            return jsonSaveDataHandler.LoadSave<Player>(0);
        }
    }
}