using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ZomCity.Tests.Editor
{
    public sealed class M05EditorAcceptanceTests
    {
        private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Test]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u6821\u9a8c\u4e00\u952e\u5bfc\u51fa\u4e0e\u6821\u9a8c\u547d\u4ee4\u53ef\u6267\u884c\u3002")]
        public void ExportAndValidateCommand_GeneratesRequiredArtifacts()
        {
            ZomCityDataBuildTools.ExportAndValidate();

            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Design/ZomCityDesign.cdb")), Is.True, "Missing ZomCityDesign.cdb in DesignRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/Settings.json")), Is.True, "Missing Settings.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/Items.json")), Is.True, "Missing Items.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/Weapons.json")), Is.True, "Missing Weapons.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/LootTables.json")), Is.True, "Missing LootTables.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/Containers.json")), Is.True, "Missing Containers.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/AddressFallbackMap.json")), Is.True, "Missing AddressFallbackMap.json in GeneratedRoot");
            Assert.That(File.Exists(ToAbsolutePath("Assets/ZomCity/Data/Generated/DataVersionManifest.json")), Is.True, "Missing DataVersionManifest.json in GeneratedRoot");
            Assert.That(GetLatestDataValidatorReport() != null, Is.True, "Missing DataValidatorReport file");
        }

        [Test]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u68c0\u67e5\u6570\u636e\u6821\u9a8c\u62a5\u544a\u662f\u5426\u5b58\u5728\u9519\u8bef\u9879\u3002")]
        public void DataValidatorReport_HasNoErrors()
        {
            ZomCityDataBuildTools.ExportAndValidate();
            var latestReportPath = GetLatestDataValidatorReport();
            Assert.That(latestReportPath, Is.Not.Null.And.Not.Empty, "Missing DataValidatorReport file");

            var reportJson = File.ReadAllText(latestReportPath);
            var report = JsonUtility.FromJson<DataValidatorReportSnapshot>(reportJson);
            Assert.That(report, Is.Not.Null, "Failed to parse DataValidatorReport");
            Assert.That(report.Errors == null || report.Errors.Count == 0, Is.True, "DataValidatorReport contains blocking errors");
            Assert.That(string.IsNullOrWhiteSpace(report.DataVersion), Is.False, "DataVersion is empty in report");
            Assert.That(string.IsNullOrWhiteSpace(report.SaveVersion), Is.False, "SaveVersion is empty in report");
        }

        [Test]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4Settings\u4e3b\u884c\u4e0ePrefabKey\u5408\u540c\u6709\u6548\u3002")]
        public void SettingsTable_ContainsSettingsMainAndValidGenericPickupPrefabKey()
        {
            ZomCityDataBuildTools.ExportAndValidate();

            var settingsJson = File.ReadAllText(ToAbsolutePath("Assets/ZomCity/Data/Generated/Settings.json"));
            var settings = JsonUtility.FromJson<SettingsTable>(settingsJson);

            Assert.That(settings, Is.Not.Null, "Failed to parse Settings.json");
            Assert.That(settings.Rows, Is.Not.Null.And.Not.Empty, "Settings table is empty");

            SettingsRow settingsMain = null;
            for (var i = 0; i < settings.Rows.Count; i++)
            {
                var row = settings.Rows[i];
                if (row == null)
                {
                    continue;
                }

                if (string.Equals(row.SettingsID, "SETTINGS_MAIN", StringComparison.Ordinal))
                {
                    settingsMain = row;
                    break;
                }
            }

            Assert.That(settingsMain, Is.Not.Null, "Missing SETTINGS_MAIN row");
            Assert.That(PrefabKeyValidator.TryValidate(settingsMain.GenericPickupPrefabKey, out _), Is.True, "GenericPickupPrefabKey is invalid");
        }

        [Test]
        [Category("M0.5")]
        [Description("\u4f5c\u7528\uff1a\u9a8c\u8bc1PrefabKey\u547d\u540d\u5408\u540c\u7684\u62e6\u622a\u80fd\u529b\u3002")]
        public void PrefabKeyValidator_ValidatesAddressContract()
        {
            Assert.That(PrefabKeyValidator.TryValidate("ZC/Items/MAT_SCRAP", out _), Is.True, "Valid PrefabKey rejected");
            Assert.That(PrefabKeyValidator.TryValidate("Items/MAT_SCRAP", out _), Is.False, "Prefix error was not blocked");
            Assert.That(PrefabKeyValidator.TryValidate("ZC/Unknown/MAT_SCRAP", out _), Is.False, "Domain error was not blocked");
            Assert.That(PrefabKeyValidator.TryValidate("ZC/Items/mat_scrap", out _), Is.False, "StableID format error was not blocked");
        }

        [Test]
        [Category("M0.6")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 M0.6 \u65b0\u589e\u4e8b\u4ef6ID\u6ee1\u8db3 EVT_<Domain>_<Verb> \u547d\u540d\u89c4\u8303\u3002")]
        public void GameplayEventIds_M06EventNamesMatchConvention()
        {
            Assert.That(GameplayEventIds.NoiseEmit.StartsWith("EVT_", StringComparison.Ordinal), Is.True, "NoiseEmit EventId prefix mismatch");
            Assert.That(GameplayEventIds.NoiseEmit.Contains("_"), Is.True, "NoiseEmit EventId separator mismatch");

            Assert.That(GameplayEventIds.CombatDamage.StartsWith("EVT_", StringComparison.Ordinal), Is.True, "CombatDamage EventId prefix mismatch");
            Assert.That(GameplayEventIds.CombatDamage.Contains("_"), Is.True, "CombatDamage EventId separator mismatch");

            Assert.That(GameplayEventIds.LootPickup.StartsWith("EVT_", StringComparison.Ordinal), Is.True, "LootPickup EventId prefix mismatch");
            Assert.That(GameplayEventIds.LootPickup.Contains("_"), Is.True, "LootPickup EventId separator mismatch");
        }

        [Test]
        [Category("M0.6")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 EventViewer \u53ef\u521b\u5efa\u5e76\u53ef\u6e05\u7a7a\u7f13\u5b58\u3002")]
        public void GameplayEventViewer_CanCreateAndClearRecords()
        {
            GameplayEventViewer.EnsureExists();
            var viewer = GameplayEventViewer.Instance;
            Assert.That(viewer, Is.Not.Null, "EventViewer missing");

            viewer.ClearRecords();
            Assert.That(viewer.CachedRecordCount, Is.EqualTo(0), "Viewer records were not cleared");

            UnityEngine.Object.DestroyImmediate(viewer.gameObject);
        }

        [Test]
        [Category("M0.7")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 M0.7 \u56de\u5f52\u573a\u666f\u9aa8\u67b6\u5b58\u5728\u4e14\u5305\u542b\u5173\u952e\u951a\u70b9\u3002")]
        public void M07_RegressionScenes_ExistAndContainAnchors()
        {
            AssertSceneContainsTokens(
                "Assets/Scenes/TestScene/Sidescroller Demo/TestScene.unity",
                "MainCameraSidescroller",
                "Player_ZC_Base");

            AssertSceneContainsTokens(
                "Assets/Scenes/WeaponTest/WeaponTest.unity",
                "GroundCollider",
                "Spawn_Player",
                "Spawn_WeaponTarget",
                "FireLane_Anchor");

            AssertSceneContainsTokens(
                "Assets/Scenes/Run_MonsterTest/Run_MonsterTest.unity",
                "MainCameraSidescroller",
                "Spawn_Player",
                "Spawn_Monster",
                "Patrol_Area_Center");

            AssertSceneContainsTokens(
                "Assets/Scenes/Run_PickupTest/Run_PickupTest.unity",
                "MainCameraSidescroller",
                "Spawn_Player",
                "Spawn_Pickup",
                "Spawn_Container");
        }

        [Test]
        [Category("M0.7")]
        [Description("\u4f5c\u7528\uff1a\u786e\u8ba4 M0.7 TestSceneBootstrap \u53ef\u521b\u5efa\u8fd0\u884c\u65f6\u8282\u70b9\u3002")]
        public void M07_TestSceneBootstrap_CanCreateRuntimeObject()
        {
            ZomCityTestSceneBootstrap.EnsureExists();
            var runtimeObject = GameObject.Find("[ZomCity]TestSceneBootstrap");
            Assert.That(runtimeObject, Is.Not.Null, "Missing runtime bootstrap object");

            if (runtimeObject != null)
            {
                UnityEngine.Object.DestroyImmediate(runtimeObject);
            }
        }

        private static void AssertSceneContainsTokens(string sceneRelativePath, params string[] requiredTokens)
        {
            var fullPath = ToAbsolutePath(sceneRelativePath);
            Assert.That(File.Exists(fullPath), Is.True, "Scene file missing: " + sceneRelativePath);

            var text = File.ReadAllText(fullPath);
            for (var i = 0; i < requiredTokens.Length; i++)
            {
                var token = requiredTokens[i];
                Assert.That(text.Contains(token), Is.True, "Scene token missing: " + sceneRelativePath + " => " + token);
            }
        }

        private static string GetLatestDataValidatorReport()
        {
            var reportDirectory = ToAbsolutePath(ZomCityProjectConstants.Paths.ReportsOutDir);
            if (!Directory.Exists(reportDirectory))
            {
                return null;
            }

            var files = Directory.GetFiles(reportDirectory, "DataValidatorReport_*.json", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                return null;
            }

            Array.Sort(files, StringComparer.Ordinal);
            return files[files.Length - 1];
        }

        private static string ToAbsolutePath(string relativePath)
        {
            var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(ProjectRoot, normalizedPath);
        }

        [Serializable]
        private sealed class DataValidatorReportSnapshot
        {
            public string DataVersion;
            public string SaveVersion;
            public List<string> Errors;
        }
    }
}
