using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZomCity
{
    public static class DataRegistry
    {
        private const string SystemActorId = "SYSTEM_DATA";

        private static readonly Dictionary<string, ItemRow> ItemById = new Dictionary<string, ItemRow>(StringComparer.Ordinal);
        private static readonly Dictionary<string, WeaponRow> WeaponById = new Dictionary<string, WeaponRow>(StringComparer.Ordinal);
        private static readonly Dictionary<string, LootTableRow> LootTableById = new Dictionary<string, LootTableRow>(StringComparer.Ordinal);
        private static readonly Dictionary<string, ContainerRow> ContainerById = new Dictionary<string, ContainerRow>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> AddressFallbackByKey = new Dictionary<string, string>(StringComparer.Ordinal);

        private static bool s_loaded;

        public static DataVersionManifest Manifest { get; private set; } = new DataVersionManifest();
        public static SettingsRow SettingsMain { get; private set; } = new SettingsRow();

        public static string DataVersion => string.IsNullOrWhiteSpace(Manifest.DataVersion) ? "UNKNOWN" : Manifest.DataVersion;
        public static string SaveVersion => string.IsNullOrWhiteSpace(Manifest.SaveVersion) ? "UNKNOWN" : Manifest.SaveVersion;

        public static void EnsureLoaded()
        {
            if (s_loaded)
            {
                return;
            }

            Reload();
        }

        public static void Reload()
        {
            s_loaded = true;
            ItemById.Clear();
            WeaponById.Clear();
            LootTableById.Clear();
            ContainerById.Clear();
            AddressFallbackByKey.Clear();

            Manifest = LoadJsonFile("DataVersionManifest.json", new DataVersionManifest());

            var settingsTable = LoadJsonFile("Settings.json", new SettingsTable());
            SettingsMain = ResolveSettingsMain(settingsTable);

            var itemsTable = LoadJsonFile("Items.json", new ItemsTable());
            for (var i = 0; i < itemsTable.Rows.Count; i++)
            {
                var row = itemsTable.Rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.ItemID))
                {
                    continue;
                }

                ItemById[row.ItemID] = row;
            }

            var weaponsTable = LoadJsonFile("Weapons.json", new WeaponsTable());
            for (var i = 0; i < weaponsTable.Rows.Count; i++)
            {
                var row = weaponsTable.Rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.WeaponID))
                {
                    continue;
                }

                WeaponById[row.WeaponID] = row;
            }

            var lootTables = LoadJsonFile("LootTables.json", new LootTablesTable());
            for (var i = 0; i < lootTables.Rows.Count; i++)
            {
                var row = lootTables.Rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.LootTableID))
                {
                    continue;
                }

                LootTableById[row.LootTableID] = row;
            }

            var containers = LoadJsonFile("Containers.json", new ContainersTable());
            for (var i = 0; i < containers.Rows.Count; i++)
            {
                var row = containers.Rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.ContainerID))
                {
                    continue;
                }

                ContainerById[row.ContainerID] = row;
            }

            var fallback = LoadJsonFile("AddressFallbackMap.json", new AddressFallbackTable());
            for (var i = 0; i < fallback.Rows.Count; i++)
            {
                var row = fallback.Rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Address) || string.IsNullOrWhiteSpace(row.ResourcePath))
                {
                    continue;
                }

                AddressFallbackByKey[row.Address] = row.ResourcePath;
            }
        }

        public static bool TryGetItem(string itemId, out ItemRow row)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(itemId) && ItemById.TryGetValue(itemId, out row))
            {
                return true;
            }

            row = null;
            return false;
        }

        public static ItemRow GetItem(string itemId)
        {
            if (TryGetItem(itemId, out var row))
            {
                return row;
            }

            PublishDataMissing("Items", itemId, "DataRegistry.GetItem missing id");

            if (Debug.isDebugBuild || Application.isEditor)
            {
                throw new InvalidOperationException($"DataRegistry.GetItem missing id={itemId} dataVersion={DataVersion}");
            }

            return null;
        }

        public static bool TryGetWeapon(string weaponId, out WeaponRow row)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(weaponId) && WeaponById.TryGetValue(weaponId, out row))
            {
                return true;
            }

            row = null;
            return false;
        }

        public static bool TryGetContainer(string containerId, out ContainerRow row)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(containerId) && ContainerById.TryGetValue(containerId, out row))
            {
                return true;
            }

            row = null;
            return false;
        }

        public static bool TryGetLootTable(string lootTableId, out LootTableRow row)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(lootTableId) && LootTableById.TryGetValue(lootTableId, out row))
            {
                return true;
            }

            row = null;
            return false;
        }

        public static bool TryGetAddressFallback(string address, out string resourcePath)
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(address) && AddressFallbackByKey.TryGetValue(address, out resourcePath))
            {
                return true;
            }

            resourcePath = string.Empty;
            return false;
        }

        public static void PublishDataMissing(string domain, string dataId, string reason)
        {
            GameplayEventHub.Publish(new DataMissingEvent
            {
                EventId = GameplayEventIds.DataMissing,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = SystemActorId,
                DataDomain = domain,
                DataId = string.IsNullOrWhiteSpace(dataId) ? "<empty>" : dataId,
                DataVersion = DataVersion,
                SaveVersion = SaveVersion,
                Reason = reason,
            });
        }

        public static void PublishPrefabLoadFailed(string prefabKey, string reason)
        {
            GameplayEventHub.Publish(new PrefabLoadFailedEvent
            {
                EventId = GameplayEventIds.PrefabLoadFailed,
                Frame = Time.frameCount,
                Scene = SceneManager.GetActiveScene().name,
                ActorId = SystemActorId,
                PrefabKey = prefabKey,
                DataVersion = DataVersion,
                SaveVersion = SaveVersion,
                Reason = reason,
            });
        }

        private static SettingsRow ResolveSettingsMain(SettingsTable table)
        {
            if (table != null && table.Rows != null)
            {
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
                    if (row == null)
                    {
                        continue;
                    }

                    if (string.Equals(row.SettingsID, "SETTINGS_MAIN", StringComparison.Ordinal))
                    {
                        return row;
                    }
                }

                if (table.Rows.Count > 0 && table.Rows[0] != null)
                {
                    return table.Rows[0];
                }
            }

            PublishDataMissing("Settings", "SETTINGS_MAIN", "Settings data invalid");
            return new SettingsRow { SettingsID = "SETTINGS_MAIN" };
        }

        private static T LoadJsonFile<T>(string fileName, T fallback) where T : class
        {
            var json = LoadJsonText(fileName);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback;
            }

            try
            {
                var parsed = JsonUtility.FromJson<T>(json);
                return parsed ?? fallback;
            }
            catch (Exception)
            {
                PublishDataMissing("Generated", fileName, $"Json data invalid");
                return fallback;
            }
        }

        private static string LoadJsonText(string fileName)
        {
            var fullPath = GetGeneratedFilePath(fileName);
            if (File.Exists(fullPath))
            {
                return File.ReadAllText(fullPath);
            }

            var resourcePath = "ZomCity/Data/Generated/" + Path.GetFileNameWithoutExtension(fileName);
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null)
            {
                return textAsset.text;
            }

            PublishDataMissing("Generated", fileName, "Generated file not found");
            return string.Empty;
        }

        private static string GetGeneratedFilePath(string fileName)
        {
            var full = Path.Combine(Application.dataPath, "ZomCity", "Data", "Generated", fileName);
            return full;
        }
    }
}

