using KahaGameCore.Common;
using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.Gameplay
{
    [System.Serializable]
    public class ActorSetting
    {
        [Tooltip("歸零後死亡")]
        [Rename("生命值")] public int health;
        [Tooltip("根據設置，部分操作會消耗體力值，不足消耗時，無法執行操作。")]
        [Rename("體力值")] public int stamina;
        [Tooltip("每秒恢復體力值，但不是固定每秒觸發增加定值，是每個delta time都會等比逐漸恢復。")]
        [Rename("每秒恢復體力值")] public int restoreStaminaPerSecond;
        [Tooltip("傷害計算時會用到，(攻擊值*攻擊倍率)-防禦值=扣除生命值，最少會-1。")]
        [Rename("攻擊值")] public int attack;
        [Tooltip("擊中時減少目標體力，允許負數=可以幫敵人增加體力")]
        [Rename("擊中時減少目標體力")] public int deductTargetStamina;
        [Tooltip("傷害計算時會用到，(攻擊值*攻擊倍率)-防禦值=扣除生命值，最少會-1。")]
        [Rename("防禦值")] public int defense;
        [Tooltip("普通移動時的速度")]
        [Rename("移動速度")] public float moveSpeed;
        [Tooltip("倒退移動時的移動速度")]
        [Rename("倒退速度")] public float backSpeed;
        [Tooltip("跑步時消耗多少體力，但不是固定每秒觸發增加定值，是每個delta time都會等比逐漸減少。")]
        [Rename("跑步消耗體力")] public float runCostPerSecond;
        [Tooltip("跑步移動時的速度。")]
        [Rename("跑步速度")] public float runSpeed;
        [Tooltip("衝刺時的移動速度，衝刺長度=衝刺速度*衝刺持續時間。")]
        [Rename("衝刺速度")] public float dashSpeed;
        [Tooltip("衝刺持續時間，衝刺長度=衝刺速度*衝刺持續時間。")]
        [Rename("衝刺持續時間")] public float dashDuration = 0.5f;
        [Tooltip("衝刺冷卻時間，衝刺冷卻時間內無法再次衝刺。")]
        [Rename("衝刺冷卻時間")] public float dashCooldown = 0.5f;
        [Tooltip("按下跳躍鍵後要經過準備跳躍時間後才會真的起跳。")]
        [Rename("準備跳躍時間")] public float readyJumpTime = 0.1f;
        [Tooltip("跳躍時增加多少高度，1=遊戲內一單位")]
        [Rename("跳躍高度")] public float simpleJumpAddY;
        [Tooltip("從跳起到最高處的持續時間。")]
        [Rename("跳躍持續時間")] public float simpleJumpDuration;
        [Tooltip("下落速度，每秒下降多少高度。")]
        [Rename("下落速度")] public float fallSpeed = 1f;
        [Tooltip("受傷後倒地持續時間，倒地時間結束後會自動站起來。")]
        [Rename("倒地持續時間")] public float downDuration = 0.5f;
        [Tooltip("防禦持續時間，持續時間內受到攻擊會觸發Perfect Guard，Perfect Guard會將攻擊者擊飛。")]
        [Rename("防禦持續時間")] public float defenseDuration = 0.5f;
    }
}