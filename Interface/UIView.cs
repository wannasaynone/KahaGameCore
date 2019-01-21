using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KahaGameCore.View.UI
{
    public abstract class UIView : View
    {
        public abstract bool IsEnable { get; }
        public abstract void EnablePage(Manager.Manager manager, bool enable);
    }
}
