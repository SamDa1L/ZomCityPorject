using System.Collections;
using UnityEngine;
using ZomCity.Gameplay.Pool;

namespace ZomCity.Gameplay.Debug
{
    /// <summary>
    /// Pool 冒烟测试驱动脚本（M0 测试场景用）。
    /// 挂载到测试场景任意 GameObject，配置 TestPrefab 后进入 PlayMode 即可验证：
    ///   Spawn → 使用（等待） → Despawn（手动）
    ///   Spawn → 使用（等待） → DespawnAfter（AutoDespawnDelay 自动归还）
    ///
    /// 使用前提：
    ///   1. 场景中存在 ZomCityObjectPool 单例（或由本脚本自动创建）
    ///   2. TestPrefab 根节点挂载 PoolableVFX（AutoDespawnDelay > 0 时自动归还）
    ///   3. 或 TestPrefab 为任意 GameObject（手动 Despawn 路径）
    /// </summary>
    [AddComponentMenu("ZomCity/Debug/Pool Smoke Test")]
    public sealed class PoolSmokeTest : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("用于测试的 Prefab（直接拖入，不走 Addressables，仅验证 Pool 接口闭环）")]
        public GameObject TestPrefab;

        [Tooltip("Spawn 位置偏移（相对于本 GameObject）")]
        public Vector3 SpawnOffset = new Vector3(0f, 1f, 0f);

        [Tooltip("手动 Despawn 延迟（秒）。0 = 依赖 PoolableVFX.AutoDespawnDelay 自动归还")]
        [Min(0f)]
        public float ManualDespawnDelay = 2f;

        [Tooltip("循环测试间隔（秒）。0 = 仅执行一次")]
        [Min(0f)]
        public float LoopInterval = 3f;

        private const string TestAddress = "ZC/Debug/POOL_SMOKE_TEST";

        private ZomCityObjectPool _pool;
        private bool _registered;

        // ── Unity 生命周期 ────────────────────────────────────────────────────

        private void Awake()
        {
            // 若场景中尚无 Pool 单例，自动创建（仅测试场景兜底）
            if (ZomCityObjectPool.Instance == null)
            {
                var poolGO = new GameObject("[ZomCityObjectPool]");
                poolGO.AddComponent<ZomCityObjectPool>();
                UnityEngine.Debug.Log("[PoolSmokeTest] 自动创建 ZomCityObjectPool 单例。");
            }

            _pool = ZomCityObjectPool.Instance;
        }

        private void Start()
        {
            if (TestPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[PoolSmokeTest] TestPrefab 未配置，跳过测试。");
                return;
            }

            // 注册 Prefab 到 Pool（替代 Addressables 加载，直接注入）
            _pool.RegisterPrefab(TestAddress, TestPrefab);
            _pool.Prewarm(TestAddress, 4);
            _registered = true;

            UnityEngine.Debug.Log($"[PoolSmokeTest] RegisterPrefab + Prewarm 完成，address={TestAddress}");

            if (LoopInterval > 0f)
            {
                StartCoroutine(LoopTest());
            }
            else
            {
                StartCoroutine(RunOnce());
            }
        }

        // ── 测试协程 ──────────────────────────────────────────────────────────

        /// <summary>循环执行 Spawn → Despawn 闭环。</summary>
        private IEnumerator LoopTest()
        {
            while (true)
            {
                yield return RunOnce();
                yield return new WaitForSeconds(LoopInterval);
            }
        }

        /// <summary>
        /// 单次执行完整闭环：
        ///   Case A（ManualDespawnDelay > 0）：Spawn → 等待 → 手动 Despawn
        ///   Case B（ManualDespawnDelay == 0）：Spawn → 依赖 PoolableVFX.AutoDespawnDelay 自动归还
        /// </summary>
        private IEnumerator RunOnce()
        {
            if (!_registered)
            {
                yield break;
            }

            var spawnPos = transform.position + SpawnOffset;

            // ── Spawn ──────────────────────────────────────────────────────
            var obj = _pool.Spawn(TestAddress, spawnPos, Quaternion.identity);

            if (obj == null)
            {
                UnityEngine.Debug.LogError("[PoolSmokeTest] Spawn 返回 null，Pool 未正确注册 Prefab。");
                yield break;
            }

            var aliveAfterSpawn = _pool.GetAliveCount(TestAddress);
            UnityEngine.Debug.Log(
                $"[PoolSmokeTest] Spawn OK  address={TestAddress} " +
                $"obj={obj.name} alive={aliveAfterSpawn}");

            // ── 使用（等待） ───────────────────────────────────────────────
            if (ManualDespawnDelay > 0f)
            {
                yield return new WaitForSeconds(ManualDespawnDelay);

                // ── 手动 Despawn ───────────────────────────────────────────
                _pool.Despawn(obj);

                var aliveAfterDespawn = _pool.GetAliveCount(TestAddress);
                UnityEngine.Debug.Log(
                    $"[PoolSmokeTest] Despawn OK  address={TestAddress} " +
                    $"alive={aliveAfterDespawn}");
            }
            else
            {
                // Case B：依赖 PoolableVFX.AutoDespawnDelay 自动归还
                UnityEngine.Debug.Log(
                    $"[PoolSmokeTest] DespawnAfter 路径：依赖 PoolableVFX.AutoDespawnDelay 自动归还，" +
                    $"address={TestAddress} alive={aliveAfterSpawn}");
            }
        }
    }
}
