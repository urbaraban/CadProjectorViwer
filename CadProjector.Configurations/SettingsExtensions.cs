using System;

namespace CadProjector.Configurations
{
    public static class SettingsExtensions
    {
        public static T GetValueOrDefault<T>(this ISettingsProvider settings, string key, T defaultValue = default)
     {
try
  {
 var value = settings.GetValue(key);
     if (value == null) return defaultValue;
     return (T)Convert.ChangeType(value, typeof(T));
 }
            catch
    {
      return defaultValue;
      }
        }
    }

    public interface ISettingsProvider
    {
        object GetValue(string key);
        void SetValue(string key, object value);
        void Save();
    }
}