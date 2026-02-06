using System;

namespace ZomCity
{
    /// <summary>
    /// Single-source-of-truth constants for project paths and cross-system contracts.
    /// Tools and runtime code should reference this class rather than hard-coding strings.
    /// </summary>
    public static class ZomCityProjectConstants
    {
        public static class Paths
        {
            // Paths are project-root-relative (not absolute).
            public const string DocsRoot = "Docs/ZomCity/";
            public const string ReportsOutDir = "TempLogs/ZomCityReports/";

            public const string DesignRoot = "Assets/ZomCity/Data/Design/";
            public const string GeneratedRoot = "Assets/ZomCity/Data/Generated/";
            public const string ContentRoot = "Assets/ZomCity/Content/";
        }

        public static class Reports
        {
            public const string TimestampFormat = "yyyyMMdd_HHmmss";
            public const int Retention = 20;

            public static string FormatJson(string reportName, DateTime utcNow)
                => $"{reportName}_{utcNow:yyyyMMdd_HHmmss}.json";
        }

        public static class Addressables
        {
            public const string AddressPrefix = "ZC";

            public static readonly string[] DomainWhitelist =
            {
                "Enemies",
                "Rooms",
                "Containers",
                "Items",
                "UI",
                "VFX",
                "Audio",
            };

            public static string MakeAddress(string domain, string stableId)
                => $"{AddressPrefix}/{domain}/{stableId}";
        }

        public static class UnityTags
        {
            public static readonly string[] Whitelist =
            {
                "Player",
                "Enemy",
                "Interactable",
                "Container",
            };
        }

        public static class StableIds
        {
            public static readonly string[] PrefixWhitelist =
            {
                "MAT_",
                "CON_",
                "WPN_",
                "ATT_",
                "DATA_",
                "LT_",
                "CT_",
                "EN_",
                "RM_",
                "ZN_",
                "RC_",
                "UP_",
            };
        }
    }
}
