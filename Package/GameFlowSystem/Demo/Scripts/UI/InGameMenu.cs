using System;
using KahaGameCore.Package.DialogueSystem;
using KahaGameCore.Package.GameFlowSystem;
using KahaGameCore.Package.PlayerControlable;
using UnityEngine;

public class InGameMenu : MonoBehaviour
{
    public event Action<string> OnActionSelected;

    public void Button_SelectAction(string action)
    {
        OnActionSelected?.Invoke(action);
    }
}