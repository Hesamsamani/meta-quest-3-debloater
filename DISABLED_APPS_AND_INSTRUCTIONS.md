# Disabled Apps & Services Log + Complete Usage Instructions

**Device Verified:** Meta Quest 3 (`eureka` • Serial `2G0YC5ZH0L0126` • Android 14 / `UP1A.231005.007.A1`)  
**Method Used:** Reversible user-space disable (`pm disable-user --user 0 <package>`) — zero files were uninstalled, and VR Home (`com.oculus.vrshell/.HomeActivity`) was verified intact after every package.  
**Pre-existing Disabled Package (Untouched System Default):** `com.meta.credentialsmanager` (`1 / 396` before our run -> **`48 / 396`** after disabling all `47` `SAFE` bloatware apps/services listed below — `28` initial + `19` uncovered in the deep second-pass scan).  
**OS-Protected Note:** `com.android.devicelockcontroller` was also evaluated during the deep scan, refused by Horizon OS with `SecurityException: Cannot disable a protected package`, and kept excluded from the safe catalog.

---

## 1. Complete List of Apps & Services Turned Off (47 Packages)

### A. Telemetry, Advertising, QA Automation & Enterprise Services (20 Background Services)

| # | Friendly Name | Package ID | Purpose / What Turning It Off Stops |
|---:|---|---|---|
| 1 | **Meta Horizon Ad ID Service** | `com.meta.horizonadidservice` | Meta Horizon advertising identifier tracking and ad attribution service. |
| 2 | **Meta Federated Computing** | `com.meta.federatedcomputing.oculus` | Background federated machine learning and telemetry model training. |
| 3 | **Android AdServices API** | `com.android.adservices.api` | Android Privacy Sandbox ad targeting and attribution API. |
| 4 | **Android Extended AdServices** | `com.android.ext.adservices.api` | Extended Android Privacy Sandbox advertising background service. |
| 5 | **On-Device Ad Personalization** | `com.android.ondevicepersonalization.services` | On-device behavioral ad personalization engine. |
| 6 | **Android Federated Compute** | `com.android.federatedcompute.services` | Background Android federated telemetry training daemon. |
| 7 | **Oculus Bug Report Uploader** | `com.oculus.bugreportuploader` | Background diagnostic dump and crash log uploader to Meta servers. |
| 8 | **Oculus Bug Report Service** | `com.oculus.bugreportservice` | Background diagnostic capture and bug report collection daemon. |
| 9 | **Quick Promotion / Upsells** | `com.oculus.quickpromotionservice` | Pushes promotional banners, Horizon+ upsells, and modal prompts in VR. |
| 10 | **Post-Update Onboarding Prompts** | `com.oculus.nux.ota` | Post-OTA new-user experience onboarding and promotional cards. |
| 11 | **Meta Cloud Backup Transport** | `com.oculus.backuptransportservice` | Cloud app backup sync transport to Meta servers (ADB backup unaffected). |
| 12 | **Quest for Business MDM** | `com.oculus.q4b.mdm` | Enterprise Mobile Device Management agent (unused on consumer headsets). |
| 13 | **Wearable Location Service** | `com.facebook.wearable.system.location.service` | Resident background wearable location tracking service (~110 MB RSS). |
| 14 | **Wearable Location Proxy** | `com.facebook.wearable.system.location.proxy` | Resident background proxy for Meta wearable location polling (~87 MB RSS). |
| 15 | **Oculus Lab Service (A/B Experiments)** *(Deep Scan)* | `com.oculus.labservice` | Active background A/B testing & feature-experimentation telemetry daemon (~93 MB PSS). |
| 16 | **Oculus Enterprise OEM Config** *(Deep Scan)* | `com.oculus.oemconfig` | Enterprise MDM managed-device OEM configuration agent (~107 MB PSS). |
| 17 | **Meta Pauldron VR QA Automation** *(Deep Scan)* | `com.meta.automation.pauldron.vr` | Internal Meta QA automated test runner agent left in retail firmware. |
| 18 | **Oculus App Automation Service** *(Deep Scan)* | `com.oculus.appautomation` | Automated UI/app test harness service used for internal QA testing. |
| 19 | **Android CTS Test Shim** *(Deep Scan)* | `com.android.cts.ctsshim` | Empty Android Compatibility Test Suite (CTS) validation shim. |
| 20 | **Android Privileged CTS Test Shim** *(Deep Scan)* | `com.android.cts.priv.ctsshim` | Empty privileged Android Compatibility Test Suite (CTS) validation shim. |

### B. Preinstalled Social Platforms (4 Apps)

| # | Friendly Name | Package ID | Purpose / What Turning It Off Stops |
|---:|---|---|---|
| 1 | **Meta Horizon Worlds** | `com.facebook.horizon` | Preinstalled Meta Horizon Worlds client and Horizon Feed integration. |
| 2 | **Facebook VR App** | `com.oculus.facebook` | Preinstalled Facebook social VR wrapper application. |
| 3 | **Instagram VR App** | `com.oculus.igvr` | Preinstalled Instagram VR wrapper application. |
| 4 | **Threads VR App** | `com.oculus.thvr` | Preinstalled Threads social media VR wrapper application. |

### C. Preinstalled Demos, Assistants, Onboarding & Unused Android Phone/TV Stubs (23 Apps & Services)

| # | Friendly Name | Package ID | Purpose / What Turning It Off Stops |
|---:|---|---|---|
| 1 | **Theater Elsewhere / Quill Player** | `com.facebook.arvr.quillplayer` | Preinstalled Quill VR animation viewer and Showcase demos. |
| 2 | **First Hand Demo** | `com.oculus.samples.firsthand` | Preinstalled Meta hand-tracking onboarding demonstration app. |
| 3 | **Hands Education Module** | `com.meta.handseducationmodule` | Interactive tutorial overlay for Quest hand gestures. |
| 4 | **Curio Mixed-Reality Ruler** | `com.meta.curio.ruler` | Preinstalled MR ruler novelty utility. |
| 5 | **Curio Mixed-Reality Toybox** | `com.meta.curio.toybox` | Preinstalled MR toybox novelty mini-app. |
| 6 | **Meta Voice Assistant** | `com.oculus.assistant` | Meta voice command listener and assistant background daemon. |
| 7 | **Quest Guidebook** | `com.oculus.guidebook` | Resident background tips & onboarding guidebook service (~112 MB RSS). |
| 8 | **Meta Help Center** | `com.oculus.helpcenter` | Meta support articles & troubleshooting viewer (~118 MB cached RSS). |
| 9 | **VR Privacy Checkup Prompts** | `com.oculus.vrprivacycheckup` | Periodic Meta privacy checkup wizard notifications. |
| 10 | **Oculus Calendar Panel** | `com.oculus.panelapp.calendar` | Legacy Horizon Home calendar dock widget. |
| 11 | **Meta NUX Onboarding Daemon** *(Deep Scan)* | `com.meta.environment.prod.nuxd` | New-User Experience (NUX) onboarding daemon not needed after setup. |
| 12 | **Meta Surface Typing Tutorial (NUX)** *(Deep Scan)* | `com.meta.surfacetypingnux` | Onboarding tutorial prompt for direct-touch surface typing. |
| 13 | **Meta Hyperscape Room Capture** *(Deep Scan)* | `com.meta.HyperscapeHmdCapture` | Cloud room-photogrammetry capture app (keeps local Guardian intact). |
| 14 | **Oculus Retail Kiosk Mode Panel** *(Deep Scan)* | `com.oculus.panelapp.kiosk` | Commercial / retail store demo kiosk mode panel. |
| 15 | **Microsoft Windows App (RDP Stub)** *(Deep Scan)* | `com.microsoft.rdc.androidx` | Preinstalled Microsoft Remote Desktop client stub. |
| 16 | **Android Work Profile Provisioning** *(Deep Scan)* | `com.android.managedprovisioning` | Corporate BYOD work-profile MDM provisioning wizard (~95 MB PSS). |
| 17 | **Android TV Live Channels Provider** *(Deep Scan)* | `com.android.providers.tv` | Android TV broadcast tuner database provider unused on VR headsets (~99 MB PSS). |
| 18 | **Android Calendar Storage Provider** *(Deep Scan)* | `com.android.providers.calendar` | Background calendar database provider that still wakes up in RAM (~99 MB PSS). |
| 19 | **Android Health Connect Controller** *(Deep Scan)* | `com.android.healthconnect.controller` | Android 14 phone fitness/health data sharing controller UI. |
| 20 | **Android Health Connect Backup** *(Deep Scan)* | `com.android.health.connect.backuprestore` | Background cloud backup helper for Android Health Connect. |
| 21 | **Android Legacy Bookmark Provider** *(Deep Scan)* | `com.android.bookmarkprovider` | Obsolete AOSP browser bookmark content provider. |
| 22 | **Android Call Blocked Number Provider** *(Deep Scan)* | `com.android.providers.blockednumber` | Telephony call/SMS spam blocking database (Quest has no cellular radio). |
| 23 | **Android Nearby Share Halfsheet** *(Deep Scan)* | `com.android.nearby.halfsheet` | Google Nearby device pairing pop-up sheet stub. |

---

## 2. How to Use This Tool (`Quest Debloat`)

### Folder Contents
```text
Quest Debloat/
├── dist/QuestDebloater.exe               # Standalone Windows Dark-Mode GUI + CLI
├── DISABLED_APPS_AND_INSTRUCTIONS.md     # This reference & disabled package log
├── README.md                             # Project overview, benchmarks & screenshots
├── quest-debloat-selector.html           # Interactive browser script generator
├── configs/
│   ├── packages-catalog.json             # 84-package tiered Horizon OS catalog
│   └── adguard-meta-telemetry.txt        # Optional AdGuard Home / Pi-hole DNS blocklist
├── scripts/
│   ├── debloat.ps1 / restore.ps1         # Windows PowerShell scripts
│   └── debloat.sh  / restore.sh          # macOS / Linux / WSL Bash scripts
├── backups/                              # Saved pre-debloat & timestamped JSON/TXT snapshots
├── docs/screenshot-gui.png               # Live GUI verification screenshot
└── src/QuestDebloater/                   # Full .NET 8 C# source code
```

---

## 3. Running the GUI & Common Tasks

### Prerequisites
1. Enable **Developer Mode** in the Meta Horizon mobile app (*Menu -> Devices -> Headset Settings -> Developer Mode*).
2. Connect the Quest 3 via USB-C (or Wireless ADB) and accept **Allow USB debugging** inside the headset.

### Method 1: Desktop GUI (`dist\QuestDebloater.exe`)
Double-click `dist\QuestDebloater.exe` to open the interface:
- **Scan Headset**: Detects your Quest 3, Horizon OS build, battery, free RAM, live per-package RSS memory usage, and enabled/disabled status.
- **Presets**:
  - **Safe Recommended (47)** *(already applied to your headset)* — Telemetry, ads, promo upsells, QA/enterprise agents, social wrappers, demo bloat, and unused phone/TV stubs.
  - **Telemetry & Ads (20)** — Strictly invisible background tracking, experimentation, QA automation, & promotional services only.
  - **PCVR / Minimalist (59)** — Adds Social Platform (`com.oculus.socialplatform`), Presence, Avatar Editor, Quest TV, Remote Desktop, Horizon Media Player (`com.oculus.horizonmediaplayer`), Accounts Center, and 4 extra Vista home themes (keeps Haven 2025, Store, Browser, and Virtual Desktop/OpenXR untouched).
- **Filter Tabs & Search**: Browse by category (`Telemetry & Ads`, `Social & Horizon`, `Bloat & Demos`, `Environments`, `Caution`, `Protected`, `Disabled Only`) or filter by keyword.
- **Protected Lock**: Core VR Shell, SystemUX, Guardian, OpenXR, OCMS, and drivers are hard-locked so they cannot be accidentally disabled.
- **Quest 3 Quick Tweaks Sidebar**:
  - Refresh Rate: `72 Hz`, `90 Hz`, `120 Hz`
  - Fixed Foveated Rendering (`FFR`): `Off`, `Med`, `High`, `Dyn`
  - CPU/GPU Clocks: `Auto` or `Max Level 4`
  - `FPS HUD On/Off`, `Keep Awake / Auto Sleep`, `Guardian On/Off`, `Free RAM (Kill Bloat)`, `Restart VR Shell`.

### Method 2: Command Line (CLI)
```powershell
# Check current status, memory, and disabled counts
.\dist\QuestDebloater.exe status

# Save a fresh timestamped backup snapshot
.\dist\QuestDebloater.exe backup

# Apply any preset (telemetry | safe | pcvr)
.\dist\QuestDebloater.exe apply --preset safe

# Re-enable ALL catalog packages back to stock
.\dist\QuestDebloater.exe restore
```

### Method 3: Standalone PowerShell or Bash Scripts
```powershell
# Windows PowerShell
pwsh .\scripts\debloat.ps1 -Preset safe
pwsh .\scripts\restore.ps1
```
```bash
# macOS / Linux / WSL / Git Bash
./scripts/debloat.sh safe
./scripts/restore.sh
```

---

## 4. How to Re-Enable (Undo) Any App or Service

- **Restore everything at once** (returns headset to exact pre-debloat state):
  - Click **Restore All Meta Defaults** in `QuestDebloater.exe`, **or** run `.\dist\QuestDebloater.exe restore`, **or** run `pwsh .\scripts\restore.ps1`.
- **Re-enable a single app or service** (for example, Meta AI Voice Assistant or Horizon Worlds):
  - In `QuestDebloater.exe`: filter by **Disabled Only**, check the app, and click **Re-Enable Selected**.
  - Or via ADB command:
    ```bash
    adb shell pm enable <package_id>
    # Examples:
    adb shell pm enable com.oculus.assistant
    adb shell pm enable com.facebook.horizon
    ```

---

## 5. Optional Router / DNS Telemetry Blocking
Some OS-level packages (like `com.oculus.updater`) are protected by Horizon OS and cannot be disabled by ADB. To block Meta telemetry or firmware updates at the network level, copy the rules from [`configs/adguard-meta-telemetry.txt`](configs/adguard-meta-telemetry.txt) into **AdGuard Home** (*Filters -> Custom filtering rules*) or **Pi-hole**.
