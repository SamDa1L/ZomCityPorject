using System;
using System.Text.RegularExpressions;

namespace ZomCity
{
    public static class PrefabKeyValidator
    {
        private static readonly Regex StableIdRegex = new Regex("^[A-Z][A-Z0-9_]{2,63}$", RegexOptions.Compiled);

        public static bool TryValidate(string prefabKey, out string reason)
        {
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(prefabKey))
            {
                reason = "PrefabKey invalid";
                return false;
            }

            var parts = prefabKey.Split('/');
            if (parts.Length != 3)
            {
                reason = "PrefabKey invalid";
                return false;
            }

            if (!string.Equals(parts[0], ZomCityProjectConstants.Addressables.AddressPrefix, StringComparison.Ordinal))
            {
                reason = "PrefabKey invalid";
                return false;
            }

            var domain = parts[1];
            var stableId = parts[2];

            if (!IsWhitelistedDomain(domain))
            {
                reason = "Domain is not in whitelist";
                return false;
            }

            if (!StableIdRegex.IsMatch(stableId))
            {
                reason = "StableID does not match required regex";
                return false;
            }

            return true;
        }

        private static bool IsWhitelistedDomain(string domain)
        {
            var whitelist = ZomCityProjectConstants.Addressables.DomainWhitelist;
            for (var i = 0; i < whitelist.Length; i++)
            {
                if (string.Equals(whitelist[i], domain, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
