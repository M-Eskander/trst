# TRST — Survive the Waves

A fast-paced wave-based arena shooter where you fight procedurally generated enemies, collect XP, and upgrade your abilities. Built entirely at runtime — zero external assets, zero serialized scenes.

> Made by **[Claude (Anthropic)](https://claude.ai)** to demonstrate the ability to work with **Unity 6 (6000.3.6f1)** end-to-end: generating procedural content, architecting game systems, and producing a fully playable game from a blank project.

## 🎮 Gameplay

You control a neon ship in a closed arena. Waves of enemies spawn and grow harder over time. Survive as long as you can, collect XP orbs from killed enemies, level up, and choose upgrades that stack across runs.

- **5 enemy types**: Basic, Fast, Tank, Elite (gold variant), and Bosses (every 5th wave)
- **5 wave types**: Standard, Horde, Swarm, Bulwark, Elite — each with unique spawn composition
- **22 upgrades**: Damage, multi-shot, piercing, crit, chain lightning, frost aura, thorns, vampire, dash impact, and more
- **2 boss variations**: Ranged (burst projectiles) and Berserker (charge + heavy damage), each with 3 health phases

## 🎯 Controls

| Input | Action |
|---|---|
| **WASD** | Move |
| **Mouse** | Aim |
| **Left Click** | Shoot |
| **Shift (hold)** | Sprint |
| **Space** | Dash (brief invincibility) |
| **ESC** | Pause / Resume |

## ⚙️ How It Works

Everything is generated at runtime — no textures, models, or scenes to import:

- **Sprites** created via `Texture2D.SetPixels()` — player ship, enemy shapes, projectiles, XP orbs, hitmarker
- **Audio** synthesized via `AudioClip.Create()` — each shoot, hit, explosion, and the ambient soundtrack is a procedurally generated waveform
- **UI** built from `GameObject` + `RectTransform` + `TextMeshProUGUI` — no prefabs, no Canvas serialization
- **VFX** via on-the-fly `ParticleSystem` construction — impact sparks, explosions, heal effects, death bursts
- **Materials** via `Shader.Find("Universal Render Pipeline/Unlit")` — neon glow achieved through color + trail renderers

The entry point is `Bootstrapper.Init()` (a `RuntimeInitializeOnLoadMethod`), which spawns a `Bootstrap` GameObject. Its `Awake()` runs the entire setup pipeline in order.

## 🏗️ Project Structure

```
Assets/Scripts/
├── Audio/         AudioManager.cs — procedural SFX + music
├── Camera/        CameraFollow.cs — screen shake + player tracking
├── Enemies/       EnemyBase.cs, BossEnemy.cs, EnemySpawner.cs, ...
├── Game/          Bootstrap.cs, GameManager.cs, GameInitializer.cs, ProceduralAssets.cs
├── Pickups/       XPOrb.cs, XPOrbPool.cs
├── Player/        PlayerController.cs, PlayerInputHandler.cs, Projectile.cs
├── UI/            UIManager.cs, UpgradeCardUI.cs
├── Upgrades/      UpgradeManager.cs — 22 upgrade definitions
└── VFX/           VFXManager.cs, VisualEffects.cs
```

## 🚀 Build & Run

### From Unity Editor
1. Open the project folder in **Unity 6000.3.6f1** (or newer)
2. Open **File → Build Settings**
3. Add the `TRST` scene (or any scene — Bootstrap creates everything at runtime)
4. Click **Build** and run the executable

### From CLI (Linux)
```bash
/path/to/Unity -projectPath . -buildLinux64Player ./Builds/trst -quit -logFile build.log
```

### macOS / Windows
Same process — just change the build target in Build Settings.

## 📦 System Requirements

- **Unity 6000.3.6f1** (Universal Render Pipeline)
- **Rendering**: URP (Forward rendering)
- **Input**: New Input System (com.unity.inputsystem 1.18.0+)

## 🧪 Tested

Built and tested in Unity 6000.3.6f1 on Arch Linux. No external dependencies, no asset store packages, no serialized scene files.

---

*Made by [Claude](https://claude.ai) · June 2026*
