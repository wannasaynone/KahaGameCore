using System;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem.DialogueCommand
{
    public class DialogueCommand_CreateCreateGeneralAnimationPlayer : DialogueCommandBase
    {
        private static List<GeneralAnimationPlayer> m_generalAnimationPlayers = new List<GeneralAnimationPlayer>();

        public static GeneralAnimationPlayer GetGeneralAnimationPlayer(string playerName)
        {
            GeneralAnimationPlayer generalAnimationPlayer = m_generalAnimationPlayers.Find(x => x.name == playerName);
            if (generalAnimationPlayer == null)
            {
                Debug.LogError("GeneralAnimationPlayer not found: " + playerName);
                return null;
            }

            return generalAnimationPlayer;
        }

        public DialogueCommand_CreateCreateGeneralAnimationPlayer(DialogueData dialogueData, IDialogueView dialogueView) : base(dialogueData, dialogueView)
        {
        }

        public override void Process(Action onCompleted, Action onForceQuit)
        {
            GeneralAnimationPlayer gameObject = UnityEngine.Object.Instantiate(Resources.Load<GeneralAnimationPlayer>(DialogueData.Arg1));
            float x = float.Parse(DialogueData.Arg2);
            float y = float.Parse(DialogueData.Arg3);

            gameObject.transform.position = new Vector3(x, y, 0);
        }
    }
}