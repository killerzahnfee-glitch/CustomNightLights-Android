using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
        Monitor.Log("Light radius multiplier: " + config.RadiusMultiplier, LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !e.IsMultipleOf(5)) return;
        double multiplier = Math.Clamp(config.RadiusMultiplier, 0.1, 10.0);
        sourcesField ??= typeof(Game1).GetField("currentLightSources", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        sourcesProperty ??= typeof(Game1).GetProperty("currentLightSources", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        object? sources = sourcesField?.GetValue(null) ?? sourcesProperty?.GetValue(null);
        if (sources is not IEnumerable enumerable) return;
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

    private sealed class OriginalRadius(float value) { public float Value { get; } = value; }
}
public sealed class ModConfig { public double RadiusMultiplier { get; set; } = 2.0; }
