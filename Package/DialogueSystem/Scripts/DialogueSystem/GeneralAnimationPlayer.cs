using DG.Tweening;
using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class GeneralAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;
        [SerializeField] private SpriteRenderer m_spriteRenderer;

        public void PlayAnimation(string animationName)
        {
            if (m_animator != null)
            {
                m_animator.Play(animationName);
            }
        }

        public void FadeOut(float duration)
        {
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.DOFade(0, duration);
            }
        }
    }
}