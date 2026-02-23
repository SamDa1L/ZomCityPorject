using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    /// <summary>
    /// 纯代码 Boot 钩子（M0.1）。
    /// 启动时冻结 Time 和 FramePacing 参数并创建 Guard。
    /// </summary>
    public static class ZomCityBootRuntime
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var go = new GameObject("[ZomCity]BootInitRunner");
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<ZomCityBootInitRunner>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }

        private sealed class ZomCityBootInitRunner : MonoBehaviour
        {
            private IEnumerator Start()
            {
                // 延后一帧初始化，避开场景备份恢复阶段。
                yield return null;

                ZomCityTimeSettingsAuthority.ApplyAuthoritative();
                TimeSettingsGuard.EnsureExists();
                LayerTagCatalogGuard.EnsureExists();
                GameplayPlaneConstraint.EnsureExists();
                GameplayEventViewer.EnsureExists();
                ZomCityTestSceneBootstrap.EnsureExists();

                Destroy(gameObject);
            }
        }
    }

    [DefaultExecutionOrder(-11000)]
    [DisallowMultipleComponent]
    public sealed class ZomCityTestSceneBootstrap : MonoBehaviour
    {
        private const string RuntimeObjectName = "[ZomCity]TestSceneBootstrap";

        private const string SceneTest = "TestScene";
        private const string SceneWeapon = "WeaponTest";
        private const string SceneMonster = "Run_MonsterTest";
        private const string ScenePickup = "Run_PickupTest";

        private const string AnchorSpawnPlayer = "Spawn_Player";
        private const string AnchorWeaponTarget = "Spawn_WeaponTarget";
        private const string AnchorFireLane = "FireLane_Anchor";
        private const string AnchorSpawnMonster = "Spawn_Monster";
        private const string AnchorSpawnPickup = "Spawn_Pickup";
        private const string AnchorSpawnContainer = "Spawn_Container";

        private const string PlayerPlaceholderName = "[ZomCity]PlayerPlaceholder";
        private const string PlayerRuntimeName = "Player_ZC_Base";
        private const string PlayerPrefabResourcePath = "ZomCity/Player_ZC_Base";
        private const string PlayerPrefabAssetPath = "Assets/ZomCity/Content/Characters/Player/Player_ZC_Base.prefab";
        private const string WeaponTargetName = "[ZomCity]WeaponTargetDummy";
        private const string EnemyDummyName = "[ZomCity]EnemyDummy";
        private const string WeaponPickupName = "[ZomCity]WeaponPickupDummy";
        private const string AmmoPickupName = "[ZomCity]AmmoPickupDummy";
        private const string ItemPickupName = "[ZomCity]ItemPickupDummy";
        private const string ContainerDummyName = "[ZomCity]ContainerDummy";

        private static readonly string[] ManagedSceneNames =
        {
            SceneTest,
            SceneWeapon,
            SceneMonster,
            ScenePickup,
        };

        private static ZomCityTestSceneBootstrap s_instance;

        public static void EnsureExists()
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = FindAnyObjectByType<ZomCityTestSceneBootstrap>();
            if (s_instance != null)
            {
                return;
            }

            var go = new GameObject(RuntimeObjectName);
            s_instance = go.AddComponent<ZomCityTestSceneBootstrap>();
            DontDestroyOnLoad(go);
        }

        public static bool IsManagedSceneName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (var i = 0; i < ManagedSceneNames.Length; i++)
            {
                if (string.Equals(ManagedSceneNames[i], sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public static void EnsureSceneBaselineNow()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.ApplySceneBaseline(SceneManager.GetActiveScene());
        }

        public static void SpawnDebugWeaponPickup()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.EnsureWeaponPickup(SceneManager.GetActiveScene(), true);
        }

        public static void SpawnDebugAmmoPickup()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.EnsureAmmoPickup(SceneManager.GetActiveScene(), true);
        }

        public static void SpawnDebugEnemy()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.EnsureEnemyDummy(SceneManager.GetActiveScene(), true);
        }

        public static void SpawnDebugItemPickup()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.EnsureItemPickup(SceneManager.GetActiveScene(), true);
        }

        public static void SpawnDebugContainer()
        {
            EnsureExists();
            if (s_instance == null)
            {
                return;
            }

            s_instance.EnsureContainerDummy(SceneManager.GetActiveScene(), true);
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySceneBaseline(SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplySceneBaseline(scene);
        }

        private void ApplySceneBaseline(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (!IsManagedSceneName(scene.name))
            {
                return;
            }

            EnsurePlayerPlaceholder(scene, false);

            if (string.Equals(scene.name, SceneWeapon, StringComparison.Ordinal))
            {
                EnsureWeaponTarget(scene, false);
                EnsureWeaponPickup(scene, false);
                EnsureAmmoPickup(scene, false);
                return;
            }

            if (string.Equals(scene.name, SceneMonster, StringComparison.Ordinal))
            {
                EnsureEnemyDummy(scene, false);
                return;
            }

            if (string.Equals(scene.name, ScenePickup, StringComparison.Ordinal))
            {
                EnsureItemPickup(scene, false);
                EnsureContainerDummy(scene, false);
            }
        }

        private GameObject EnsurePlayerPlaceholder(Scene scene, bool forceReposition)
        {
            var spawnAnchor = FindTransformByName(scene, AnchorSpawnPlayer);
            var spawnPos = spawnAnchor != null
                ? spawnAnchor.position
                : ResolveFallbackPosition(scene);
            var spawnRot = spawnAnchor != null
                ? spawnAnchor.rotation
                : Quaternion.identity;

            var player = FindObjectInSceneByTag(scene, TagCatalog.Player);
            if (player != null)
            {
                if (forceReposition)
                {
                    player.transform.SetPositionAndRotation(spawnPos, spawnRot);
                }

                EnsurePlayerBinding(player);
                return player;
            }

            if (TryInstantiatePlayerFromPrefab(scene, spawnPos, spawnRot, out var spawnedPlayer))
            {
                EnsurePlayerBinding(spawnedPlayer);
                return spawnedPlayer;
            }

            var go = EnsurePrimitive(
                scene,
                PlayerPlaceholderName,
                PrimitiveType.Capsule,
                spawnPos,
                new Vector3(0.8f, 1.6f, 0.8f),
                TagCatalog.Player,
                LayerCatalog.Player,
                false,
                true,
                false);

            var body = go.transform.Find("Body");
            if (body == null)
            {
                var bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(go.transform, false);
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints =
                RigidbodyConstraints.FreezePositionZ |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationY |
                RigidbodyConstraints.FreezeRotationZ;

            EnsurePlayerBinding(go);
            return go;
        }

        private static bool TryInstantiatePlayerFromPrefab(Scene scene, Vector3 spawnPos, Quaternion spawnRot, out GameObject instance)
        {
            instance = null;
            var playerPrefab = TryLoadPlayerPrefab();
            if (playerPrefab == null)
            {
                return false;
            }

            instance = Instantiate(playerPrefab, spawnPos, spawnRot);
            instance.name = PlayerRuntimeName;
            SceneManager.MoveGameObjectToScene(instance, scene);
            return true;
        }

        private static GameObject TryLoadPlayerPrefab()
        {
            var prefab = Resources.Load<GameObject>(PlayerPrefabResourcePath);
            if (prefab != null)
            {
                return prefab;
            }

            var assetDatabaseType = Type.GetType("UnityEditor.AssetDatabase, UnityEditor");
            if (assetDatabaseType == null)
            {
                return null;
            }

            var loadAssetMethod = assetDatabaseType.GetMethod(
                "LoadAssetAtPath",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Type) },
                null);
            if (loadAssetMethod == null)
            {
                return null;
            }

            return loadAssetMethod.Invoke(null, new object[] { PlayerPrefabAssetPath, typeof(GameObject) }) as GameObject;
        }

        private static void EnsurePlayerBinding(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            TryAssignTag(player, TagCatalog.Player);
            TryAssignLayer(player, LayerCatalog.Player);

            var binding = player.GetComponent<GameplayRootBinding>();
            if (binding == null)
            {
                binding = player.AddComponent<GameplayRootBinding>();
            }

            var visualRoot = FindTransformByNameRecursive(player.transform, "Body");
            binding.PhysicsRoot = player.transform;
            binding.VisualRoot = visualRoot != null ? visualRoot : player.transform;
            binding.ActorId = "PLAYER_MAIN";
            binding.AutoBindChildByName = false;
        }

        private GameObject EnsureWeaponTarget(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorWeaponTarget, ResolveFallbackPosition(scene) + new Vector3(6f, 0.8f, 0f));
            return EnsurePrimitive(
                scene,
                WeaponTargetName,
                PrimitiveType.Cube,
                pos,
                new Vector3(1.2f, 1.8f, 1.2f),
                TagCatalog.Enemy,
                LayerCatalog.Enemy,
                false,
                true,
                forceReposition);
        }

        private GameObject EnsureEnemyDummy(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorSpawnMonster, ResolveFallbackPosition(scene) + new Vector3(4f, 0.8f, 0f));
            return EnsurePrimitive(
                scene,
                EnemyDummyName,
                PrimitiveType.Capsule,
                pos,
                new Vector3(1f, 1.8f, 1f),
                TagCatalog.Enemy,
                LayerCatalog.Enemy,
                false,
                true,
                forceReposition);
        }

        private GameObject EnsureWeaponPickup(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorFireLane, ResolveFallbackPosition(scene) + new Vector3(1.8f, 0.6f, 0f));
            return EnsurePrimitive(
                scene,
                WeaponPickupName,
                PrimitiveType.Cylinder,
                pos,
                new Vector3(0.45f, 0.2f, 0.45f),
                TagCatalog.Interactable,
                LayerCatalog.Interactable,
                true,
                true,
                forceReposition);
        }

        private GameObject EnsureAmmoPickup(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorFireLane, ResolveFallbackPosition(scene) + new Vector3(2.6f, 0.6f, 0f));
            return EnsurePrimitive(
                scene,
                AmmoPickupName,
                PrimitiveType.Sphere,
                pos,
                new Vector3(0.35f, 0.35f, 0.35f),
                TagCatalog.Interactable,
                LayerCatalog.Interactable,
                true,
                true,
                forceReposition);
        }

        private GameObject EnsureItemPickup(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorSpawnPickup, ResolveFallbackPosition(scene) + new Vector3(2f, 0.6f, 0f));
            return EnsurePrimitive(
                scene,
                ItemPickupName,
                PrimitiveType.Sphere,
                pos,
                new Vector3(0.4f, 0.4f, 0.4f),
                TagCatalog.Interactable,
                LayerCatalog.Interactable,
                true,
                true,
                forceReposition);
        }

        private GameObject EnsureContainerDummy(Scene scene, bool forceReposition)
        {
            var pos = ResolveAnchorPosition(scene, AnchorSpawnContainer, ResolveFallbackPosition(scene) + new Vector3(3.2f, 0.8f, 0f));
            return EnsurePrimitive(
                scene,
                ContainerDummyName,
                PrimitiveType.Cube,
                pos,
                new Vector3(1.1f, 0.9f, 1.1f),
                TagCatalog.Container,
                LayerCatalog.Container,
                true,
                true,
                forceReposition);
        }

        private static GameObject EnsurePrimitive(
            Scene scene,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            string tagName,
            string layerName,
            bool isTrigger,
            bool makeKinematic,
            bool forceReposition)
        {
            var go = FindObjectInSceneByName(scene, objectName);
            var created = false;
            if (go == null)
            {
                go = GameObject.CreatePrimitive(primitiveType);
                go.name = objectName;
                SceneManager.MoveGameObjectToScene(go, scene);
                created = true;
            }

            if (created || forceReposition)
            {
                go.transform.position = position;
            }

            go.transform.localScale = scale;
            TryAssignTag(go, tagName);
            TryAssignLayer(go, layerName);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = isTrigger;
            }

            var rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }

            rb.isKinematic = makeKinematic;
            rb.useGravity = !makeKinematic;
            rb.constraints = makeKinematic
                ? RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.FreezePositionZ |
                  RigidbodyConstraints.FreezeRotationX |
                  RigidbodyConstraints.FreezeRotationY |
                  RigidbodyConstraints.FreezeRotationZ;

            return go;
        }

        private static void TryAssignTag(GameObject go, string tagName)
        {
            if (go == null || string.IsNullOrWhiteSpace(tagName))
            {
                return;
            }

            try
            {
                if (TagCatalog.Exists(tagName))
                {
                    go.tag = tagName;
                }
            }
            catch (UnityException)
            {
            }
        }

        private static void TryAssignLayer(GameObject go, string layerName)
        {
            if (go == null || string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            var index = LayerCatalog.Resolve(layerName);
            if (index >= 0)
            {
                go.layer = index;
            }
        }

        private static Vector3 ResolveAnchorPosition(Scene scene, string anchorName, Vector3 fallback)
        {
            var anchor = FindTransformByName(scene, anchorName);
            return anchor != null ? anchor.position : fallback;
        }

        private static Vector3 ResolveFallbackPosition(Scene scene)
        {
            var player = FindObjectInSceneByTag(scene, TagCatalog.Player);
            if (player != null)
            {
                return player.transform.position;
            }

            return Vector3.zero;
        }

        private static GameObject FindObjectInSceneByName(Scene scene, string objectName)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var found = FindTransformByNameRecursive(root.transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindObjectInSceneByTag(Scene scene, string tagName)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(tagName))
            {
                return null;
            }

            if (!TagCatalog.Exists(tagName))
            {
                return null;
            }

            GameObject[] tagged;
            try
            {
                tagged = GameObject.FindGameObjectsWithTag(tagName);
            }
            catch (UnityException)
            {
                return null;
            }

            for (var i = 0; i < tagged.Length; i++)
            {
                var go = tagged[i];
                if (go == null)
                {
                    continue;
                }

                if (go.scene == scene)
                {
                    return go;
                }
            }

            return null;
        }

        private static Transform FindTransformByName(Scene scene, string objectName)
        {
            if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var found = FindTransformByNameRecursive(root.transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindTransformByNameRecursive(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, objectName, StringComparison.Ordinal))
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                var found = FindTransformByNameRecursive(child, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
