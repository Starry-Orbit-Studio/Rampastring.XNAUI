using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace Rampastring.XNAUI
{
    public static class StringManager
    {
        private static Dictionary<string, string> _dictionary;
        private static Dictionary<string, string> _failbackDictionary;

        public static bool IsInitialized { get; private set; }

        public static void Initialize(Dictionary<string, string> map, Dictionary<string, string> failback)
        {
            if (IsInitialized)
                throw new Exception("StringManager has been inited");

            IsInitialized = true;
            _dictionary = map;
            _failbackDictionary = failback;
        }

        /// <summary>
        /// Get localized translation strings
        /// </summary>
        /// <param name="key"></param>
        /// <returns>localized translation strings</returns>
        public static string GetString(string key)
        {
            if ((_dictionary != null && _dictionary.TryGetValue(key, out string value))
                || (_failbackDictionary != null && _failbackDictionary.TryGetValue(key, out value)))
                if(!string.IsNullOrEmpty(value))
                    return value;
            Logger.Warn($"Cannot load {key}");
            return key;
        }
        /// <summary>
        /// Get localized translation strings and interpolate them
        /// </summary>
        /// <param name="key"></param>
        /// <param name="args">arguments</param>
        /// <returns>localized translation strings</returns>
        public static string GetString(string key, params string[] args) => string.Format(GetString(key), args);


        /// <summary>
        /// Display the name of the language provided by the failback localization file
        /// </summary>
        /// <returns>Language Name</returns>
        public static string GetFailbackName()
            => _failbackDictionary == null ? "None" : _failbackDictionary.TryGetValue("locale", out string value) ? value : "Unknown";

        /// <summary>
        /// Display the name of the language provided by the current localization file
        /// </summary>
        /// <returns>Language Name</returns>
        public static string GetCurrentName()
            => _dictionary == null ? "None" : _dictionary.TryGetValue("locale", out string value) ? value : "Unknown";

        /// <summary>
        /// Get UI String
        /// </summary>
        /// <param name="name"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        internal static string GetUIString(this XNAControls.XNAControl control, string property)
            => GetString(string.Join(".", new[] { "UI", control.Name, property }));
        internal static string GetUIStringEx(this XNAControls.XNAControl control, string property)
        {
            string origin = string.Join(".", new[] { "UI", control.Name, property });
            string value = control.GetUIString(property);
            if (value != origin)
                return value;

            string key = origin;

            Type type = control.GetType();

            while (value == key)
            {
                key = string.Join(".", new[] { "UI", type.Name, property });
                value = GetString(key);
                if (type == typeof(XNAControls.XNAControl))
                    break;

                type = type.BaseType;
            }
            return value != key ? value : origin;
        }
    }
}
