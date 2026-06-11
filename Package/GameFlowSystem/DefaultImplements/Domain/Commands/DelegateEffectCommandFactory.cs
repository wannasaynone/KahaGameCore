using System;
using KahaGameCore.Package.EffectProcessor;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements.Commands
{
    /// <summary>以委派建立指令實例的通用工廠，省去每個指令各寫一個工廠類別。</summary>
    public class DelegateEffectCommandFactory : EffectCommandFactoryBase
    {
        private readonly Func<EffectCommandBase> createCommand;

        public DelegateEffectCommandFactory(Func<EffectCommandBase> createCommand)
        {
            this.createCommand = createCommand ?? throw new ArgumentNullException(nameof(createCommand));
        }

        public override EffectCommandBase Create()
        {
            return createCommand();
        }
    }
}
