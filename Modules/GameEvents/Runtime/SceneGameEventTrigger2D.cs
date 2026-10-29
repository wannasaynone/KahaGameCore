using KahaGameCore.Interaction;
using UnityEngine;

namespace KahaGameCore.GameEvents
{
    /// <summary>Runs a Game Event from an Interactable2D on the same GameObject.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Interactable2D))]
    public sealed class SceneGameEventTrigger2D : SceneGameEventTriggerBase<Interactable2D, Collider2D>
    {
    }
}
