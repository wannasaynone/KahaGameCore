using System.Collections.Generic;
using KahaGameCore.Package.SideScrollerActor.View;

namespace KahaGameCore.Package.SideScrollerActor.Game.Flow
{
    public class FlowContext
    {
        public CombatState_LevelController LevelController { get; set; }
        public InGameView InGameView { get; set; }
        public TitleView TitleView { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
    }
}
