using UnityEngine;
using System;

namespace Player.Animation
{
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Player/Animation/Animation Controller")]
    public class AnimationController : MonoBehaviour
    {
        private Animator animator;

        public Animator Animator => animator;

        private void Awake()
        {
            if (!animator) animator = GetComponent<Animator>();
        }

        public void Play(string stateName, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            int hash = AnimationStates.GetHash(stateName);
            Play(hash, layer, normalizedTime);
        }

        public void Play(int stateHash, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            animator.Play(stateHash, layer, normalizedTime);
        }

        public void CrossFade(string stateName, float duration, int layer = -1, float normalizedTimeOffset = float.NegativeInfinity)
        {
            animator.CrossFade(stateName, duration, layer, normalizedTimeOffset);
        }

        public void SetFloat(string name, float value)
        {
            animator.SetFloat(name, value);
        }

        public void SetBool(string name, bool value)
        {
            animator.SetBool(name, value);
        }

        public void SetTrigger(string name)
        {
            animator.SetTrigger(name);
        }

        public bool IsPlaying(int stateHash, int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == stateHash;
        }

        public float GetCurrentStateTime(int layer = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
        }
    }
}
