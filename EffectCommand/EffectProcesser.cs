using KahaGameCore.Common;
using System;
using System.Collections.Generic;
using KahaGameCore.Processer;

namespace KahaGameCore.EffectCommand
{
    public class EffectProcesser
    {
        public class Facotry : Zenject.PlaceholderFactory<EffectProcesser>
        {

        }

        public class ProcessData
        {
            public string timing = "";
            public CombatUnit caster = null;
            public CombatUnit target = null;
            public int skipIfCount = 0;
        }

        private class EffectData : IProcessable
        {
            public EffectCommandBase command;
            public string[] vars;

            public void Process(Action onCompleted, Action onForceQuit)
            {
                if (command.processData.skipIfCount > 0 
                    && !command.IsIfCommand)
                {
                    onCompleted?.Invoke();
                }
                else
                {
                    command.Process(vars, onCompleted, onForceQuit);
                }
            }
        }

        private bool m_useable = false;

        private readonly Dictionary<string, List<EffectData>> m_timingToEffectProcesser = new Dictionary<string, List<EffectData>>();
        private readonly Zenject.SignalBus m_signalBus;
        private readonly EffectCommandFactoryContainer m_effectCommandFactoryContainer;

        public EffectProcesser(Zenject.SignalBus signalBus, EffectCommandFactoryContainer factoryContainer)
        {
            m_signalBus = signalBus;
            m_effectCommandFactoryContainer = factoryContainer;
        }
            
        public async System.Threading.Tasks.Task Initial(string rawCommandString)
        {
            if (string.IsNullOrEmpty(rawCommandString))
            {
                m_useable = true;
                return;
            }

            rawCommandString = rawCommandString.Replace(" ", "").Replace("\n", "").Replace("\t", "").Replace("\r", "");

            await StartDeserializeCommandTask(rawCommandString);

            m_signalBus.Subscribe<EffectTimingTriggedSignal>(Start);
            m_useable = true;
        }

        private System.Threading.Tasks.Task StartDeserializeCommandTask(string rawCommandString)
        {
            Dictionary<string, string> _timingToRawCommand = DeserializeCommandRawDatas(rawCommandString);

            if (_timingToRawCommand == null || _timingToRawCommand.Count <= 0)
                return System.Threading.Tasks.Task.CompletedTask;

            foreach (KeyValuePair<string, string> kvp in _timingToRawCommand)
            {
                string[] _commandStrings = kvp.Value.Split(';');
                List<EffectData> _effects = new List<EffectData>();
                for (int _commandStringIndex = 0; _commandStringIndex < _commandStrings.Length; _commandStringIndex++)
                {
                    if (string.IsNullOrEmpty(_commandStrings[_commandStringIndex]))
                        continue;

                    EffectData _effectData = DeserializeCommands(_commandStrings[_commandStringIndex]);
                    if (_effectData == null)
                    {
                        continue;
                    }

                    _effects.Add(_effectData);
                }
                m_timingToEffectProcesser.Add(kvp.Key, _effects);
            }

            return System.Threading.Tasks.Task.CompletedTask;

        }

        public void Start(EffectTimingTriggedSignal signal)
        {
            if (signal == null)
            {
                UnityEngine.Debug.Log("returned");
                return;
            }
            if(!m_useable)
            {
                TimerManager.Schedule(UnityEngine.Time.deltaTime, delegate { Start(signal); });
                return;
            }
            if (m_timingToEffectProcesser.ContainsKey(signal.ProcessData.timing))
            {
                List<EffectData> _effects = m_timingToEffectProcesser[signal.ProcessData.timing];
                for (int i = 0; i < _effects.Count; i++)
                {
                    _effects[i].command.processData = signal.ProcessData;
                }

                signal.ProcessData.skipIfCount = 0;
                new Processer<EffectData>(_effects.ToArray()).Start(null, null);
            }
        }

        private Dictionary<string, string> DeserializeCommandRawDatas(string rawData)
        {
            string _deserializeBuffer = "";
            string _timing = "";
            bool _startRecordCommands = false;
            Dictionary<string, string> _timingToRawCommand = new Dictionary<string, string>();

            for (int i = 0; i < rawData.Length; i++)
            {
                if (_startRecordCommands)
                {
                    if (rawData[i] == '}')
                    {
                        _timingToRawCommand.Add(_timing, _deserializeBuffer);
                        _deserializeBuffer = "";
                        _timing = "";
                        _startRecordCommands = false;
                        continue;
                    }

                    _deserializeBuffer += rawData[i];
                    continue;
                }

                if (rawData[i] == '{')
                {
                    _startRecordCommands = true;
                    _timing = _deserializeBuffer;
                    _deserializeBuffer = "";
                    continue;
                }

                _deserializeBuffer += rawData[i];
            }

            return _timingToRawCommand;
        }

        private EffectData DeserializeCommands(string commandData)
        {
            string _deserializeBuffer = "";
            int _leftCounter = 0;
            EffectData _newData = new EffectData();
            for (int i = 0; i < commandData.Length; i++)
            {
                if (_leftCounter > 0)
                {
                    if (commandData[i] == ')')
                    {
                        _leftCounter--;
                        if (_leftCounter <= 0)
                        {
                            int _varLeftCounter = 0;
                            string _varBuffer = "";
                            List<string> _varsTempList = new List<string>();
                            for(int j = 0; j < _deserializeBuffer.Length; j++)
                            {
                                if (_deserializeBuffer[j] == ',' && _varLeftCounter == 0)
                                {
                                    _varsTempList.Add(_varBuffer);
                                    _varBuffer = "";
                                    continue;
                                }
                                _varBuffer += _deserializeBuffer[j];
                                if(_deserializeBuffer[j] == ')')
                                {
                                    _varLeftCounter--;
                                }
                                if (_deserializeBuffer[j] == '(')
                                {
                                    _varLeftCounter++;
                                }
                                if(j == _deserializeBuffer.Length - 1)
                                {
                                    _varsTempList.Add(_varBuffer);
                                }
                            }

                            _newData.vars = _varsTempList.ToArray();
                            return _newData;
                        }
                    }

                    _deserializeBuffer += commandData[i];
                    if (commandData[i] == '(')
                    {
                        _leftCounter++;
                    }
                    continue;
                }

                if (commandData[i] == '(')
                {
                    _newData.command = m_effectCommandFactoryContainer.GetEffectCommand(_deserializeBuffer);
                    if (_newData.command != null)
                    {
                        _leftCounter++;
                        _deserializeBuffer = "";
                        continue;
                    }
                    else
                    {
                        return null;
                    }
                }
                _deserializeBuffer += commandData[i];
            }

            return null;
        }
    }
}
