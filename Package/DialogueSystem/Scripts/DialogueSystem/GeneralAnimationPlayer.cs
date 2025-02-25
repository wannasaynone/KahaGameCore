using UnityEngine;

namespace KahaGameCore.Package.DialogueSystem
{
    public class GeneralAnimationPlayer : MonoBehaviour
    {
        [SerializeField] private Animator m_animator;

        public void PlayAnimation(string animationName)
        {
            if (m_animator != null)
            {
                m_animator.Play(animationName);
            }
        }
    }
}