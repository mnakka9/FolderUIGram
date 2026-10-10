using Microsoft.Maui.Storage;

namespace FolderUIGram
{
    public static class ThemeService
    {
        private const string PrefKey = "AppTheme";
        private static ResourceDictionary? _current;

        public static readonly IReadOnlyList<string> Names = new[] { "Arctic Breeze", "Sage Mist", "Champagne Rose" };

        private static readonly Dictionary<string, Func<ResourceDictionary>> Factories = new()
        {
            ["Arctic Breeze"] = () => new Resources.Themes.ArcticBreeze(),
            ["Sage Mist"] = () => new Resources.Themes.SageMist(),
            ["Champagne Rose"] = () => new Resources.Themes.ChampagneRose(),
        };

        public static string CurrentName { get; private set; } = Names[0];

        public static void ApplySaved() => Apply(Preferences.Default.Get(PrefKey, Names[0]), save: false);

        public static void Apply(string name, bool save = true)
        {
            if (!Factories.TryGetValue(name, out var factory)) name = Names[0];
            var merged = Application.Current?.Resources.MergedDictionaries;
            if (merged == null) return;

            var next = Factories[name]();
            if (_current != null) merged.Remove(_current);
            merged.Add(next);
            _current = next;
            CurrentName = name;
            if (save) Preferences.Default.Set(PrefKey, name);
        }
    }
}
