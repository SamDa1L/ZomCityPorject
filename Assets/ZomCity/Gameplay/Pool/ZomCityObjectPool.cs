using System.Collections.Generic;
using UnityEngine;

namespace ZomCity.Gameplay.Pool
{
    /// <summary>
    /// ZomCity 对象池主入口（单例）。
    /// 覆盖范围：Projectile / HitVFX / MuzzleFlash / FloatingText / LootDrop / NoiseIndicator。
    /// 禁止在任何玩法代码中直接调用 Instantiate / Destroy，必须通过本类的 Spawn / Despawn。
    ///
    /// 加载职责说明：
    ///   本类不直接依赖 Addressables。Prefab 加载由调用方通过 AddressablesLoader.LoadPrefabAsync
    ///   完成后，调用 RegisterPrefab(address, prefab) 注入，再调用 Prewarm / Spawn。
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    [DisallowMultipleComponent]
    public sealed class ZomCityObjectPool : MonoBehaviour
    {
        // ── 单例 ──────────────────────────────────────────────────────────────

        public static ZomCityObjectPool Instance { get; private set; }

        // ── MVP 预算常量（写死，来源：M0_Blocker_ObjectPoolSpec_v1.0.0.md）────

        private static readonly PoolBudget[] Budgets =
        {
            new PoolBudget("Projectile",    poolSize: 64,  maxAlivePerRoom: 128),
            new PoolBudget("HitVFX",        poolSize: 32,  maxAlivePerRoom: 64),
            new PoolBudget("MuzzleFlash",   poolSize: 16,  maxAlivePerRoom: 32),
            new PoolBudget("FloatingText",  poolSize: 24,  maxAlivePerRoom: 48),
            new PoolBudget("LootDrop",      poolSize: 32,  maxAlivePerRoom: 64),
            new PoolBudget("NoiseIndicator",poolSize: 16,  maxAlivePerRoom: 32),
        };

        // ── 内部数据结构 ──────────────────────────────────────────────────────

        /// <summary>每个 Address 对应一个独立的对象池桶。</summary>
        private readonly Dictionary<string, PoolBucket> _buckets = new Dictionary<string, PoolBucket>();

        /// <summary>已存活对象 → 所属桶的反向映射（用于 Despawn 时快速定位）。</summary>
        private readonly Dictionary<GameObject, PoolBucket> _aliveToBucket = new Dictionary<GameObject, PoolBucket>();

        // ── Unity 生命周期 ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ── 公开接口 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 注册已加载的 Prefab 到指定 address 的桶。
        /// 调用方须先通过 AddressablesLoader.LoadPrefabAsync 加载完成后再调用本方法。
        /// </summary>
        public void RegisterPrefab(string address, GameObject prefab)
        {
            if (prefab == null)
            {
                UnityEngine.Debug.LogError($"[ZomCityObjectPool] RegisterPrefab 收到 null prefab，address={address}");
                return;
            }

            if (!_buckets.TryGetValue(address, out var bucket))
            {
                bucket = CreateBucket(address);
            }

            bucket.SetPrefab(prefab);
        }

        /// <summary>
        /// 预热：填充空闲队列到 initialCount。
        /// 必须在 RegisterPrefab 之后调用，否则无效。
        /// </summary>
        public void Prewarm(string address, int initialCount)
        {
            if (!_buckets.TryGetValue(address, out var bucket))
            {
                bucket = CreateBucket(address);
            }

            bucket.Prewarm(initialCount);
        }

        /// <summary>
        /// 从池中取出一个实例。
        /// Prefab 必须已通过 RegisterPrefab 注入，否则返回 null 并记录错误。
        /// </summary>
        public GameObject Spawn(string address, Vector3 position, Quaternion rotation)
        {
            if (!_buckets.TryGetValue(address, out var bucket))
            {
                bucket = CreateBucket(address);
            }

            return bucket.Spawn(position, rotation, _aliveToBucket);
        }

        /// <summary>
        /// 归还实例到池。禁止在归还后继续访问该对象。
        /// </summary>
        public void Despawn(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!_aliveToBucket.TryGetValue(obj, out var bucket))
            {
                // 不属于任何池的对象，直接销毁并记录警告
                UnityEngine.Debug.LogWarning($"[ZomCityObjectPool] Despawn 收到非池对象：{obj.name}，直接销毁。");
                Destroy(obj);
                return;
            }

            _aliveToBucket.Remove(obj);
            bucket.Despawn(obj);
        }

        /// <summary>
        /// 获取指定 address 当前存活数量（用于 Debug 面板与预算校验）。
        /// </summary>
        public int GetAliveCount(string address)
        {
            return _buckets.TryGetValue(address, out var bucket) ? bucket.AliveCount : 0;
        }

        // ── 内部方法 ──────────────────────────────────────────────────────────

        private PoolBucket CreateBucket(string address)
        {
            var budget = FindBudget(address);
            var bucket = new PoolBucket(address, budget, transform);
            _buckets[address] = bucket;
            return bucket;
        }

        private static PoolBudget FindBudget(string address)
        {
            // 按 address 中的 Domain 段匹配预算（例如 ZC/VFX/VFX_HIT_SPARK → HitVFX）
            for (var i = 0; i < Budgets.Length; i++)
            {
                if (address.Contains(Budgets[i].Category))
                {
                    return Budgets[i];
                }
            }

            // 未匹配到预算时使用保守默认值
            return new PoolBudget("Default", poolSize: 16, maxAlivePerRoom: 32);
        }

        // ── 内部数据类型 ──────────────────────────────────────────────────────

        private readonly struct PoolBudget
        {
            public readonly string Category;
            public readonly int PoolSize;
            public readonly int MaxAlivePerRoom;

            public PoolBudget(string category, int poolSize, int maxAlivePerRoom)
            {
                Category = category;
                PoolSize = poolSize;
                MaxAlivePerRoom = maxAlivePerRoom;
            }
        }

        /// <summary>
        /// 单个 Address 的对象池桶。
        /// 管理空闲队列、存活计数、超限降级策略。
        /// Prefab 由外部通过 SetPrefab 注入，本桶不持有任何 Addressables 引用。
        /// </summary>
        private sealed class PoolBucket
        {
            private readonly string _address;
            private readonly PoolBudget _budget;
            private readonly Transform _poolRoot;
            private readonly Queue<GameObject> _free = new Queue<GameObject>();

            // 存活对象列表（用于超限时回收最旧实例）
            private readonly List<GameObject> _alive = new List<GameObject>();

            private GameObject _prefab;

            public int AliveCount => _alive.Count;

            public PoolBucket(string address, PoolBudget budget, Transform poolRoot)
            {
                _address = address;
                _budget = budget;
                _poolRoot = poolRoot;
            }

            /// <summary>由外部（AddressablesLoader 加载完成后）注入 Prefab。</summary>
            public void SetPrefab(GameObject prefab)
            {
                _prefab = prefab;
            }

            public void Prewarm(int count)
            {
                if (_prefab == null)
                {
                    UnityEngine.Debug.LogWarning($"[ZomCityObjectPool] Prewarm 跳过：{_address} Prefab 尚未注入，请先调用 RegisterPrefab。");
                    return;
                }

                for (var i = 0; i < count && _free.Count < _budget.PoolSize; i++)
                {
                    var obj = CreateInstance();
                    if (obj != null)
                    {
                        _free.Enqueue(obj);
                    }
                }
            }

            public GameObject Spawn(
                Vector3 position,
                Quaternion rotation,
                Dictionary<GameObject, PoolBucket> aliveToBucket)
            {
                // 超出 MaxAlivePerRoom：降级策略——回收最旧存活实例
                if (_alive.Count >= _budget.MaxAlivePerRoom)
                {
                    var oldest = _alive[0];
                    _alive.RemoveAt(0);
                    aliveToBucket.Remove(oldest);
                    ReturnToFree(oldest);

                    UnityEngine.Debug.LogWarning(
                        $"[ZomCityObjectPool] {_address} 超出 MaxAlivePerRoom({_budget.MaxAlivePerRoom})，" +
                        $"回收最旧实例降级。");
                }

                GameObject obj;

                if (_free.Count > 0)
                {
                    obj = _free.Dequeue();
                }
                else
                {
                    // 空闲队列耗尽：同步创建（仅开发阶段兜底，生产阶段应提前 Prewarm）
                    obj = CreateInstance();
                    if (obj == null)
                    {
                        UnityEngine.Debug.LogError($"[ZomCityObjectPool] Spawn 失败：{_address} Prefab 未注入，请先调用 RegisterPrefab。");
                        return null;
                    }
                }

                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);

                // 通知 IPoolable 组件
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnSpawn();

                _alive.Add(obj);
                aliveToBucket[obj] = this;
                return obj;
            }

            public void Despawn(GameObject obj)
            {
                _alive.Remove(obj);
                ReturnToFree(obj);
            }

            private void ReturnToFree(GameObject obj)
            {
                // 通知 IPoolable 组件
                var poolable = obj.GetComponent<IPoolable>();
                poolable?.OnDespawn();

                obj.SetActive(false);
                obj.transform.SetParent(_poolRoot, worldPositionStays: false);

                // 超出 PoolSize 上限时直接销毁（避免池无限膨胀）
                if (_free.Count >= _budget.PoolSize)
                {
                    Object.Destroy(obj);
                    return;
                }

                _free.Enqueue(obj);
            }

            private GameObject CreateInstance()
            {
                if (_prefab == null)
                {
                    return null;
                }

                var obj = Object.Instantiate(_prefab, _poolRoot);
                obj.SetActive(false);
                return obj;
            }
        }
    }
}
