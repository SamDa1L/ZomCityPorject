using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ZomCity.Tests.PlayMode
{
    public sealed class M05PlayModeAcceptanceTests
    {
        [UnityTest]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4PlayMode\u4e0bDataRegistry\u5df2\u52a0\u8f7d\u6838\u5fc3\u914d\u7f6e\u3002")]
        public IEnumerator DataRegistry_LoadsVersionAndSettingsOnPlayMode()
        {
            DataRegistry.Reload();
            yield return null;

            Assert.That(string.IsNullOrWhiteSpace(DataRegistry.DataVersion), Is.False, "DataVersion is empty");
            Assert.That(string.IsNullOrWhiteSpace(DataRegistry.SaveVersion), Is.False, "SaveVersion is empty");
            Assert.That(DataRegistry.SettingsMain, Is.Not.Null, "SettingsMain is null");
            Assert.That(string.Equals(DataRegistry.SettingsMain.SettingsID, "SETTINGS_MAIN", StringComparison.Ordinal), Is.True, "SettingsID is invalid");
        }

        [UnityTest]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4\u53ef\u6309ItemID\u67e5\u8be2\u5230\u914d\u7f6e\u6570\u636e\u3002")]
        public IEnumerator DataRegistry_CanQueryConfiguredItemByStableId()
        {
            DataRegistry.Reload();
            yield return null;

            var ok = DataRegistry.TryGetItem("MAT_SCRAP", out var row);
            Assert.That(ok, Is.True, "Item lookup failed for MAT_SCRAP");
            Assert.That(row, Is.Not.Null, "Item row is null");
            Assert.That(string.Equals(row.ItemID, "MAT_SCRAP", StringComparison.Ordinal), Is.True, "ItemID mismatch");
        }

        [UnityTest]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4\u7f3a\u5931\u6570\u636e\u65f6\u4f1a\u62a5\u9519\u5e76\u4e0a\u62a5\u4e8b\u4ef6\u3002")]
        public IEnumerator DataRegistry_MissingItemRaisesEventAndException()
        {
            DataRegistry.Reload();
            yield return null;

            var hasCapturedEvent = false;
            var capturedEvent = new DataMissingEvent();

            void OnDataMissing(DataMissingEvent evt)
            {
                capturedEvent = evt;
                hasCapturedEvent = true;
            }

            var previousErrorStackTrace = Application.GetStackTraceLogType(LogType.Error);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

            GameplayEventHub.DataMissing += OnDataMissing;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex(@"EVT_Data_Missing.*MAT_NOT_EXISTS_FOR_TEST"));
                Assert.Throws<InvalidOperationException>(() => DataRegistry.GetItem("MAT_NOT_EXISTS_FOR_TEST"), "Expected exception was not thrown");
                yield return null;

                Assert.That(hasCapturedEvent, Is.True, "DataMissing event not captured");
                Assert.That(string.Equals(capturedEvent.EventId, GameplayEventIds.DataMissing, StringComparison.Ordinal), Is.True, "EventId mismatch");
                Assert.That(string.Equals(capturedEvent.DataId, "MAT_NOT_EXISTS_FOR_TEST", StringComparison.Ordinal), Is.True, "DataId mismatch");
            }
            finally
            {
                GameplayEventHub.DataMissing -= OnDataMissing;
                Application.SetStackTraceLogType(LogType.Error, previousErrorStackTrace);
            }
        }

        [UnityTest]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4\u975e\u6cd5PrefabKey\u4f1a\u89e6\u53d1\u5931\u8d25\u56de\u8c03\u4e0e\u4e8b\u4ef6\u3002")]
        public IEnumerator AddressablesLoader_InvalidPrefabKeyRaisesFailEvent()
        {
            DataRegistry.Reload();
            yield return null;

            var hasCapturedEvent = false;
            var capturedEvent = new PrefabLoadFailedEvent();
            var failCallbackTriggered = false;

            void OnPrefabLoadFailed(PrefabLoadFailedEvent evt)
            {
                capturedEvent = evt;
                hasCapturedEvent = true;
            }

            var previousErrorStackTrace = Application.GetStackTraceLogType(LogType.Error);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

            GameplayEventHub.PrefabLoadFailed += OnPrefabLoadFailed;
            try
            {
                LogAssert.Expect(LogType.Error, new Regex(@"EVT_Data_PrefabLoadFailed.*INVALID_PREFAB_KEY"));
                yield return AddressablesLoader.LoadPrefabAsync(
                    "INVALID_PREFAB_KEY",
                    _ => Assert.Fail("Invalid PrefabKey must not load successfully"),
                    _ => { failCallbackTriggered = true; });

                yield return null;

                Assert.That(failCallbackTriggered, Is.True, "Fail callback was not triggered");
                Assert.That(hasCapturedEvent, Is.True, "PrefabLoadFailed event not captured");
                Assert.That(string.Equals(capturedEvent.EventId, GameplayEventIds.PrefabLoadFailed, StringComparison.Ordinal), Is.True, "EventId mismatch");
                Assert.That(string.Equals(capturedEvent.PrefabKey, "INVALID_PREFAB_KEY", StringComparison.Ordinal), Is.True, "PrefabKey mismatch");
            }
            finally
            {
                GameplayEventHub.PrefabLoadFailed -= OnPrefabLoadFailed;
                Application.SetStackTraceLogType(LogType.Error, previousErrorStackTrace);
            }
        }

        [UnityTest]
        [Category("M0.6")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 Noise/Damage/Pickup \u4e8b\u4ef6\u53ef\u53d1\u5e03\u5e76\u88ab\u8ba2\u9605\u3002")]
        public IEnumerator GameplayEventHub_NoiseDamagePickup_CanPublishAndSubscribe()
        {
            var hasNoise = false;
            var hasDamage = false;
            var hasPickup = false;

            void OnNoise(NoiseEvent evt)
            {
                hasNoise = string.Equals(evt.EventId, GameplayEventIds.NoiseEmit, StringComparison.Ordinal) &&
                           string.Equals(evt.WeaponId, "WPN_TEST_M06", StringComparison.Ordinal);
            }

            void OnDamage(DamageEvent evt)
            {
                hasDamage = string.Equals(evt.EventId, GameplayEventIds.CombatDamage, StringComparison.Ordinal) &&
                            string.Equals(evt.TargetId, "DummyTarget", StringComparison.Ordinal) &&
                            evt.Amount > 0f;
            }

            void OnPickup(PickupEvent evt)
            {
                hasPickup = string.Equals(evt.EventId, GameplayEventIds.LootPickup, StringComparison.Ordinal) &&
                            string.Equals(evt.ItemId, "MAT_SCRAP", StringComparison.Ordinal) &&
                            evt.Quantity == 2;
            }

            GameplayEventHub.Noise += OnNoise;
            GameplayEventHub.CombatDamage += OnDamage;
            GameplayEventHub.LootPickup += OnPickup;
            try
            {
                GameplayEventHub.Publish(new NoiseEvent
                {
                    EventId = GameplayEventIds.NoiseEmit,
                    Frame = Time.frameCount,
                    Scene = "M06_TestScene",
                    ActorId = "PLAYER_MAIN",
                    WeaponId = "WPN_TEST_M06",
                    Radius = 9f,
                    SourcePosition = Vector3.zero,
                    Reason = "Test",
                });

                GameplayEventHub.Publish(new DamageEvent
                {
                    EventId = GameplayEventIds.CombatDamage,
                    Frame = Time.frameCount,
                    Scene = "M06_TestScene",
                    ActorId = "PLAYER_MAIN",
                    WeaponId = "WPN_TEST_M06",
                    TargetId = "DummyTarget",
                    Amount = 12f,
                    HitPoint = new Vector3(1f, 2f, 0f),
                    SourcePosition = Vector3.zero,
                });

                GameplayEventHub.Publish(new PickupEvent
                {
                    EventId = GameplayEventIds.LootPickup,
                    Frame = Time.frameCount,
                    Scene = "M06_TestScene",
                    ActorId = "PLAYER_MAIN",
                    ItemId = "MAT_SCRAP",
                    Quantity = 2,
                    Source = "Test",
                });

                yield return null;

                Assert.That(hasNoise, Is.True, "Noise event not captured");
                Assert.That(hasDamage, Is.True, "Damage event not captured");
                Assert.That(hasPickup, Is.True, "Pickup event not captured");
            }
            finally
            {
                GameplayEventHub.Noise -= OnNoise;
                GameplayEventHub.CombatDamage -= OnDamage;
                GameplayEventHub.LootPickup -= OnPickup;
            }
        }

        [UnityTest]
        [Category("M0.6")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 EventViewer \u53ef\u8bb0\u5f55\u4e8b\u4ef6\u5e76\u652f\u6301\u6682\u505c\u66f4\u65b0\u3002")]
        public IEnumerator GameplayEventViewer_RecordsAndPauseWorks()
        {
            GameplayEventViewer.EnsureExists();
            yield return null;

            var viewer = GameplayEventViewer.Instance;
            Assert.That(viewer, Is.Not.Null, "EventViewer missing");

            viewer.PauseUpdate = false;
            viewer.MaxRecords = 256;
            viewer.ClearRecords();

            GameplayEventHub.Publish(new PickupEvent
            {
                EventId = GameplayEventIds.LootPickup,
                Frame = Time.frameCount,
                Scene = "M06_TestScene",
                ActorId = "PLAYER_MAIN",
                ItemId = "MAT_M06_PICKUP",
                Quantity = 1,
                Source = "Test",
            });

            yield return null;

            Assert.That(viewer.ContainsRecord(GameplayEventIds.LootPickup, itemId: "MAT_M06_PICKUP"), Is.True, "Viewer missing pickup record");

            var before = viewer.CachedRecordCount;
            viewer.PauseUpdate = true;

            GameplayEventHub.Publish(new PickupEvent
            {
                EventId = GameplayEventIds.LootPickup,
                Frame = Time.frameCount,
                Scene = "M06_TestScene",
                ActorId = "PLAYER_MAIN",
                ItemId = "MAT_M06_PICKUP_PAUSE",
                Quantity = 1,
                Source = "TestPause",
            });

            yield return null;

            Assert.That(viewer.CachedRecordCount, Is.EqualTo(before), "PauseUpdate changed record count");
            Assert.That(viewer.ContainsRecord(GameplayEventIds.LootPickup, itemId: "MAT_M06_PICKUP_PAUSE"), Is.False, "Paused viewer still recorded event");

            viewer.PauseUpdate = false;
        }

        [UnityTest]
        [Category("M0.6")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 EventViewer \u6700\u8fd1N\u6761\u7f13\u5b58\u4e0a\u9650\u751f\u6548\u3002")]
        public IEnumerator GameplayEventViewer_RespectsMaxRecordLimit()
        {
            GameplayEventViewer.EnsureExists();
            yield return null;

            var viewer = GameplayEventViewer.Instance;
            Assert.That(viewer, Is.Not.Null, "EventViewer missing");

            viewer.ClearRecords();
            viewer.PauseUpdate = false;
            viewer.MaxRecords = 20;

            for (var i = 0; i < 40; i++)
            {
                GameplayEventHub.Publish(new NoiseEvent
                {
                    EventId = GameplayEventIds.NoiseEmit,
                    Frame = Time.frameCount,
                    Scene = "M06_TestScene",
                    ActorId = "PLAYER_MAIN",
                    WeaponId = "WPN_TEST_M06",
                    Radius = 5f + i,
                    SourcePosition = Vector3.zero,
                    Reason = "TestLimit",
                });
            }

            yield return null;

            Assert.That(viewer.CachedRecordCount, Is.LessThanOrEqualTo(20), "Record limit not applied");
            Assert.That(viewer.ContainsRecord(GameplayEventIds.NoiseEmit, weaponId: "WPN_TEST_M06"), Is.True, "Noise record missing");
        }

        [UnityTest]
        [Category("M0.7")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4M0.7\u53d7\u7ba1\u573a\u666f\u4f1a\u81ea\u52a8\u751f\u6210\u73a9\u5bb6\u4e0e\u6b66\u5668\u573a\u666f\u57fa\u7ebf\u5360\u4f4d\u3002")]
        public IEnumerator M07_TestSceneBootstrap_ManagedSceneBaselineCreatesPlaceholders()
        {
            var originalScene = SceneManager.GetActiveScene();
            DestroyRuntimeBootstrapObject();
            yield return null;

            var tempScene = SceneManager.CreateScene("WeaponTest");
            SceneManager.SetActiveScene(tempScene);

            ZomCityTestSceneBootstrap.EnsureSceneBaselineNow();
            yield return null;

            Assert.That(FindObjectInSceneByTag(tempScene, TagCatalog.Player), Is.Not.Null, "Missing player actor in managed scene");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]WeaponTargetDummy"), Is.Not.Null, "Missing weapon target placeholder in managed scene");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]WeaponPickupDummy"), Is.Not.Null, "Missing weapon pickup placeholder in managed scene");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]AmmoPickupDummy"), Is.Not.Null, "Missing ammo pickup placeholder in managed scene");

            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalScene);
            }

            var unload = SceneManager.UnloadSceneAsync(tempScene);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }

            DestroyRuntimeBootstrapObject();
            yield return null;
        }

        [UnityTest]
        [Category("M0.7")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4M0.7\u8c03\u8bd5\u5237\u65b0API\u53ef\u5728\u53d7\u7ba1\u573a\u666f\u521b\u5efa\u5360\u4f4d\u7269\u4f53\u3002")]
        public IEnumerator M07_TestSceneBootstrap_DebugSpawnApisCreateDebugObjects()
        {
            var originalScene = SceneManager.GetActiveScene();
            DestroyRuntimeBootstrapObject();
            yield return null;

            var tempScene = SceneManager.CreateScene("Run_PickupTest");
            SceneManager.SetActiveScene(tempScene);

            ZomCityTestSceneBootstrap.SpawnDebugWeaponPickup();
            ZomCityTestSceneBootstrap.SpawnDebugAmmoPickup();
            ZomCityTestSceneBootstrap.SpawnDebugEnemy();
            ZomCityTestSceneBootstrap.SpawnDebugItemPickup();
            ZomCityTestSceneBootstrap.SpawnDebugContainer();
            yield return null;

            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]WeaponPickupDummy"), Is.Not.Null, "Missing weapon pickup debug object");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]AmmoPickupDummy"), Is.Not.Null, "Missing ammo pickup debug object");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]EnemyDummy"), Is.Not.Null, "Missing enemy debug object");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]ItemPickupDummy"), Is.Not.Null, "Missing item pickup debug object");
            Assert.That(FindObjectInSceneByName(tempScene, "[ZomCity]ContainerDummy"), Is.Not.Null, "Missing container debug object");

            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.SetActiveScene(originalScene);
            }

            var unload = SceneManager.UnloadSceneAsync(tempScene);
            while (unload != null && !unload.isDone)
            {
                yield return null;
            }

            DestroyRuntimeBootstrapObject();
            yield return null;
        }

        private static void DestroyRuntimeBootstrapObject()
        {
            var runtimeBootstrap = GameObject.Find("[ZomCity]TestSceneBootstrap");
            if (runtimeBootstrap != null)
            {
                UnityEngine.Object.Destroy(runtimeBootstrap);
            }
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
                if (go != null && go.scene == scene)
                {
                    return go;
                }
            }

            return null;
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
