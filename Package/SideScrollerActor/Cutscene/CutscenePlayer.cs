using System;
using System.Collections.Generic;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.EffectProcessor.Data;
using UnityEngine;
using KahaGameCore.Package.SideScrollerActor.Cutscene.Command;
using KahaGameCore.Package.SideScrollerActor.Camera;
using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.SideScrollerActor.Cutscene
{
    public class CutscenePlayer
    {
        public static CutscenePlayer Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError("CutscenePlayer instance is null. Please ensure it is initialized before use.");
                    return null;
                }
                return instance;
            }
        }

        public static void Initialize(IDialogueView dialogueView)
        {
            if (instance == null)
            {
                instance = new CutscenePlayer(dialogueView);
            }
            else
            {
                Debug.LogWarning("CutscenePlayer instance is already initialized.");
            }
        }

        private static CutscenePlayer instance = null;

        private EffectCommandFactoryContainer effectCommandFactoryContainer = new EffectCommandFactoryContainer();
        private Dictionary<string, List<EffectProcessor.EffectProcessor.EffectData>> blockToEffectDatas = new Dictionary<string, List<EffectProcessor.EffectProcessor.EffectData>>();
        private bool isDeserialized = false;

        private Transform cameraLastTraget = null;

        private Action onCompleted = null;
        private EffectProcessor.EffectProcessor effectProcessor = null;

        private CutscenePlayer(IDialogueView dialogueView)
        {
            DeserializeData(dialogueView);
        }

        private async void DeserializeData(IDialogueView dialogueView)
        {
            if (isDeserialized)
            {
                return;
            }

            effectCommandFactoryContainer.RegisterFactory("MoveActor", new CutsceneCommandFactory_MoveActor());
            effectCommandFactoryContainer.RegisterFactory("MoveCamera", new CutsceneCommandFactory_MoveCamera());
            effectCommandFactoryContainer.RegisterFactory("PlayAnimation", new CutsceneCommandFactory_PlayAnimation());
            effectCommandFactoryContainer.RegisterFactory("SetCamera", new CutsceneCommandFactory_SetCamera());
            effectCommandFactoryContainer.RegisterFactory("PlaySound", new CutsceneCommandFactory_PlaySound());
            effectCommandFactoryContainer.RegisterFactory("DisableCutInText", new CutsceneCommandFactory_DisableCutInText());
            effectCommandFactoryContainer.RegisterFactory("CutInText", new CutsceneCommandFactory_CutInText());
            effectCommandFactoryContainer.RegisterFactory("BlackFadeIn", new CutsceneCommandFactory_BlackFadeIn());
            effectCommandFactoryContainer.RegisterFactory("BlackFadeOut", new CutsceneCommandFactory_BlackFadeOut());
            effectCommandFactoryContainer.RegisterFactory("TriggerDialogue", new CutsceneCommandFactory_TriggerDialogue(dialogueView));
            effectCommandFactoryContainer.RegisterFactory("Wait", new CutsceneCommandFactory_Wait());
            effectCommandFactoryContainer.RegisterFactory("SetActorPosition", new CutsceneCommandFactory_SetActorPosition());
            effectCommandFactoryContainer.RegisterFactory("SetCameraToActor", new CutsceneCommandFactory_SetCameraToActor());
            effectCommandFactoryContainer.RegisterFactory("PlayBGM", new CutsceneCommandFactory_PlayBGM());
            effectCommandFactoryContainer.RegisterFactory("StopBGM", new CutsceneCommandFactory_StopBGM());
            effectCommandFactoryContainer.RegisterFactory("PlayRepeatSound", new CutsceneCommandFactory_PlayRepeatSound());
            effectCommandFactoryContainer.RegisterFactory("ShowStoryCG", new CutsceneCommandFactory_ShowStoryCG());
            effectCommandFactoryContainer.RegisterFactory("HideStoryCG", new CutsceneCommandFactory_HideStoryCG());

            EffectCommandDeserializer effectCommandDeserializer = new EffectCommandDeserializer(effectCommandFactoryContainer);

            blockToEffectDatas = await effectCommandDeserializer.DeserializeAsync(Resources.Load<TextAsset>("Data/Cutscene").text);

            isDeserialized = true;
        }

        public void Play(string blockID, Action onCompleted)
        {
            if (effectProcessor != null)
            {
                Debug.LogError("Cutscene is already playing. Please wait for it to finish before starting a new one.");
                return;
            }

            if (blockToEffectDatas.ContainsKey(blockID))
            {
                this.onCompleted = onCompleted;

                cameraLastTraget = CameraController.Instance.target;
                CameraController.Instance.target = null;

                effectProcessor = new EffectProcessor.EffectProcessor();
                effectProcessor.OnProcessEnded += OnEnded;
                effectProcessor.SetUp(blockToEffectDatas);
                effectProcessor.Start(new ProcessData { caster = null, skipIfCount = 0, targets = null, timing = blockID });
            }
            else
            {
                Debug.LogError($"Block ID '{blockID}' not found in cutscene data.");
            }
        }

        private void OnEnded()
        {
            effectProcessor.OnProcessEnded -= OnEnded;
            effectProcessor = null;

            CameraController.Instance.target = cameraLastTraget;
            cameraLastTraget = null;

            if (onCompleted != null)
            {
                onCompleted.Invoke();
                onCompleted = null;
            }
        }
    }
}