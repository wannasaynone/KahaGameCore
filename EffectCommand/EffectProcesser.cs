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

        public EffectProcesser(Zenject.SignalBus signalBus)
        {
            signalBus.Subscribe<EffectTimingTriggedSignal>(Start);
        }

        public void SetUp(Dictionary<string, List<EffectData>> timingToEffectProcesser)
        {
            m_timingToEffectProcesser = timingToEffectProcesser;
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
                List<EffectData> _effects = m_timingToEffectProcesser[signal.Timing];

                ProcessData _processData = new ProcessData
                {
                    caster = signal.Caster,
                    target = signal.Target,
                    timing = signal.Timing,
                    skipIfCount = 0
                };

                for (int i = 0; i < _effects.Count; i++)
                {
                    _effects[i].SetProcessData(_processData);
                }

                new Processer<EffectData>(_effects.ToArray()).Start(null, null);
            }
        }
    }
}
