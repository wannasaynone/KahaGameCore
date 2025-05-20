using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level
{
    public class SpawnPoint : MonoBehaviour
    {
        public string FromRoomObjectName => fromRoomObjectName;
        [SerializeField] private string fromRoomObjectName = "";
    }
}