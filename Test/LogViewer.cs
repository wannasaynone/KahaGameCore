using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogViewer : MonoBehaviour
{
    private enum LogLevel
    {
        All,
        ErrorOnly
    }

    [SerializeField] private Text m_outputText = null;
    [SerializeField] private int m_maxLine = 5;
    [SerializeField] private LogLevel m_logLevel = LogLevel.All;

    private string[] m_logStack = null;

    private void Start ()
    {
        m_logStack = new string[m_maxLine];

        for(int i = 0; i < m_logStack.Length; i++)
        {
            m_logStack[i] = "";
        }

        Application.logMessageReceived += HandleLog;
    }

    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if(m_logLevel == LogLevel.ErrorOnly && 
            (type == LogType.Log || type == LogType.Warning))
        {
            return;
        }

        string _log = "";

        for(int i = 0; i < m_logStack.Length; i++)
        {
            if(i + 1 < m_logStack.Length)
            {
                m_logStack[i] = m_logStack[i + 1];
            }
            else
            {
                string _from = stackTrace.Split('\n')[1];
                string[] _methodInfo = _from.Split('.');
                string _className = _methodInfo[_methodInfo.Length - 2].Split(':')[0];
                m_logStack[i] = string.Format("[{0}][{1}] {2}", type.ToString(), _className, condition);
            }

            _log += m_logStack[i];

            if(i != m_logStack.Length - 1)
            {
                _log += "\n";
            }
        }

        m_outputText.text = _log;
    }
}
