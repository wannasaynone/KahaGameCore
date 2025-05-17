using UnityEngine;

namespace KahaGameCore.Package.SideScrollerActor.WeaponScripts
{
    public class MeleeWeapon : Weapon
    {
        private const float RESUME_ATTACK_TIME = 0.35f;

        [Header("近戰武器允許製作連段攻擊，陣列表示連續輸入攻擊操作可以進行連段，連段設置在AttackInfo身上")]
        [SerializeField] private AttackInfo[] attackInfos;

        private int currentAttackIndex = -1;
        private float resumeAttackIndexTimer = 0f;
        private float attackTimer = 0f;

        public override AttackInfo PeekNextAttackInfo()
        {
            if (attackInfos == null || attackInfos.Length == 0)
            {
                Debug.LogError("No attack info found in Weapon: " + gameObject.name);
                return null;
            }

            int nextAttackIndex = currentAttackIndex + 1;
            if (nextAttackIndex >= attackInfos.Length)
            {
                nextAttackIndex = 0;
            }

            return attackInfos[nextAttackIndex];
        }

        public override AttackInfo GetNextAttackInfo()
        {
            if (attackInfos == null || attackInfos.Length == 0)
            {
                Debug.LogError("No attack info found in Weapon: " + gameObject.name);
                return null;
            }

            if (currentAttackIndex != -1 && attackTimer < attackInfos[currentAttackIndex].allowNextAttackTime)
            {
                return null;
            }

            currentAttackIndex++;
            if (currentAttackIndex >= attackInfos.Length)
            {
                currentAttackIndex = 0;
            }

            AttackInfo attackInfo = attackInfos[currentAttackIndex];
            attackTimer = 0f;
            resumeAttackIndexTimer = RESUME_ATTACK_TIME + attackInfo.duration;

            return attackInfo;
        }

        private void Update()
        {
            attackTimer += Time.deltaTime;

            if (resumeAttackIndexTimer > 0)
            {
                resumeAttackIndexTimer -= Time.deltaTime;
                if (resumeAttackIndexTimer <= 0)
                {
                    currentAttackIndex = -1;
                }
            }
        }
    }
}