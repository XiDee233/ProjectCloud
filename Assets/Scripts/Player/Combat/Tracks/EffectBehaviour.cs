using UnityEngine;
using UnityEngine.Playables;

namespace Player.Combat.Tracks
{
    [System.Serializable]
    public class EffectBehaviour : PlayableBehaviour
    {
        [Header("特效参数")]
        public GameObject effectPrefab;
        public Vector3 spawnOffset;
        public bool destroyOnEnd = true;

        private GameObject _spawnedEffect;
        private bool _hasSpawned = false;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _hasSpawned = false;
            _spawnedEffect = null;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Transform spawnPoint = playerData as Transform;
            if (spawnPoint == null) return;

            double time = playable.GetTime();
            double duration = playable.GetDuration();

            // 检测是否应该生成特效
            if (time >= 0 && !_hasSpawned && effectPrefab != null && info.weight > 0f)
            {
                Vector3 spawnPosition = spawnPoint.position + spawnPoint.TransformDirection(spawnOffset);
                Quaternion spawnRotation = spawnPoint.rotation;
                _spawnedEffect = Object.Instantiate(effectPrefab, spawnPosition, spawnRotation);
                _hasSpawned = true;
            }

            // 检测是否应该销毁特效
            if (destroyOnEnd && time >= duration && _spawnedEffect != null)
            {
                Object.Destroy(_spawnedEffect);
                _spawnedEffect = null;
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (destroyOnEnd && _spawnedEffect != null)
            {
                Object.Destroy(_spawnedEffect);
                _spawnedEffect = null;
            }
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_spawnedEffect != null)
            {
                Object.Destroy(_spawnedEffect);
                _spawnedEffect = null;
            }
        }
    }
}
