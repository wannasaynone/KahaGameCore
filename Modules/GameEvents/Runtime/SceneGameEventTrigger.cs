using KahaGameCore.Interaction;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>Runs a Game Event from an Interactable3D on the same GameObject.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable3D))]
    public sealed class SceneGameEventTrigger : SceneGameEventTriggerBase<Interactable3D, Collider>
    {
    }
}
