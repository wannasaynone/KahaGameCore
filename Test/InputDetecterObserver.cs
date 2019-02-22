using KahaGameCore;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InputDetecterObserver : MonoBehaviour
{
    [SerializeField] private Text m_outputText = null;

    private string m_infoString = "";
    private Dictionary<InputDetecter2D.State, int> m_stateToTimes = new Dictionary<InputDetecter2D.State, int>();

    private InputDetecter2D m_inputDetecter2D = new InputDetecter2D();

    private void Awake()
    {
        m_outputText.raycastTarget = false;
    }

    private void Update ()
    {
        m_infoString = "";

        InputDetecter2D.InputInfo _inputInfo = m_inputDetecter2D.DetectInput();

        m_infoString += string.Format("Position={0},\n", _inputInfo.InputPosition);

        if(m_stateToTimes.ContainsKey(_inputInfo.InputState))
        {
            m_stateToTimes[_inputInfo.InputState]++;
        }
        else
        {
            m_stateToTimes.Add(_inputInfo.InputState, 1);
        }

        string _stateString = "";
        foreach(KeyValuePair<InputDetecter2D.State, int> keyValuePair in m_stateToTimes)
        {
            _stateString += string.Format("[{0}:{1}]", keyValuePair.Key.ToString(), keyValuePair.Value.ToString());
        }

        m_infoString += string.Format("State={0},\n", _stateString);

        m_infoString += string.Format("isOnUGUI={0},\n", _inputInfo.isOnUGUI);
        m_infoString += string.Format("RayCastCollider={0},\n", _inputInfo.RayCastCollider);
        m_infoString += string.Format("RayCastTranform={0},\n", _inputInfo.RayCastTranform);

        string _fingerIDs = "";
        var _fingerIDsList = m_inputDetecter2D.TouchIds;
        for(int i = 0; i < _fingerIDsList.Count; i++)
        {
            _fingerIDs += _fingerIDsList[i].ToString() + ",";
        }
        m_infoString += string.Format("Fingers={0}\n", _fingerIDs);

        m_outputText.text = m_infoString;
    }
}
