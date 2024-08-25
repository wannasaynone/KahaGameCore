using System;
using KahaGameCore.Package.GameFlowSystem;
using UnityEngine;

public class InGameMenu : MonoBehaviour
{
    public event Action<string> OnActionSelected;

    [SerializeField] private TMPro.TextMeshProUGUI dayText;
    [SerializeField] private TMPro.TextMeshProUGUI timeText;

    private void OnEnable()
    {
        dayText.text = string.Format("Day: {0}", SharedRepoditory.playerInstance.Stats.GetTotal("Day", true).ToString("00"));
        timeText.text = string.Format("Time: {0}", SharedRepoditory.playerInstance.Stats.GetTotal("Time", true).ToString("00"));
    }

    public void Button_SelectAction(string action)
    {
        OnActionSelected?.Invoke(action);
    }
}