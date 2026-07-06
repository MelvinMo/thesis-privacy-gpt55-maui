# Sleep Tracker — GPT-5.5 → .NET MAUI Migration

> **Thesis artifact 3 of 7.** This repository is one implementation from a McMaster University M.Sc. thesis studying whether AI coding agents can migrate a mobile health app across frameworks without degrading usability. See [Thesis citation](#thesis-citation) and [Related repositories](#related-repositories) below.

.NET MAUI (C#) rewrite of the original React Native "Sleep Tracker" privacy-transparency app, produced by **GPT-5.5** under a shared 15-rule migration prompt. It talks to the same Node.js/Express backend as the original app (see [thesis-privacy-baseline](https://github.com/MelvinMo/thesis-privacy-baseline)).

---

## Usability findings (from the thesis)

This migration was evaluated with Nielsen's ten usability heuristics across six standardized tasks by a single assessor (severity 0–4, lower is better). Full detail is in **Chapter 5** of the thesis (App 3).

| Metric | Value |
|---|---|
| Aggregate severity total | **28** |
| vs. baseline (React Native, total 16) | **+12** |
| Rank among all 7 implementations | **7th of 7 (worst)** |

This is the least usable of all seven implementations evaluated in the thesis — both AI coding agents produced their worst result on the MAUI target. Several source-specific controls were flattened into generic system dialogs (`DisplayPromptAsync`, `DisplayActionSheet`), the privacy tooltip omits tappable links to the privacy policy, PIPEDA regulation, and opt-out preferences, and edit affordances use plain text buttons rather than the source app's pencil icon. The project is organized into 33 files following the standard MAUI layout (Views/ViewModels split) — unlike GPT-5.5's KMP and Flutter migrations, which each concentrated all logic in a single file. See the thesis for the full per-heuristic breakdown and screenshots.

---

## Related repositories

| Repo | Description |
|---|---|
| [thesis-privacy-baseline](https://github.com/MelvinMo/thesis-privacy-baseline) | Original React Native app (unmodified snapshot) |
| [thesis-privacy-sonnet46-kmp](https://github.com/MelvinMo/thesis-privacy-sonnet46-kmp) | Claude Sonnet 4.6 → KMP |
| [thesis-privacy-sonnet46-flutter](https://github.com/MelvinMo/thesis-privacy-sonnet46-flutter) | Claude Sonnet 4.6 → Flutter |
| [thesis-privacy-sonnet46-maui](https://github.com/MelvinMo/thesis-privacy-sonnet46-maui) | Claude Sonnet 4.6 → .NET MAUI |
| [thesis-privacy-gpt55-kmp](https://github.com/MelvinMo/thesis-privacy-gpt55-kmp) | GPT-5.5 → KMP |
| [thesis-privacy-gpt55-flutter](https://github.com/MelvinMo/thesis-privacy-gpt55-flutter) | GPT-5.5 → Flutter |
| **thesis-privacy-gpt55-maui** | **This repo** — GPT-5.5 → .NET MAUI |

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 9.0+ | `dotnet --version` |
| .NET MAUI Workload | 9.0 | `dotnet workload install maui` |
| Android SDK / platform tools | API 21+ | |
| Android phone | Developer Options + USB or wireless debugging enabled | |

Check your setup:
```bash
dotnet --info
dotnet workload list
adb version
```
If MAUI is missing: `dotnet workload install maui`

---

## 1. Build the APK

```bash
dotnet restore
dotnet build SleepTrackerMaui.csproj -f net9.0-android
```

The debug APK is written to:
```
bin/Debug/net9.0-android/com.mcscert.sleeptracker.mauidev-Signed.apk
```

---

## 2. Connect a device

**USB:** enable Developer Options and USB Debugging on the phone, connect via cable, then:
```bash
adb devices
```

**Wireless (Android 11+):** on the phone, **Developer Options → Wireless debugging** → enable → **Pair device with pairing code**, then from your computer:
```bash
adb pair <phone-ip>:<pairing-port>
adb connect <phone-ip>:<port>   # the main IP & port shown on-screen, not the pairing port
adb devices
```

---

## 3. Install and run

```bash
adb install -r bin/Debug/net9.0-android/com.mcscert.sleeptracker.mauidev-Signed.apk
adb shell monkey -p com.mcscert.sleeptracker.mauidev 1
```

**Reinstalling cleanly** (e.g. after a code change) — if a plain install fails:
```bash
adb shell am force-stop com.mcscert.sleeptracker.mauidev
adb uninstall com.mcscert.sleeptracker.mauidev
dotnet build SleepTrackerMaui.csproj -f net9.0-android
adb install -r -d --no-incremental bin/Debug/net9.0-android/com.mcscert.sleeptracker.mauidev-Signed.apk
adb shell monkey -p com.mcscert.sleeptracker.mauidev 1
```
`--no-incremental` avoids stale or missing debug assemblies on Android for MAUI debug installs.

**Checking it's running / debugging a crash:**
```bash
adb shell pidof com.mcscert.sleeptracker.mauidev   # prints a PID if the process is alive
adb logcat -d -t 300 | grep -E "sleeptracker|FATAL EXCEPTION|AndroidRuntime|Unhandled Exception"
```

---

## 4. Point the app at a backend

Copy the example file for reference values:
```bash
cp .env.example .env
```

Backend URLs are compiled-in constants in `Services/AppConfig.cs`. Update `ApiUnencryptedUrl` to your machine's LAN IP (find it with `ipconfig`/`ifconfig`), or `ApiEncryptedUrl` to your own deployed backend, then rebuild. To run the backend locally, see [thesis-privacy-baseline](https://github.com/MelvinMo/thesis-privacy-baseline).

---

## Runtime permissions

Expect microphone, notification, and sensor-related permission prompts while testing sleep-mode flows — grant them for full functionality.

---

## What this project uses

- .NET 9 MAUI single-project app, MVVM via `CommunityToolkit.Mvvm` (`ObservableObject` + relay commands).
- `Microsoft.Data.Sqlite`, preserving the original `journals` and `sensor_data` table/column names.
- MAUI `SecureStorage` (Android Keystore / iOS Keychain-backed) for tokens.
- Android package ID: `com.mcscert.sleeptracker.mauidev`.

---

## Project structure

```
├── Views/                  # ContentPage subclasses (33 files, standard MAUI layout)
├── ViewModels/              # ObservableObject subclasses (Auth, Profile, Sleep, Journal, Statistics, Onboarding)
├── Services/
│   └── AppConfig.cs         # Backend URLs (see Section 4)
├── Models/
├── Platforms/Android/
├── Platforms/iOS/
├── .env.example             # Backend URL reference values (see Section 4)
└── .gitignore
```

---

## Known limitations (from the thesis)

- Bedtime/alarm entry, diary editing, and sleep-note selection use generic `DisplayPromptAsync`/`DisplayActionSheet` system dialogs rather than the source app's custom controls.
- The multi-select sleep-note interaction is constrained to single-select by `DisplayActionSheet`, even though the underlying toggle logic supports multi-select.
- The privacy tooltip omits tappable links to the PIPEDA regulation, Privacy Policy section, and Opt Out preferences.
- Edit affordances use plain "Edit" text buttons rather than the source app's pencil icon.
- See Chapter 5 of the thesis for the full task-by-task and heuristic-by-heuristic severity breakdown, including the two other GPT-5.5 migrations (KMP, Flutter).

---

## Environment variables & secrets

This repository contains **no real credentials**. `.env.example` holds placeholder/reference values only (a backend URL, not a secret). If you deploy your own backend, keep its real `.env` (JWT secret, database/Firebase keys, LLM API keys) out of version control — it's already covered by `.gitignore`.

---

## Thesis citation

If you reference this artifact, please cite:

> Mokhtari, M. (2026). *"Who Moved My Button?": A Usability Evaluation of LLM-Assisted Cross-Platform Migration* [Master's thesis, McMaster University]. Department of Computing and Software. Supervisor: Richard F. Paige.

---

## License

All rights reserved. This repository is published for academic review and reproducibility alongside the thesis above. No license is granted for reuse, modification, or redistribution without permission from the author.
