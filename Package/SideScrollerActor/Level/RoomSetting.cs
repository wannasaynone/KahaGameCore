using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Level
{
    public class RoomSetting : MonoBehaviour
    {
        public Transform SpawnPoint => spawnPoint;
        [SerializeField] private Transform spawnPoint;
        public Transform BoardTransform_min => boardTransform_min;
        [SerializeField] private Transform boardTransform_min;
        public Transform BoardTransform_max => boardTransform_max;
        [SerializeField] private Transform boardTransform_max;
        public bool EnableWhiteNoise => enableWhiteNoise;
        [SerializeField] private bool enableWhiteNoise = false;
#if USING_URP
        public VolumeProfile VolumeProfile => volumeProfile;
        [SerializeField] private VolumeProfile volumeProfile;
#endif
        public AudioClip BackgroundMusic => backgroundMusic;
        [SerializeField] private AudioClip backgroundMusic;

        private void OnEnable()
        {
            if (boardTransform_min == null)
            {
                Debug.LogError("RoomSetting boardTransform_min is not assigned. name: " + gameObject.name);
            }

            if (boardTransform_max == null)
            {
                Debug.LogError("RoomSetting boardTransform_max is not assigned. name: " + gameObject.name);
            }
        }
    }
}