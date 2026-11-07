using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Globalization;

namespace AdjustableLightRadius;

public sealed class ModEntry : Mod
{
    private ModConfig config = new();
    private readonly ConditionalWeakTable<object, OriginalRadius> originals = new();
    private FieldInfo? sourcesField;
    private PropertyInfo? sourcesProperty;
    private readonly Dictionary<Type, MemberInfo?> radiusMembers = new();

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        Monitor.Log($"Light radius multipliers: indoors={config.IndoorRadiusMultiplier}, outdoors={config.OutdoorRadiusMultiplier}", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var menu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (menu is null)
        {
            Monitor.Log("Generic Mod Config Menu not found; config.json remains available.", LogLevel.Info);
            return;
        }

        menu.Register(ModManifest, () => config = new ModConfig(), () => Helper.WriteConfig(config));
        menu.AddNumberOption(
            ModManifest,
            () => config.IndoorRadiusMultiplier,
            value => config.IndoorRadiusMultiplier = value,
            () => "Lichtradius drinnen",
            () => "Multiplikator für Lichtquellen in Gebäuden (1× bis 8×).",
            min: 1f, max: 8f, interval: 0.5f,
            formatValue: value => value.ToString("0.0", CultureInfo.InvariantCulture) + "×"
        );
        menu.AddNumberOption(
            ModManifest,
            () => config.OutdoorRadiusMultiplier,
            value => config.OutdoorRadiusMultiplier = value,
            () => "Lichtradius draußen",
            () => "Multiplikator für Lichtquellen im Freien (1× bis 3×).",
            min: 1f, max: 3f, interval: 0.5f,
            formatValue: value => value.ToString("0.0", CultureInfo.InvariantCulture) + "×"
        );
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(5)) return;
        bool outdoors = Game1.currentLocation?.IsOutdoors ?? true;
        double multiplier = outdoors
            ? Math.Clamp(config.OutdoorRadiusMultiplier, 1.0, 3.0)
            : Math.Clamp(config.IndoorRadiusMultiplier, 1.0, 8.0);

        sourcesField ??= typeof(Game1).GetField("currentLightSources", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        sourcesProperty ??= typeof(Game1).GetProperty("currentLightSources", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        object? sources = sourcesField?.GetValue(null) ?? sourcesProperty?.GetValue(null);
        IEnumerable? enumerable = sources is IDictionary dictionary ? dictionary.Values : sources as IEnumerable;
        if (enumerable is null) return;

        foreach (object? light in enumerable)
        {
            if (light is null) continue;
            Type type = light.GetType();
            if (!radiusMembers.TryGetValue(type, out MemberInfo? member))
            {
                member = (MemberInfo?)type.GetField("radius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? type.GetProperty("radius", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                radiusMembers[type] = member;
            }
            if (member is null) continue;
            try
            {
                object? raw = member is FieldInfo field ? field.GetValue(light) : ((PropertyInfo)member).GetValue(light);
                if (raw is not float current) continue;
                OriginalRadius stored = originals.GetValue(light, _ => new OriginalRadius(current));
                float desired = (float)(stored.Value * multiplier);
                if (Math.Abs(current - desired) < 0.001f) continue;
                if (member is FieldInfo f) f.SetValue(light, desired);
                else if (member is PropertyInfo p && p.CanWrite) p.SetValue(light, desired);
            }
            catch (Exception ex)
            {
                Monitor.LogOnce("Unable to change a light radius: " + ex.Message, LogLevel.Warn);
            }
        }
    }

    private sealed class OriginalRadius
    {
        public float Value { get; }

        public OriginalRadius(float value)
        {
            Value = value;
        }
    }
}

public sealed class ModConfig
{
    public float IndoorRadiusMultiplier { get; set; } = 4.0f;
    public float OutdoorRadiusMultiplier { get; set; } = 2.0f;
}

public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddNumberOption(
        IManifest mod,
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null
    );
}
