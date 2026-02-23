using System;
using System.Collections.Generic;

namespace ZomCity
{
    [Serializable]
    public sealed class DataVersionManifest
    {
        public string DataVersion = "UNKNOWN";
        public string SaveVersion = "UNKNOWN";
        public string ExportedAtUtc = string.Empty;
        public string SourceCdb = string.Empty;
        public List<string> GeneratedFiles = new List<string>();
    }

    [Serializable]
    public sealed class SettingsTable
    {
        public List<SettingsRow> Rows = new List<SettingsRow>();
    }

    [Serializable]
    public sealed class SettingsRow
    {
        public string SettingsID = string.Empty;
        public string GenericPickupPrefabKey = string.Empty;
    }

    [Serializable]
    public sealed class ItemsTable
    {
        public List<ItemRow> Rows = new List<ItemRow>();
    }

    [Serializable]
    public sealed class ItemRow
    {
        public string ItemID = string.Empty;
        public string Category = string.Empty;
        public string Rarity = string.Empty;
        public int StackMax = 1;
        public float Weight = 0f;
        public bool CanStoreInMeta = true;
        public bool CanDropInWorld = true;
        public string IconKey = string.Empty;
        public string NameKey = string.Empty;
        public string DescKey = string.Empty;
        public List<string> ItemTags = new List<string>();
        public string WorldPickupPrefabKey = string.Empty;
    }

    [Serializable]
    public sealed class WeaponsTable
    {
        public List<WeaponRow> Rows = new List<WeaponRow>();
    }

    [Serializable]
    public sealed class WeaponRow
    {
        public string WeaponID = string.Empty;
        public string ItemID = string.Empty;
        public int MagSize = 0;
        public float Rate = 0f;
        public float Damage = 0f;
        public float Spread = 0f;
        public float Recoil = 0f;
        public float NoiseRadius = 0f;
        public float ReloadTime = 0f;
        public string PrefabKey = string.Empty;
    }

    [Serializable]
    public sealed class LootTablesTable
    {
        public List<LootTableRow> Rows = new List<LootTableRow>();
    }

    [Serializable]
    public sealed class LootTableRow
    {
        public string LootTableID = string.Empty;
        public int RollMin = 1;
        public int RollMax = 1;
        public List<LootEntryRow> Entries = new List<LootEntryRow>();
    }

    [Serializable]
    public sealed class LootEntryRow
    {
        public string ItemID = string.Empty;
        public int Weight = 1;
        public int MinQty = 1;
        public int MaxQty = 1;
    }

    [Serializable]
    public sealed class ContainersTable
    {
        public List<ContainerRow> Rows = new List<ContainerRow>();
    }

    [Serializable]
    public sealed class ContainerRow
    {
        public string ContainerID = string.Empty;
        public string LootTableID = string.Empty;
        public float SearchTime = 0f;
        public string PrefabKey = string.Empty;
    }

    [Serializable]
    public sealed class AddressFallbackTable
    {
        public List<AddressFallbackRow> Rows = new List<AddressFallbackRow>();
    }

    [Serializable]
    public sealed class AddressFallbackRow
    {
        public string Address = string.Empty;
        public string ResourcePath = string.Empty;
    }
}
