using KahaGameCore.Common;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    [System.Serializable]
    public class AttackInfo : MonoBehaviour, IAttackInfo
    {
        [Space(10)]
        [Header("範圍爆炸傷害相關")]
        [Tooltip("設定是否啟用範圍爆炸傷害。")]
        [Rename("啟用範圍傷害")] public bool enableAreaDamage;
        [Tooltip("設定爆炸傷害的半徑範圍。")]
        [Rename("爆炸半徑")] public float explosionRadius = 2f;
        [Tooltip("設定範圍傷害是否影響弱點。")]
        [Rename("範圍傷害影響弱點")] public bool areaAffectWeakPoints = true;
        [Tooltip("觸發本攻擊時會播放的動畫。")]
        [Rename("攻擊動畫")] public string animationName;
        [Tooltip("可選的動畫控制器。如果設置，在播放此攻擊動畫時會臨時切換到此控制器。")]
        [Rename("動畫控制器")] public RuntimeAnimatorController animatorController;
        [Tooltip("施展這個攻擊時需要消耗多少體力，不足消耗時攻擊會失敗")]
        [Rename("消耗體力")] public float staminaCost;
        [Tooltip("施展這個攻擊時需要消耗多少生命值，不足消耗時攻擊會失敗")]
        [Rename("消耗生命值")] public int healthCost;
        [Tooltip("設定子彈產生時是否讓遊戲產生時間暫停的效果。")]
        [Rename("產生子彈時暫停")] public bool pauseWhenCreateBullet;
        [Tooltip("設定產生子彈時產生時間暫停的持續時間。")]
        [Rename("子彈產生時暫停的持續時間")] public float pauseDurationWhenCreateBullet;
        [Tooltip("攻擊的持續時間，持續時間結束後回到待機狀態，並讓連段重置。")]
        [Rename("攻擊持續時間")] public float duration;
        [Tooltip("設定多少時間後允許下一個攻擊輸入，在這個時間之前無視攻擊輸入。設定的時間大於等於攻擊持續時間會使連朝設置失效。")]
        [Rename("允許攻擊輸入時間")] public float allowNextAttackTime;
        [Tooltip("設定子彈產生的時間。")]
        [Rename("子彈產生時間")] public float createBulletTime;
        [Tooltip("允許任何類型的GameObject填入以增加功能泛用性。")]
        [Rename("子彈Prefab")] public GameObject bulletPrefab;
        [Tooltip("設定子彈產生的位置。")]
        [Rename("發射點")] public Transform bulletSpawnPoint;
        [Tooltip("設定傷害倍率，原始傷害=Actor的攻擊值*傷害倍率。")]
        [Rename("傷害倍率")] public float mutiply = 1f;
        [Tooltip("設定當子彈擊中目標，目標後退的持續時間。攻擊一定會造成擊退，要關閉的話可以將此值設為0。")]
        [Rename("擊退力持續時間")] public float hitForce_duration;
        [Tooltip("設定當子彈擊中目標，目標後退的每單位時間移動多少。")]
        [Rename("擊退力")] public float hitForce_power;
        [Tooltip("設定是否為重攻擊。目標受到重攻擊擊中時將會被擊飛。")]
        [Rename("是否為重攻擊")] public bool isHeavyHit;
        [Tooltip("設定子彈擊中敵人時是否產生時間暫停的效果。")]
        [Rename("子彈擊中時暫停")] public bool pauseWhenHit;
        [Tooltip("設定子彈擊中敵人時產生時間暫停的持續時間。")]
        [Rename("擊中時暫停的持續時間")] public float pauseDurationWhenHit;
        [Tooltip("設定子彈擊中時震動攝影機幅度。大於0時才有效果。")]
        [Rename("子彈擊中時震動攝影機")] public float shakeWhenHit;
        [Tooltip("設定是否允許子彈擊中WEAK POINT時產生爆擊。子彈的中心點在敵人爆擊Y範圍內時產生爆擊。")]
        [Rename("允許爆擊")] public bool allowCritical;
        [Tooltip("設定產生爆擊時的傷害，傷害=原始傷害*爆擊倍率。")]
        [Rename("爆擊倍率")] public float criticalMutiply = 1f;
        [Tooltip("設定子彈爆擊敵人時是否產生時間暫停的效果。")]
        [Rename("子彈爆擊時暫停")] public bool pauseWheCritical;
        [Tooltip("設定子彈爆擊敵人時產生時間暫停的持續時間。如果同時有填寫擊中時暫停的持續時間，則會疊加上去。")]
        [Rename("擊爆擊時暫停的持續時間")] public float pauseDurationWhenCritical;
        [Tooltip("設定爆擊時震動攝影機幅度。大於0時才有效果。")]
        [Rename("爆擊時震動攝影機")] public float shakeWhenCritical;
        [Space(10)]
        [Header("準備攻擊相關")]
        [Header("當準備攻擊動畫有填值時才會生效，表示這個攻擊要先進行「準備」操作才能發動。目前預設為要按住滑鼠右鍵。")]
        [Tooltip("設定按住準備攻擊鍵、待機時會播放的動畫。")]
        [Rename("準備攻擊動畫")] public string prepareAnimationName;
        [Rename("準備攻擊時變更的camera offset x")] public float cameraOffset_prepareAttack = 2f;
        [Tooltip("設定按住準備攻擊鍵時要開啟的物件。")]
        [Rename("準備攻擊時開啟的物件")] public GameObject enableWhenPrepare;
        [Tooltip("在準備攻擊狀態下普通移動時要播放的動畫。面向與前進方向相同時播放這個。")]
        [Rename("準備攻擊時移動動畫")] public string prepareAndMoveAnimationName;
        [Tooltip("在準備攻擊狀態下普通移動時要播放的動畫。面向與前進方向相反時播放這個。")]
        [Rename("準備攻擊時移動動畫(反向)")] public string prepareAndMoveAnimationName_Reverse;
        [Tooltip("在準備攻擊狀態下普通移動時，增加多少移動速度。")]
        [Rename("準備攻擊時移動速度增加")] public float addMoveSpeedWhenPrepare;
        [Space(10)]
        [Header("可不填")]
        [Tooltip("設定發射時產生的火花。可以不填。")]
        [Rename("火花Prefab")] public GameObject muzzlePrefab;
        [Tooltip("設定攻擊時要開啟的物件。可以不填。")]
        [Rename("攻擊時開啟的物件")] public GameObject enableWhenAttacking;
        [Space(10)]
        [Header("AI 設置相關")]
        [Space(10)]
        [Tooltip("設定AI在進行這個攻擊時，要先移動到多遠的距離才開始進行攻擊。")]
        [Rename("AI攻擊距離")] public float startAttackDistance;

        public string GetAnimationName() => animationName;

        public string GetPrepareAnimationName() => prepareAnimationName;

        public string GetPrepareAndMoveAnimationName(bool isReverse = false) =>
            isReverse ? prepareAndMoveAnimationName_Reverse : prepareAndMoveAnimationName;

        public float GetStaminaCost() => staminaCost;

        public int GetHealthCost() => healthCost;

        public float GetDuration() => duration;

        public float GetAllowNextAttackTime() => allowNextAttackTime;

        public float GetCreateBulletTime() => createBulletTime;

        public GameObject GetBulletPrefab() => bulletPrefab;

        public Transform GetBulletSpawnPoint() => bulletSpawnPoint;

        public GameObject GetMuzzlePrefab() => muzzlePrefab;

        public float GetMutiply() => mutiply;

        public float GetHitForceDuration() => hitForce_duration;

        public float GetHitForcePower() => hitForce_power;

        public bool IsHeavyHit() => isHeavyHit;

        public bool ShouldPauseWhenCreateBullet() => pauseWhenCreateBullet;

        public float GetPauseDurationWhenCreateBullet() => pauseDurationWhenCreateBullet;

        public bool ShouldPauseWhenHit() => pauseWhenHit;

        public float GetPauseDurationWhenHit() => pauseDurationWhenHit;

        public bool ShouldPauseWhenCritical() => pauseWheCritical;

        public float GetPauseDurationWhenCritical() => pauseDurationWhenCritical;

        public GameObject GetEnableWhenPrepare() => enableWhenPrepare;

        public GameObject GetEnableWhenAttacking() => enableWhenAttacking;

        public float GetCameraOffsetPrepareAttack() => cameraOffset_prepareAttack;

        public float GetAddMoveSpeedWhenPrepare() => addMoveSpeedWhenPrepare;

        public bool IsAreaDamageEnabled() => enableAreaDamage;

        public float GetExplosionRadius() => explosionRadius;

        public bool DoesAreaAffectWeakPoints() => areaAffectWeakPoints;

        public bool AllowCritical() => allowCritical;

        public float GetCriticalMultiply() => criticalMutiply;

        public float GetShakeWhenHit() => shakeWhenHit;

        public float GetShakeWhenCritical() => shakeWhenCritical;

        public RuntimeAnimatorController GetAnimatorController() => animatorController;
    }
}
