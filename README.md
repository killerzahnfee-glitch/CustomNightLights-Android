# Adjustable Light Radius — Android prototype

This is a new implementation of the light-radius feature, not a decompilation of the binary-only CustomNightLights mod. **Not compiled or tested on Android yet.** Do not install together with the original mod.

Target environment: Stardew Valley 1.6.15.3, Android SMAPI 4.3.2.5 (compatibility not verified).

Set `RadiusMultiplier` in `config.json` (2.0 = twice the radius). Light colors are unchanged.

## Build requirements
.NET SDK and the corresponding Stardew Valley + SMAPI assemblies are required. The `Pathoschild.Stardew.ModBuildConfig` package supports builds against a local game installation. GitHub-hosted runners do not have Stardew Valley's proprietary game files, so a functional CI build cannot be guaranteed without supplying those dependencies lawfully.

## Limitations
Reflection accesses the light-source collection and its radius field. Android builds may differ, and not all lights may be covered. Verify with a SMAPI log and actual in-game testing.
