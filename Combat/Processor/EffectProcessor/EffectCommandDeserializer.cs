using System.Collections.Generic;

namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public class EffectCommandDeserializer
    {
        private readonly EffectCommandFactoryContainer m_effectCommandFactoryContainer;

        public EffectCommandDeserializer(EffectCommandFactoryContainer factoryContainer)
        {
            m_effectCommandFactoryContainer = factoryContainer;
        }

        public async System.Threading.Tasks.Task<Dictionary<string, List<EffectProcessor.EffectData>>> DeserializeAsync(string rawCommandString)
        {
            if (string.IsNullOrEmpty(rawCommandString))
            {
                return new Dictionary<string, List<EffectProcessor.EffectData>>();
            }

            rawCommandString = rawCommandString.Replace(" ", "").Replace("\n", "").Replace("\t", "").Replace("\r", "");

            return await DeserializeCommandToKvp(rawCommandString);
        }

        public Dictionary<string, List<EffectProcessor.EffectData>> Deserialize(string rawCommandString)
        {
            if (string.IsNullOrEmpty(rawCommandString))
            {
                return new Dictionary<string, List<EffectProcessor.EffectData>>();
            }

            rawCommandString = rawCommandString.Replace(" ", "").Replace("\n", "").Replace("\t", "").Replace("\r", "");

            return DeserializeCommandToKvp(rawCommandString).Result;
        }

        private System.Threading.Tasks.Task<Dictionary<string, List<EffectProcessor.EffectData>>> DeserializeCommandToKvp(string rawCommandString)
        {
            Dictionary<string, string> _timingToRawCommand = DeserializeRawDataIntoTimingToLines(rawCommandString);

            if (_timingToRawCommand == null || _timingToRawCommand.Count <= 0)
                return System.Threading.Tasks.Task.FromResult(new Dictionary<string, List<EffectProcessor.EffectData>>());

            Dictionary<string, List<EffectProcessor.EffectData>> _timingToEffectDatas = new Dictionary<string, List<EffectProcessor.EffectData>>();

            foreach (KeyValuePair<string, string> kvp in _timingToRawCommand)
            {
                string[] _commandStrings = kvp.Value.Split(';');
                List<EffectProcessor.EffectData> _effects = new List<EffectProcessor.EffectData>();
                for (int _commandStringIndex = 0; _commandStringIndex < _commandStrings.Length; _commandStringIndex++)
                {
                    if (string.IsNullOrEmpty(_commandStrings[_commandStringIndex]))
                    {
                        continue;
                    }

                    EffectProcessor.EffectData _effectData = DeserializeCommandLineToEffectData(_commandStrings[_commandStringIndex]);
                    if (_effectData == null)
                    {
                        UnityEngine.Debug.Log("[EffectCommandDeserializer][DeserializeCommandToKvp] invail line=" + _commandStrings[_commandStringIndex] + ", continued");
                        continue;
                    }

                    _effects.Add(_effectData);
                }
                _timingToEffectDatas.Add(kvp.Key, _effects);
            }

            return System.Threading.Tasks.Task.FromResult(_timingToEffectDatas);
        }

        private Dictionary<string, string> DeserializeRawDataIntoTimingToLines(string rawData)
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

        private EffectProcessor.EffectData DeserializeCommandLineToEffectData(string commandData)
        {
            EffectCommandBase _tempCommand = null;
            string _deserializeBuffer = "";
            int _leftCounter = 0;
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
                            for (int j = 0; j < _deserializeBuffer.Length; j++)
                            {
                                if (_deserializeBuffer[j] == ',' && _varLeftCounter == 0)
                                {
                                    _varsTempList.Add(_varBuffer);
                                    _varBuffer = "";
                                    continue;
                                }
                                _varBuffer += _deserializeBuffer[j];
                                if (_deserializeBuffer[j] == ')')
                                {
                                    _varLeftCounter--;
                                }
                                if (_deserializeBuffer[j] == '(')
                                {
                                    _varLeftCounter++;
                                }
                                if (j == _deserializeBuffer.Length - 1)
                                {
                                    _varsTempList.Add(_varBuffer);
                                }
                            }

                            return new EffectProcessor.EffectData(_tempCommand, _varsTempList.ToArray());
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
                    _tempCommand = m_effectCommandFactoryContainer.GetEffectCommand(_deserializeBuffer);
                    if (_tempCommand != null)
                    {
                        _leftCounter++;
                        _deserializeBuffer = "";
                        continue;
                    }
                    else
                    {
                        UnityEngine.Debug.Log("[EffectCommandDeserializer][DeserializeCommandLineToEffectData] can't get command=" + _deserializeBuffer + ", return null. check is missing factory or raw data contains error");
                        return null;
                    }
                }
                _deserializeBuffer += commandData[i];
            }

            return null;
        }
    }
}