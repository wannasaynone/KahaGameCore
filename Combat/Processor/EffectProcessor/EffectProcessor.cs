using System;
using System.Collections.Generic;
using KahaGameCore.Processor;

namespace KahaGameCore.Combat.Processor.EffectProcessor
{
    public class EffectProcessor
    {
        public class Facotry : Zenject.PlaceholderFactory<EffectProcessor>
        {

        }

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

        private Dictionary<string, List<EffectData>> m_timingToEffectProcesser = new Dictionary<string, List<EffectData>>();

        public event Action OnProcessEnded;
        public event Action OnProcessQuitted;
        public event Action<ProcessData> OnProcessDataUpdated;

        private Dictionary<string, Processor<EffectData>> m_timingToProcesser = new Dictionary<string, Processor<EffectData>>();

        private readonly Zenject.SignalBus m_signalBus;

        public EffectProcessor(Zenject.SignalBus signalBus)
        {
            m_signalBus = signalBus;
            m_signalBus.Subscribe<EffectTimingTriggedSignal>(Start);
        }

        public void SetUp(Dictionary<string, List<EffectData>> timingToEffectProcesser)
        {
            m_timingToEffectProcesser = timingToEffectProcesser;
            foreach(KeyValuePair<string, List<EffectData>> keyValuePair in m_timingToEffectProcesser)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    OnProcessDataUpdated += keyValuePair.Value[i].SetProcessData;
                }
            }
        }
            
        public void Start(EffectTimingTriggedSignal signal)
        {
            if (signal == null)
            {
                return;
            }

            if (m_timingToEffectProcesser.Count <= 0)
            {
                return;
            }

            if (m_timingToEffectProcesser.ContainsKey(signal.Timing))
            {
                ProcessData _processData = new ProcessData
                {
                    caster = signal.Caster,
                    target = signal.Target,
                    timing = signal.Timing,
                    skipIfCount = 0
                };

                OnProcessDataUpdated?.Invoke(_processData);

                if (!m_timingToProcesser.ContainsKey(signal.Timing))
                {
                    List<EffectData> _effects = m_timingToEffectProcesser[signal.Timing];
                    m_timingToProcesser.Add(signal.Timing, new Processor<EffectData>(_effects.ToArray()));
                }

                m_timingToProcesser[signal.Timing].Start(OnProcessEnded, OnProcessQuitted);
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, List<EffectData>> keyValuePair in m_timingToEffectProcesser)
            {
                for (int i = 0; i < keyValuePair.Value.Count; i++)
                {
                    OnProcessDataUpdated -= keyValuePair.Value[i].SetProcessData;
                }
            }
            m_signalBus.Unsubscribe<EffectTimingTriggedSignal>(Start);
        }
    }
}
