using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level
{
    public class RoomSetting : MonoBehaviour
    {
        public Transform BoardTransform_min => boardTransform_min;
        private Transform boardTransform_min;
        public Transform BoardTransform_max => boardTransform_max;
        private Transform boardTransform_max;
        public bool EnableWhiteNoise => enableWhiteNoise;
        [SerializeField] private bool enableWhiteNoise = false;
#if USING_URP
        public VolumeProfile VolumeProfile => volumeProfile;
        [SerializeField] private VolumeProfile volumeProfile;
#endif
        public AudioClip BackgroundMusic => backgroundMusic;
        [SerializeField] private AudioClip backgroundMusic;

        private SpawnPoint[] spawnPoints;

        private void OnEnable()
        {
            boardTransform_min = transform.Find("BoderHint_Min");
            boardTransform_max = transform.Find("BoderHint_Max");

            if (boardTransform_min == null)
            {
                Debug.LogError("Should have a BoderHint_Min object in RoomSetting. name: " + gameObject.name);
            }

            if (boardTransform_max == null)
            {
                Debug.LogError("Should have a BoderHint_Max object in RoomSetting. name: " + gameObject.name);
            }

            spawnPoints = GetComponentsInChildren<SpawnPoint>();
        }

        public SpawnPoint GetSpawnPointByFromRoomName(string roomName)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint.FromRoomObjectName == roomName)
                {
                    return spawnPoint;
                }
            }

            Debug.LogError("Spawn point not found for \"from room\" name: [" + roomName + "] in " + gameObject.name);
            return null;
        }
    }
}