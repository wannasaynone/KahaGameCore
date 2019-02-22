using KahaGameCore;
using UnityEngine;
using UnityEngine.UI;

public class InputDetecterObserver : MonoBehaviour
{
    [SerializeField] private Text m_outputText = null;

    private string m_infoString = "";

    private void Awake()
    {
        m_outputText.raycastTarget = false;
    }

    private void Update ()
    {
        m_infoString = "";

        InputDetecter2D.InputInfo _inputInfo = InputDetecter2D.DetectInput();

        m_infoString += string.Format("Position={0},\n", _inputInfo.InputPosition);
        m_infoString += string.Format("State={0},\n", _inputInfo.InputState);
        m_infoString += string.Format("isOnUGUI={0},\n", _inputInfo.isOnUGUI);
        m_infoString += string.Format("RayCastCollider={0},\n", _inputInfo.RayCastCollider);
        m_infoString += string.Format("RayCastTranform={0},\n", _inputInfo.RayCastTranform);

        m_outputText.text = m_infoString;
    }
}
