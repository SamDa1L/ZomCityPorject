using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZomCity
{
    public static class ZomCityDataBuildTools
    {
        private const string DataVersion = "M0_5_DEV_20260210_01";
        private const string SaveVersion = "SV_0_0_1";

        [MenuItem("Tools/ZomCity/M0.5/Export Deterministic Seed Data")]
        public static void ExportSeedData()
        {
            var designDir = AbsProjectPath(ZomCityProjectConstants.Paths.DesignRoot);
            var generatedDir = AbsProjectPath(ZomCityProjectConstants.Paths.GeneratedRoot);
            Directory.CreateDirectory(designDir);
            Directory.CreateDirectory(generatedDir);

            var cdbPath = Path.Combine(designDir, "ZomCityDesign.cdb");
            File.WriteAllText(cdbPath, BuildCdbSeedText());

            var settings = BuildSettingsTable();
            var items = BuildItemsTable();
            var weapons = BuildWeaponsTable();
            var lootTables = BuildLootTablesTable();
            var containers = BuildContainersTable();
            var fallback = BuildFallbackTable();

            WriteJson(Path.Combine(generatedDir, "Settings.json"), settings);
            WriteJson(Path.Combine(generatedDir, "Items.json"), items);
            WriteJson(Path.Combine(generatedDir, "Weapons.json"), weapons);
            WriteJson(Path.Combine(generatedDir, "LootTables.json"), lootTables);
            WriteJson(Path.Combine(generatedDir, "Containers.json"), containers);
            WriteJson(Path.Combine(generatedDir, "AddressFallbackMap.json"), fallback);

            var manifest = new DataVersionManifest
            {
                DataVersion = DataVersion,
                SaveVersion = SaveVersion,
                ExportedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                SourceCdb = ZomCityProjectConstants.Paths.DesignRoot + "ZomCityDesign.cdb",
                GeneratedFiles = new List<string>
                {
                    "Settings.json",
                    "Items.json",
                    "Weapons.json",
                    "LootTables.json",
                    "Containers.json",
                    "AddressFallbackMap.json",
                },
            };
            WriteJson(Path.Combine(generatedDir, "DataVersionManifest.json"), manifest);

            AssetDatabase.Refresh();
            Debug.Log("[ZomCity] M0.5 seed data export completed");
        }

        [MenuItem("Tools/ZomCity/M0.5/Run DataValidator")]
        public static void RunDataValidator()
        {
            var report = BuildValidationReport();
            var reportDir = AbsProjectPath(ZomCityProjectConstants.Paths.ReportsOutDir);
            Directory.CreateDirectory(reportDir);

            var fileName = ZomCityProjectConstants.Reports.FormatJson("DataValidatorReport", DateTime.UtcNow);
            var fullPath = Path.Combine(reportDir, fileName);
            File.WriteAllText(fullPath, JsonUtility.ToJson(report, true));

            CleanupOldReports(reportDir, "DataValidatorReport_*.json", ZomCityProjectConstants.Reports.Retention);
            AssetDatabase.Refresh();

            if (report.Errors.Count == 0)
            {
                Debug.Log($"[ZomCity] DataValidator passed. Report: {fullPath}");
            }
            else
            {
                Debug.LogError($"[ZomCity] DataValidator failed. ErrorCount={report.Errors.Count}. Report: {fullPath}");
            }
        }

        [MenuItem("Tools/ZomCity/M0.5/Export And Validate")]
        public static void ExportAndValidate()
        {
            ExportSeedData();
            RunDataValidator();
        }

        private static DataValidatorReport BuildValidationReport()
        {
            var generatedDir = AbsProjectPath(ZomCityProjectConstants.Paths.GeneratedRoot);

            var report = new DataValidatorReport
            {
                ReportName = "DataValidatorReport",
                GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Errors = new List<string>(),
                Warnings = new List<string>(),
            };

            var manifest = ReadJson<DataVersionManifest>(Path.Combine(generatedDir, "DataVersionManifest.json"));
            if (manifest == null)
            {
                report.Errors.Add("Json data invalid");
                report.DataVersion = "UNKNOWN";
                report.SaveVersion = "UNKNOWN";
                return report;
            }

            report.DataVersion = manifest.DataVersion;
            report.SaveVersion = manifest.SaveVersion;

            var settings = ReadJson<SettingsTable>(Path.Combine(generatedDir, "Settings.json")) ?? new SettingsTable();
            var items = ReadJson<ItemsTable>(Path.Combine(generatedDir, "Items.json")) ?? new ItemsTable();
            var weapons = ReadJson<WeaponsTable>(Path.Combine(generatedDir, "Weapons.json")) ?? new WeaponsTable();
            var lootTables = ReadJson<LootTablesTable>(Path.Combine(generatedDir, "LootTables.json")) ?? new LootTablesTable();
            var containers = ReadJson<ContainersTable>(Path.Combine(generatedDir, "Containers.json")) ?? new ContainersTable();

            ValidateSettings(settings, report);
            var itemSet = ValidateItems(items, report);
            ValidateWeapons(weapons, itemSet, report);
            var lootSet = ValidateLootTables(lootTables, itemSet, report);
            ValidateContainers(containers, lootSet, report);

            return report;
        }

        private static void ValidateSettings(SettingsTable settings, DataValidatorReport report)
        {
            if (settings.Rows == null || settings.Rows.Count == 0)
            {
                report.Errors.Add("Settings data invalid");
                return;
            }

            var countMain = 0;
            for (var i = 0; i < settings.Rows.Count; i++)
            {
                var row = settings.Rows[i];
                if (row == null)
                {
                    continue;
                }

                if (string.Equals(row.SettingsID, "SETTINGS_MAIN", StringComparison.Ordinal))
                {
                    countMain++;
                }

                if (!PrefabKeyValidator.TryValidate(row.GenericPickupPrefabKey, out var reason))
                {
                    report.Errors.Add($"Settings data invalid");
                }
            }

            if (countMain != 1)
            {
                report.Errors.Add($"Settings data invalid");
            }
        }

        private static HashSet<string> ValidateItems(ItemsTable items, DataValidatorReport report)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < items.Rows.Count; i++)
            {
                var row = items.Rows[i];
                if (row == null)
                {
                    report.Errors.Add($"Items data invalid");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.ItemID))
                {
                    report.Errors.Add($"Items data invalid");
                    continue;
                }

                if (!seen.Add(row.ItemID))
                {
                    report.Errors.Add($"Items data invalid");
                }
            }

            return seen;
        }

        private static void ValidateWeapons(WeaponsTable weapons, HashSet<string> itemSet, DataValidatorReport report)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < weapons.Rows.Count; i++)
            {
                var row = weapons.Rows[i];
                if (row == null)
                {
                    report.Errors.Add($"Weapons data invalid");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.WeaponID))
                {
                    report.Errors.Add($"Weapons data invalid");
                    continue;
                }

                if (!seen.Add(row.WeaponID))
                {
                    report.Errors.Add($"Weapons data invalid");
                }

                if (string.IsNullOrWhiteSpace(row.ItemID) || !itemSet.Contains(row.ItemID))
                {
                    report.Errors.Add($"Weapons data invalid");
                }

                if (!string.IsNullOrWhiteSpace(row.PrefabKey) && !PrefabKeyValidator.TryValidate(row.PrefabKey, out var reason))
                {
                    report.Errors.Add($"PrefabKey invalid");
                }
            }
        }

        private static HashSet<string> ValidateLootTables(LootTablesTable lootTables, HashSet<string> itemSet, DataValidatorReport report)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < lootTables.Rows.Count; i++)
            {
                var row = lootTables.Rows[i];
                if (row == null)
                {
                    report.Errors.Add($"LootTables data invalid");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.LootTableID))
                {
                    report.Errors.Add($"LootTables data invalid");
                    continue;
                }

                if (!seen.Add(row.LootTableID))
                {
                    report.Errors.Add($"LootTables data invalid");
                }

                if (row.Entries == null || row.Entries.Count == 0)
                {
                    report.Warnings.Add($"LootTables data invalid");
                    continue;
                }

                for (var j = 0; j < row.Entries.Count; j++)
                {
                    var entry = row.Entries[j];
                    if (entry == null)
                    {
                        report.Errors.Add($"LootTables data invalid");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.ItemID) || !itemSet.Contains(entry.ItemID))
                    {
                        report.Errors.Add($"LootTables data invalid");
                    }

                    if (entry.Weight <= 0)
                    {
                        report.Errors.Add($"LootTables data invalid");
                    }
                }
            }

            return seen;
        }

        private static void ValidateContainers(ContainersTable containers, HashSet<string> lootSet, DataValidatorReport report)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < containers.Rows.Count; i++)
            {
                var row = containers.Rows[i];
                if (row == null)
                {
                    report.Errors.Add($"Containers data invalid");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.ContainerID))
                {
                    report.Errors.Add($"Containers data invalid");
                    continue;
                }

                if (!seen.Add(row.ContainerID))
                {
                    report.Errors.Add($"Containers data invalid");
                }

                if (string.IsNullOrWhiteSpace(row.LootTableID) || !lootSet.Contains(row.LootTableID))
                {
                    report.Errors.Add($"Containers data invalid");
                }

                if (!string.IsNullOrWhiteSpace(row.PrefabKey) && !PrefabKeyValidator.TryValidate(row.PrefabKey, out var reason))
                {
                    report.Errors.Add($"PrefabKey invalid");
                }
            }
        }

        private static string BuildCdbSeedText()
        {
            return "{\n" +
                   "  \"source\": \"CastleDB\",\n" +
                   "  \"note\": \"M0.5 seed placeholder. Edit via CastleDB tool before production.\",\n" +
                   "  \"dataVersion\": \"" + DataVersion + "\",\n" +
                   "  \"saveVersion\": \"" + SaveVersion + "\"\n" +
                   "}\n";
        }
        private static SettingsTable BuildSettingsTable()
        {
            return new SettingsTable
            {
                Rows = new List<SettingsRow>
                {
                    new SettingsRow
                    {
                        SettingsID = "SETTINGS_MAIN",
                        GenericPickupPrefabKey = "ZC/Items/MAT_SCRAP",
                    },
                },
            };
        }

        private static ItemsTable BuildItemsTable()
        {
            return new ItemsTable
            {
                Rows = new List<ItemRow>
                {
                    BuildItem("MAT_SCRAP", "Material"),
                    BuildItem("MAT_PARTS", "Material"),
                    BuildItem("MAT_ELECTRONICS", "Material"),
                    BuildItem("MAT_CHEM", "Material"),
                    BuildItem("MAT_FABRIC", "Material"),
                    BuildItem("CON_BANDAGE", "Consumable"),
                    BuildItem("CON_O2", "Consumable"),
                    BuildItem("CON_DECOY", "Consumable"),
                    BuildItem("WPN_TEST_PISTOL", "Weapon"),
                },
            };
        }

        private static ItemRow BuildItem(string itemId, string category)
        {
            return new ItemRow
            {
                ItemID = itemId,
                Category = category,
                Rarity = "RAR_COMMON",
                StackMax = category == "Weapon" ? 1 : 99,
                Weight = category == "Weapon" ? 3.2f : 0.1f,
                CanStoreInMeta = true,
                CanDropInWorld = true,
                IconKey = "ICON_" + itemId,
                NameKey = "ITEM_NAME_" + itemId,
                DescKey = "ITEM_DESC_" + itemId,
                ItemTags = new List<string>(),
                WorldPickupPrefabKey = string.Empty,
            };
        }

        private static WeaponsTable BuildWeaponsTable()
        {
            return new WeaponsTable
            {
                Rows = new List<WeaponRow>
                {
                    new WeaponRow
                    {
                        WeaponID = "WPN_TEST_PISTOL",
                        ItemID = "WPN_TEST_PISTOL",
                        MagSize = 12,
                        Rate = 4.0f,
                        Damage = 15f,
                        Spread = 0.02f,
                        Recoil = 0.18f,
                        NoiseRadius = 8f,
                        ReloadTime = 1.4f,
                        PrefabKey = "ZC/Items/WPN_TEST_PISTOL",
                    },
                },
            };
        }

        private static LootTablesTable BuildLootTablesTable()
        {
            return new LootTablesTable
            {
                Rows = new List<LootTableRow>
                {
                    new LootTableRow
                    {
                        LootTableID = "LT_TEST_COMMON",
                        RollMin = 1,
                        RollMax = 2,
                        Entries = new List<LootEntryRow>
                        {
                            new LootEntryRow { ItemID = "MAT_SCRAP", Weight = 100, MinQty = 1, MaxQty = 3 },
                            new LootEntryRow { ItemID = "CON_BANDAGE", Weight = 40, MinQty = 1, MaxQty = 1 },
                        },
                    },
                },
            };
        }

        private static ContainersTable BuildContainersTable()
        {
            return new ContainersTable
            {
                Rows = new List<ContainerRow>
                {
                    new ContainerRow
                    {
                        ContainerID = "CT_TEST_LOCKER",
                        LootTableID = "LT_TEST_COMMON",
                        SearchTime = 0.8f,
                        PrefabKey = "ZC/Containers/CT_TEST_LOCKER",
                    },
                },
            };
        }

        private static AddressFallbackTable BuildFallbackTable()
        {
            return new AddressFallbackTable
            {
                Rows = new List<AddressFallbackRow>
                {
                    new AddressFallbackRow
                    {
                        Address = "ZC/Items/MAT_SCRAP",
                        ResourcePath = "ZomCity/MainCameraSidescroller",
                    },
                    new AddressFallbackRow
                    {
                        Address = "ZC/Containers/CT_TEST_LOCKER",
                        ResourcePath = "ZomCity/MainCameraSidescroller",
                    },
                },
            };
        }

        private static void WriteJson<T>(string path, T data)
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json + Environment.NewLine);
        }
        private static T ReadJson<T>(string path) where T : class
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void CleanupOldReports(string reportDir, string pattern, int keep)
        {
            var files = new DirectoryInfo(reportDir).GetFiles(pattern, SearchOption.TopDirectoryOnly);
            Array.Sort(files, (a, b) => b.CreationTimeUtc.CompareTo(a.CreationTimeUtc));

            for (var i = keep; i < files.Length; i++)
            {
                files[i].Delete();
            }
        }

        private static string AbsProjectPath(string projectRelative)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var normalized = projectRelative.Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(projectRoot, normalized));
        }

        [Serializable]
        private sealed class DataValidatorReport
        {
            public string ReportName;
            public string GeneratedAtUtc;
            public string DataVersion;
            public string SaveVersion;
            public List<string> Errors;
            public List<string> Warnings;
        }
    }
}
