using System.Collections;
using UnityEngine;

namespace ZomCity.Gameplay.Pool
{
    /// <summary>
    /// VFX Prefab 对象池基类。
    /// 美术在 VFX Prefab 根节点挂载此组件，程序通过 ZomCityObjectPool 管理生命周期。
    /// 禁止在 VFX Prefab 内调用 Destroy，禁止 Play On Awake，禁止 Stop Action = Destroy。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoolableVFX : MonoBehaviour, IPoolable
    {
        /// <summary>特效自动归还延迟（秒）。0 表示不自动归还，由外部调用 Despawn。</summary>
        [Min(0f)]
        public float AutoDespawnDelay = 0f;

        private ParticleSystem[] _particles;
        private Coroutine _autoDespawnRoutine;

        private void Awake()
        {
            // 缓存所有子节点 ParticleSystem，避免运行时 GetComponentsInChildren 开销
            _particles = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
        }

        /// <summary>
        /// 从池中取出时由 ZomCityObjectPool 调用。
        /// 重置并播放所有粒子系统。
        /// </summary>
        public void OnSpawn()
        {
            // 停止并清空残留粒子，再重新播放
            for (var i = 0; i < _particles.Length; i++)
            {
                _particles[i].Stop(withChildren: false, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
                _particles[i].Play(withChildren: false);
            }

            // 如果配置了自动归还延迟，启动协程
            if (AutoDespawnDelay > 0f)
            {
                if (_autoDespawnRoutine != null)
                {
                    StopCoroutine(_autoDespawnRoutine);
                }
                _autoDespawnRoutine = StartCoroutine(AutoDespawnAfterDelay());
            }
        }

        /// <summary>
        /// 归还到池时由 ZomCityObjectPool 调用。
        /// 停止所有粒子系统并隐藏对象。
        /// </summary>
        public void OnDespawn()
        {
            // 取消自动归还协程
            if (_autoDespawnRoutine != null)
            {
                StopCoroutine(_autoDespawnRoutine);
                _autoDespawnRoutine = null;
            }

            // 停止并清空所有粒子
            for (var i = 0; i < _particles.Length; i++)
            {
                _particles[i].Stop(withChildren: false, stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private IEnumerator AutoDespawnAfterDelay()
        {
            yield return new WaitForSeconds(AutoDespawnDelay);
            _autoDespawnRoutine = null;
            ZomCityObjectPool.Instance.Despawn(gameObject);
        }
    }
}
