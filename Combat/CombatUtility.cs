﻿using System.Collections.Generic;

namespace KahaGameCore.Combat
{
    public static class CombatUtility
    {
        public class CalculateData
        {
            public CombatUnit caster = null;
            public CombatUnit target = null;
            public ProcessData processData = null;
            public string formula = "";
            public bool useRawValue = false;
        }
        public static float Calculate(CalculateData data)
        {
            List<string> _buffer = new List<string> { "" };

            for (int i = 0; i < data.formula.Length; i++)
            {
                if (data.formula[i] == '(')
                {
                    _buffer.Add("");
                    continue;
                }

                if(i == data.formula.Length - 1 && data.formula[i] != ')')
                {
                    _buffer[_buffer.Count - 1] += data.formula[i];
                }

                if (data.formula[i] == ')' || i == data.formula.Length - 1)
                {
                    string _blockResult = _buffer[_buffer.Count - 1];

                    if (_buffer.Count > 1)
                    {
                        // means this block is a value command
                        if (_blockResult.Contains(",") || !_blockResult.Contains(".") && !int.TryParse(_blockResult, out int r))
                        {
                            string _commandBuffer = ""; // start get command
                            for(int j = _buffer[_buffer.Count - 2].Length - 1; j >= 0; j--)
                            {
                                if(_buffer[_buffer.Count - 2][j] != '+'
                                    && _buffer[_buffer.Count - 2][j] != '-'
                                    && _buffer[_buffer.Count - 2][j] != '*'
                                    && _buffer[_buffer.Count - 2][j] != '/'
                                    && _buffer[_buffer.Count - 2][j] != '('
                                    && _buffer[_buffer.Count - 2][j] != ')')
                                {
                                    _commandBuffer = _commandBuffer.Insert(0, _buffer[_buffer.Count - 2][j].ToString());
                                }
                                else
                                {
                                    break;
                                }
                            }

                            string _para = _buffer[_buffer.Count - 1];
                            int _commandResult = GetValueByCommand(data, _commandBuffer, _para);

                            _buffer[_buffer.Count - 2] = _buffer[_buffer.Count - 2].Replace(_commandBuffer, "");
                            _buffer.RemoveAt(_buffer.Count - 1);

                            if (_buffer.Count > 0)
                            {
                                _buffer[_buffer.Count - 1] += _commandResult;
                            }
                            else
                            {
                                _buffer.Add(_commandResult.ToString());
                            }
                            continue;
                        }

                        if(int.TryParse(_blockResult, out int _blockValue))
                        {
                            _buffer[_buffer.Count - 2] += _blockValue;
                        }
                        else
                        {
                            _buffer[_buffer.Count - 2] += Arithmetic(data, _blockResult);
                        }

                        _buffer.RemoveAt(_buffer.Count - 1);
                        continue;
                    }
                    else if (i == data.formula.Length - 1)
                    {
                        break;
                    }
                }

                _buffer[_buffer.Count - 1] += data.formula[i];
            }

            string _resultString = Arithmetic(data, _buffer[_buffer.Count - 1]);

            if (!float.TryParse(_resultString, out float _result))
            {
                _result = (float)GetValueByParaString(data, _resultString);
            }

            return _result;
        }

        public static int GetCombatFieldStatus(string statusName)
        {
            switch(statusName)
            {
                default:
                    {
                        UnityEngine.Debug.LogError("[CombatUtility][GetCombatFieldStatus] Invaild statusName=" + statusName);
                        return 0;
                    }
            }
        }

        public static int GetStatusValue(CombatUnit unit, string statusName, bool useRawValue)
        {
            if (unit == null)
            {
                UnityEngine.Debug.LogError("[CombatUtility][GetStatusValue] unit == null");
                return 0;
            }
            switch(statusName.Trim())
            {
                default:
                    {
                        UnityEngine.Debug.LogError("[CombatUtility][GetStatusValue] Invaild statusName=" + statusName);
                        return 0;
                    }
            }
        }

        private static int GetValueByCommand(CalculateData data, string command, string paraString)
        {
            bool _minus = false;
            if (command.StartsWith("-"))
            {
                command = command.Remove(0, 1);
                _minus = true;
            }
            switch (command)
            {
                case "Random":
                    {
                        string[] _varParts = paraString.Split(',');
                        int _min = System.Convert.ToInt32(float.Parse(Arithmetic(data, _varParts[0])));
                        int _max = System.Convert.ToInt32(float.Parse(Arithmetic(data, _varParts[1])));
                        if (_minus)
                            return -UnityEngine.Random.Range(_min, _max);
                        else
                            return UnityEngine.Random.Range(_min, _max);
                    }
                default:
                    {
                        throw new System.Exception("[CombatUtility][GetValueByCommand] Invaild command=" + command);
                    }
            }
        }

        private static int GetValueByParaString(CalculateData data, string paraString)
        {
            bool _minus = false;
            if(paraString.StartsWith("-"))
            {
                paraString = paraString.Remove(0, 1);
                _minus = true;
            }

            string[] _getValueData = paraString.Split('.');
            CombatUnit _getValueTarget = data.caster;

            switch (_getValueData[0].Trim())
            {
                case "Caster":
                    {
                        _getValueTarget = data.caster;
                        break;
                    }
                case "Target":
                    {
                        _getValueTarget = data.target;
                        break;
                    }
                case "CombatField":
                    {
                        if (_minus)
                            return -GetCombatFieldStatus(_getValueData[1]);
                        else
                            return GetCombatFieldStatus(_getValueData[1]);
                    }
                case "Random":
                    {
                        string _value = paraString.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("Random", "");
                        _value = paraString.Replace("(", "");
                        _value = paraString.Replace(")", "");
                        string[] _valueParts = _value.Split(',');
                        int _min = int.Parse(_valueParts[0]);
                        int _max = int.Parse(_valueParts[1]);

                        return UnityEngine.Random.Range(_min, _max);
                    }
                default:
                    {
                        throw new System.Exception("[CombatUtility][GetValueByParaString] Invaild target=" + _getValueData[0]);
                    }
            }

            if (_minus)
                return -GetStatusValue(_getValueTarget, _getValueData[1], data.useRawValue);
            else
                return GetStatusValue(_getValueTarget, _getValueData[1], data.useRawValue);
        }

        private static string Arithmetic(CalculateData data, string arithmeticString)
        {
            List<char> _mathString = new List<char>(arithmeticString);
            for (int _mathStringIndex = 0; _mathStringIndex < _mathString.Count; _mathStringIndex++)
            {
                if (_mathString[_mathStringIndex] == '*' || _mathString[_mathStringIndex] == '/')
                {
                    string _varA = "";
                    string _varB = "";
                    int _removeStartIndex = 0;
                    int _removeEndIndex = 0;

                    for (int _recordIndex = _mathStringIndex - 1; _recordIndex >= 0; _recordIndex--)
                    {
                        if (_mathString[_recordIndex] == '+')
                        {
                            _removeStartIndex = _recordIndex + 1;
                            break;
                        }

                        if (_mathString[_recordIndex] == '-')
                        {
                            if (_recordIndex == 0)
                            {
                                _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());
                                continue;
                            }
                            else
                            {
                                if (_mathString[_recordIndex - 1] == '('
                                    || _mathString[_recordIndex - 1] == ')'
                                    || _mathString[_recordIndex - 1] == '+'
                                    || _mathString[_recordIndex - 1] == '*'
                                    || _mathString[_recordIndex - 1] == '/')
                                {
                                    _removeStartIndex = _recordIndex + 1;
                                    break;
                                }
                                else
                                {
                                    _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());
                                    continue;
                                }
                            }
                        }

                        _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());

                        if (_recordIndex == 0)
                        {
                            _removeStartIndex = 0;
                        }
                    }

                    for (int _recordIndex = _mathStringIndex + 1; _recordIndex < _mathString.Count; _recordIndex++)
                    {
                        if (_mathString[_recordIndex] == '+'
                            || _mathString[_recordIndex] == '/'
                            || _mathString[_recordIndex] == '*')
                        {
                            _removeEndIndex = _recordIndex - 1;
                            break;
                        }

                        if (_mathString[_recordIndex] == '-')
                        {
                            if (_mathString[_recordIndex - 1] == '('
                                || _mathString[_recordIndex - 1] == ')'
                                || _mathString[_recordIndex - 1] == '+'
                                || _mathString[_recordIndex - 1] == '*'
                                || _mathString[_recordIndex - 1] == '/')
                            {
                                _removeStartIndex = _recordIndex + 1;
                                break;
                            }
                            else
                            {
                                _varB += _mathString[_recordIndex];
                                continue;
                            }
                        }

                        _varB += _mathString[_recordIndex];

                        if (_recordIndex == _mathString.Count - 1)
                        {
                            _removeEndIndex = _mathString.Count - 1;
                        }
                    }

                    if (!float.TryParse(_varA, out float _varAFloat))
                    {
                        _varAFloat = GetValueByParaString(data, _varA);
                    }

                    if (!float.TryParse(_varB, out float _varBFloat))
                    {
                        _varBFloat = GetValueByParaString(data, _varB);
                    }

                    if (_mathString[_mathStringIndex] == '*')
                    {
                        _mathString.RemoveRange(_removeStartIndex, _removeEndIndex - _removeStartIndex + 1);
                        _mathString.InsertRange(_removeStartIndex, System.Math.Round(_varAFloat * _varBFloat, 2).ToString("0.00"));
                    }
                    else
                    {
                        _mathString.RemoveRange(_removeStartIndex, _removeEndIndex - _removeStartIndex + 1);
                        _mathString.InsertRange(_removeStartIndex, System.Math.Round(_varAFloat / _varBFloat, 2).ToString("0.00"));
                    }

                    _mathStringIndex = 0;
                }
            }

            for (int _mathStringIndex = 0; _mathStringIndex < _mathString.Count; _mathStringIndex++)
            {
                if (_mathString[_mathStringIndex] == '+' || _mathString[_mathStringIndex] == '-')
                {
                    if(_mathStringIndex == 0)
                    {
                        continue;
                    }

                    string _varA = "";
                    string _varB = "";
                    int _removeStartIndex = 0;
                    int _removeEndIndex = 0;

                    for (int _recordIndex = _mathStringIndex - 1; _recordIndex >= 0; _recordIndex--)
                    {
                        if (_mathString[_recordIndex] == '+')
                        {
                            _removeStartIndex = _recordIndex + 1;
                            break;
                        }

                        if(_mathString[_recordIndex] == '-')
                        {
                            if (_recordIndex == 0)
                            {
                                _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());
                                continue;
                            }
                            else
                            {
                                if (_mathString[_recordIndex - 1] == '('
                                    || _mathString[_recordIndex - 1] == ')'
                                    || _mathString[_recordIndex - 1] == '+'
                                    || _mathString[_recordIndex - 1] == '*'
                                    || _mathString[_recordIndex - 1] == '/')
                                {
                                    _removeStartIndex = _recordIndex + 1;
                                    break;
                                }
                                else
                                {
                                    _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());
                                    continue;
                                }
                            }
                        }

                        _varA = _varA.Insert(0, _mathString[_recordIndex].ToString());

                        if (_recordIndex == 0)
                        {
                            _removeStartIndex = 0;
                        }
                    }

                    for (int _recordIndex = _mathStringIndex + 1; _recordIndex < _mathString.Count; _recordIndex++)
                    {
                        if (_mathString[_recordIndex] == '+')
                        {
                            _removeEndIndex = _recordIndex - 1;
                            break;
                        }

                        if (_mathString[_recordIndex] == '-')
                        {
                            if (_mathString[_recordIndex - 1] == '('
                                || _mathString[_recordIndex - 1] == ')'
                                || _mathString[_recordIndex - 1] == '+'
                                || _mathString[_recordIndex - 1] == '*'
                                || _mathString[_recordIndex - 1] == '/')
                            {
                                _removeStartIndex = _recordIndex + 1;
                                break;
                            }
                            else
                            {
                                _varB += _mathString[_recordIndex];
                                continue;
                            }
                        }

                        _varB += _mathString[_recordIndex];

                        if (_recordIndex == _mathString.Count - 1)
                        {
                            _removeEndIndex = _mathString.Count - 1;
                        }
                    }

                    if (!float.TryParse(_varA, out float _varAFloat))
                    {
                        _varAFloat = GetValueByParaString(data, _varA);
                    }

                    if (!float.TryParse(_varB, out float _varBFloat))
                    {
                        _varBFloat = GetValueByParaString(data, _varB);
                    }

                    if (_mathString[_mathStringIndex] == '+')
                    {
                        _mathString.RemoveRange(_removeStartIndex, _removeEndIndex - _removeStartIndex + 1);
                        _mathString.InsertRange(_removeStartIndex, System.Math.Round(_varAFloat + _varBFloat, 2).ToString());
                    }
                    else
                    {
                        _mathString.RemoveRange(_removeStartIndex, _removeEndIndex - _removeStartIndex + 1);
                        _mathString.InsertRange(_removeStartIndex, System.Math.Round(_varAFloat - _varBFloat, 2).ToString());
                    }

                    _mathStringIndex = 0;
                }
            }

            return new string(_mathString.ToArray());
        }
    }
}
