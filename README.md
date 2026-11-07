# Adjustable Light Radius — Android prototype

Independent implementation of light radius adjustment. **Not yet confirmed compiled or tested on Android.** Do not install alongside the original CustomNightLights.

Target: Stardew Valley 1.6.15.3 / Android SMAPI 4.3.2.5 (unverified).

## Configuration

Edit `config.json`:

```json
{
  "IndoorRadiusMultiplier": 4.0,
  "OutdoorRadiusMultiplier": 2.0
}
```

Indoor multiplier is limited to **1.0–8.0**; outdoor multiplier to **1.0–3.0**. These apply to light sources in the *current location*, according to whether the location is outdoors. Light colors remain unchanged.

## Build and testing

The GitHub Actions workflow attempts to build using public reference assemblies. Check Actions for success/failure. Android compatibility and light coverage require in-game verification. The code uses reflection to access game light sources and their radius member; some game builds and special lights may differ.

## Generic Mod Config Menu 2.1.0

Optional integration with `spacechase0.GenericModConfigMenu`. Open the mod's settings in GMCM to change **Lichtradius drinnen** (1–8×, default 4×) and **Lichtradius draußen** (1–3×, default 2×) in 0.5 increments. Saving writes `config.json`. GMCM is optional; the mod can also read the file directly. No light color settings are included.
