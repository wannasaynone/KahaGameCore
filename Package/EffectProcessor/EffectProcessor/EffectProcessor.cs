using System;
using System.Collections.Generic;
using KahaGameCore.Package.EffectProcessor.Data;
using KahaGameCore.Package.EffectProcessor.Processor;

namespace KahaGameCore.Package.EffectProcessor
{
    public class EffectProcessor
    {
        public class EffectData : IProcessable
        {
            private readonly EffectCommandBase command;
            private readonly string[] vars;

            public EffectData(EffectCommandBase command, string[] vars)
            {
                this.command = command;
                this.vars = vars;
            }

            public int GetVarsLength()
            {
                return vars == null ? 0 : vars.Length;
            }

            public void SetProcessData(ProcessData processData)
            {
                command.processData = processData;
            }

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

        private Dictionary<string, List<EffectData>> m_timingToData = new Dictionary<string, List<EffectData>>();
        private event Action<ProcessData> OnProcessDataUpdated;
        private Dictionary<string, Processor<EffectData>> m_timingToProcesser = new Dictionary<string, Processor<EffectData>>();

        public void SetUp(Dictionary<string, List<EffectData>> timingToData)
        {
            m_timingToData = timingToData;
            foreach (KeyValuePair<string, List<EffectData>> keyValuePair in m_timingToData)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    OnProcessDataUpdated += keyValuePair.Value[i].SetProcessData;
                }
            }
        }

        public bool HasTiming(string timing)
        {
            return m_timingToData.ContainsKey(timing);
        }

        public void Start(ProcessData processData, Action onEnded = null, Action onQuitted = null)
        {
            if (m_timingToData.Count <= 0)
            {
                onEnded?.Invoke();
                return;
            }

            if (m_timingToData.ContainsKey(processData.timing))
            {
                OnProcessDataUpdated?.Invoke(processData);

                if (!m_timingToProcesser.ContainsKey(processData.timing))
                {
                    List<EffectData> _effects = m_timingToData[processData.timing];
                    m_timingToProcesser.Add(processData.timing, new Processor<EffectData>(_effects.ToArray()));
                }

                m_timingToProcesser[processData.timing].Start(onEnded, onQuitted);
            }
            else
            {
                onEnded?.Invoke();
            }
        }

        /// <summary>
        /// Force quits the current processing
        /// </summary>
        public void ForceQuit()
        {
            var processors = new Dictionary<string, Processor<EffectData>>(m_timingToProcesser);

            foreach (var processor in processors.Values)
            {
                processor.ForceQuit();
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, List<EffectData>> keyValuePair in m_timingToData)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    OnProcessDataUpdated -= keyValuePair.Value[i].SetProcessData;
                }
            }
        }
    }
}
