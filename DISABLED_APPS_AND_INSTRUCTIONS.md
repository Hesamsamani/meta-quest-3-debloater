# Disabled Apps & Services Log + Complete Usage Instructions

**Device Verified:** Meta Quest 3 (`eureka` • Serial `2G0YC5ZH0L0126` • Android 14 / `UP1A.231005.007.A1`)  
**Method Used:** Reversible user-space disable (`pm disable-user --user 0 <package>`) — zero files were uninstalled, and VR Home (`com.oculus.vrshell/.HomeActivity`) was verified intact after every package.  
**Pre-existing Disabled Package (Untouched System Default):** `com.meta.credentialsmanager` (`1 / 396` before our run -> **`51 / 396`** after disabling all `50` `SAFE` bloatware apps/services listed below — `28` initial + `19` second-pass + `3` third-pass forum/OS audit additions).  
**OS-Protected Note:** `com.android.devicelockcontroller` and Android 14 `PERSISTENT` system daemons such as `com.oculus.updater` are protected by Horizon OS (`SecurityException: Cannot disable a protected package`) and excluded from the `SAFE` preset.

---

## 0. Total Measured Resource Savings (Start to Finish)

| Resource | Measured Gain / Impact on `2G0YC5ZH0L0126` | How It Breaks Down |
| :--- | :--- | :--- |
| **RAM Freed** | **~1.0 GB+ resident/cached processes eliminated** (Pass 2: `+292 MB` net / `+813 MB` kill delta; Pass 3: `3245 -> 4067 MB` available, **`+822 MB` net / `+855 MB` kill delta**, `Free RAM: 4,547,270K` in `dumpsys meminfo`) | **Batch 1 (measured resident RSS):** Wearable Location Service (`~110 MB`), Location Proxy (`~87 MB`), Guidebook (`~112 MB`), Help Center (`~118 MB`).<br>**Batch 2 (measured live before disabling):** `labservice` (`97.3 MB` RSS / `~93 MB` PSS), `oemconfig` (`117.4 MB` RSS / `~107 MB` PSS), `managedprovisioning` (`~95 MB` PSS), `providers.tv` (`~99 MB` PSS), `providers.calendar` (`~99 MB` PSS).<br>**Batch 3:** `preshutdowntaskservice` (`~98.5 MB` RSS), plus quiescent `quillplayer` / `ovrmonitormetricsservice` / `linktabula.viewer`. |
| **CPU / Background Wakeups** | **`559% idle / 600%`** (~93% idle asleep in `top`) • **`574% idle`** measured post-Pass 2 • **`334% idle`** with VR Shell active at **72/73 FPS** | Eliminates periodic background CPU wakeups from ad SDKs, federated ML training, crash uploaders, lab experiments, enterprise MDM, QA automation, pre-shutdown dumpsys boot hooks, voice trigger polling, and Android TV/Calendar syncs. |
| **Hard Disk / Storage** | **`0 MB` deleted right now** (`439G / 449G` • `9.3 GB` free) — **`2.51 GiB` (`/data/app`) reclaimable** if uninstalled | Because `pm disable-user` preserves APKs for instant 1-click restore, disk usage does not drop until uninstalled:<br>• **10 removable `/data/app` bloat apps:** `1,660,252 KB` (`~1.58 GiB`) — can be reclaimed with `pm uninstall <pkg>`.<br>• **5 OTA-updated system apps in `/data/app`:** `969,316 KB` (`~947 MiB`, e.g., Horizon Worlds `621 MiB`, Hyperscape `185 MiB`) — updates removable via `pm uninstall-system-updates <pkg>`.<br>• **35 read-only ROM packages:** `104,896 KB` (`~102.4 MiB`, including `+384 KB` from Pass 3 stubs) on immutable `/system`, `/system_ext`, `/product`, `/apex` partitions (`2,734,464 KB` total mapped footprint). |

---

## 1. Complete List of Apps & Services Turned Off (50 Packages)

### A. Telemetry, Advertising, QA Automation & Enterprise Services (21 Background Services)

| # | Friendly Name | Package ID | Affects (UAD Subsystem) | Measured RAM / APK Disk Footprint | Purpose / What Turning It Off Stops |
|---:|---|---|---|---|---|
| 1 | **Meta Horizon Ad ID Service** | `com.meta.horizonadidservice` | Ad Tracking & Personalized Ad ID | Background service • `364 KB` ROM | Stops Horizon advertising identifier tracking and ad attribution. |
| 2 | **Meta Federated Computing** | `com.meta.federatedcomputing.oculus` | Background ML Telemetry & CPU | Background ML CPU • `8 KB` ROM | Stops background federated machine learning & telemetry model training. |
| 3 | **Android AdServices API** | `com.android.adservices.api` | Android Privacy Sandbox Ad API | Background daemon • `15.6 MiB` APEX | Stops Android Privacy Sandbox ad targeting & attribution API. |
| 4 | **Android Extended AdServices** | `com.android.ext.adservices.api` | Extended Ad Attribution Service | Background daemon • `12.7 MiB` APEX | Stops extended Android ad attribution background daemon. |
| 5 | **On-Device Ad Personalization** | `com.android.ondevicepersonalization.services` | On-Device Behavioral Ad Profiling | Background CPU/RAM • `1.8 MiB` APEX | Stops on-device behavioral ad personalization engine. |
| 6 | **Android Federated Compute** | `com.android.federatedcompute.services` | Android Federated ML Telemetry | Background ML CPU • `88 KB` APEX | Stops background Android federated telemetry training daemon. |
| 7 | **Oculus Bug Report Uploader** | `com.oculus.bugreportuploader` | Crash / Diagnostic Uploader | Background network • `964 KB` ROM | Stops background diagnostic dump & crash log uploads to Meta servers. |
| 8 | **Oculus Bug Report Service** | `com.oculus.bugreportservice` | Background Bug Capture Daemon | Background daemon • `936 KB` ROM | Stops idle diagnostic capture & bug report collection daemon. |
| 9 | **Quick Promotion / Upsells** | `com.oculus.quickpromotionservice` | VR Home Upsells & Promo Nags | Background daemon • `2.0 MiB` ROM | Eliminates in-VR Horizon+ banners, store upsells, and promo popups. |
| 10 | **Post-Update Onboarding Prompts** | `com.oculus.nux.ota` | Post-Update Promo Cards | Triggered after OTA • `356 KB` ROM | Stops post-update promotional cards & onboarding wizards. |
| 11 | **Meta Cloud Backup Transport** | `com.oculus.backuptransportservice` | Meta Cloud Backup Sync | Background sync • `24.1 MiB` ROM | Stops cloud app backup transport to Meta servers (ADB backups unaffected). |
| 12 | **Quest for Business MDM** | `com.oculus.q4b.mdm` | Quest Enterprise MDM Agent | Unused enterprise stub • `16 KB` ROM | Disables enterprise Mobile Device Management agent on consumer headsets. |
| 13 | **Wearable Location Service** | `com.facebook.wearable.system.location.service` | Wearable Location Polling & RAM | **`~110 MB` resident RSS** • `7.0 MiB` ROM | Stops resident background wearable location tracking daemon. |
| 14 | **Wearable Location Proxy** | `com.facebook.wearable.system.location.proxy` | Location Proxy Background RAM | **`~87 MB` resident RSS** • `56 KB` ROM | Stops resident background proxy for wearable location polling. |
| 15 | **Oculus Lab Service (A/B Experiments)** *(Deep Scan)* | `com.oculus.labservice` | A/B Experiments Telemetry & RAM | **`97.3 MB` RSS (`~93 MB` PSS)** • `36 KB` ROM | Stops active background A/B experiment polling & telemetry flags. |
| 16 | **Oculus Enterprise OEM Config** *(Deep Scan)* | `com.oculus.oemconfig` | Enterprise OEM Provisioning & RAM | **`117.4 MB` RSS (`~107 MB` PSS)** • `12.1 MiB` ROM | Stops enterprise MDM managed-device OEM configuration agent. |
| 17 | **Meta Pauldron VR QA Automation** *(Deep Scan)* | `com.meta.automation.pauldron.vr` | Internal Meta QA Test Automation | Unused QA stub • `12 KB` ROM | Disables internal Meta QA automated test runner agent. |
| 18 | **Oculus App Automation Service** *(Deep Scan)* | `com.oculus.appautomation` | UI / App Test Automation Harness | Unused QA stub • `524 KB` ROM | Disables automated UI/app test harness service. |
| 19 | **Android CTS Test Shim** *(Deep Scan)* | `com.android.cts.ctsshim` | Android CTS Compliance Test Stub | Empty test shim • `12 KB` APEX | Disables empty Android Compatibility Test Suite validation shim. |
| 20 | **Android Privileged CTS Test Shim** *(Deep Scan)* | `com.android.cts.priv.ctsshim` | Privileged CTS Compliance Stub | Empty test shim • `36 KB` APEX | Disables empty privileged Android CTS validation shim. |
| 21 | **Pre-Shutdown Dumpsys Service** *(Pass 3)* | `com.oculus.preshutdowntaskservice` | Pre-Reboot Dumpsys Telemetry & RAM (~98.5 MB) | **`~98.5 MB` RSS** • `84 KB` ROM | Collects diagnostic dumpsys logs right before headset shutdown/reboot via `.BootReceiver`. |

### B. Preinstalled Social Platforms (4 Apps)

| # | Friendly Name | Package ID | Affects (UAD Subsystem) | Measured RAM / APK Disk Footprint | Purpose / What Turning It Off Stops |
|---:|---|---|---|---|---|
| 1 | **Meta Horizon Worlds** | `com.facebook.horizon` | Horizon Worlds App & Feed | Heavy on launch • **`620.7 MiB` on `/data/app`** (`UPDATED_SYSTEM_APP`) | Hides preinstalled Meta Horizon Worlds client & Horizon Feed triggers. |
| 2 | **Facebook VR App** | `com.oculus.facebook` | Facebook Social VR App | Heavy on launch • **`150.7 MiB` on `/data/app`** (Removable) | Disables preinstalled Facebook social VR wrapper application. |
| 3 | **Instagram VR App** | `com.oculus.igvr` | Instagram Social VR App | Heavy on launch • **`271.8 MiB` on `/data/app`** (Removable) | Disables preinstalled Instagram VR wrapper application. |
| 4 | **Threads VR App** | `com.oculus.thvr` | Threads Social VR App | On launch • **`19.1 MiB` on `/data/app`** (Removable) | Disables preinstalled Threads social media VR wrapper application. |

### C. Preinstalled Demos, Assistants, Onboarding & Unused Android Phone/TV Stubs (25 Apps & Services)

| # | Friendly Name | Package ID | Affects (UAD Subsystem) | Measured RAM / APK Disk Footprint | Purpose / What Turning It Off Stops |
|---:|---|---|---|---|---|
| 1 | **Theater Elsewhere / Quill Player** | `com.facebook.arvr.quillplayer` | Theater Elsewhere Demo | On launch • **`25.6 MiB` on `/data/app`** (Removable) | Disables preinstalled Quill VR animation viewer & Showcase demos. |
| 2 | **First Hand Demo** | `com.oculus.samples.firsthand` | First Hand Tracking Demo | On launch • **`568.8 MiB` on `/data/app`** (Removable) | Disables preinstalled hand-tracking onboarding demonstration app. |
| 3 | **Hands Education Module** | `com.meta.handseducationmodule` | Hand Gesture Tutorial Overlay | On launch • **`174.6 MiB` on `/data/app`** (Removable) | Disables interactive tutorial overlay for Quest hand gestures. |
| 4 | **Curio Mixed-Reality Ruler** | `com.meta.curio.ruler` | MR Ruler Novelty Utility | On launch • **`70.0 MiB` on `/data/app`** (Removable) | Disables preinstalled MR ruler novelty utility. |
| 5 | **Curio Mixed-Reality Toybox** | `com.meta.curio.toybox` | MR Toybox Novelty Demo | On launch • **`310.8 MiB` on `/data/app`** (Removable) | Disables preinstalled MR toybox novelty mini-app. |
| 6 | **Meta Voice Assistant** | `com.oculus.assistant` | Voice Trigger & Mic Listener | Background wakeups • `40 KB` ROM | Stops Meta voice command listener & assistant background daemon. |
| 7 | **Quest Guidebook** | `com.oculus.guidebook` | Guidebook Tips Background Service | **`~112 MB` resident RSS** • **`39.0 MiB` on `/data/app`** (`UPDATED_SYSTEM_APP`) | Stops resident background tips & onboarding guidebook service. |
| 8 | **Meta Help Center** | `com.oculus.helpcenter` | Meta Help Viewer Service | **`~118 MB` cached RSS** • **`14.0 MiB` on `/data/app`** (Removable) | Stops preinstalled support articles & troubleshooting viewer. |
| 9 | **VR Privacy Checkup Prompts** | `com.oculus.vrprivacycheckup` | Periodic Privacy Nags | Background prompts • **`16.0 MiB` on `/data/app`** (Removable) | Stops periodic Meta privacy checkup wizard notifications. |
| 10 | **Oculus Calendar Panel** | `com.oculus.panelapp.calendar` | Legacy Home Calendar Widget | Panel service • **`11.5 MiB` on `/data/app`** (`UPDATED_SYSTEM_APP`) | Disables legacy Horizon Home calendar dock widget. |
| 11 | **Meta NUX Onboarding Daemon** *(Deep Scan)* | `com.meta.environment.prod.nuxd` | New-User Setup Daemon | Background NUX • `6.9 MiB` ROM | Stops New-User Experience (NUX) onboarding daemon after setup. |
| 12 | **Meta Surface Typing Tutorial (NUX)** *(Deep Scan)* | `com.meta.surfacetypingnux` | Direct-Touch Typing Tutorial | Onboarding prompt • `1.2 MiB` ROM | Stops direct-touch surface typing onboarding prompt. |
| 13 | **Meta Hyperscape Room Capture** *(Deep Scan)* | `com.meta.HyperscapeHmdCapture` | Cloud Room Photogrammetry | App only (**Guardian OK**) • **`184.9 MiB` on `/data/app`** (`UPDATED_SYSTEM_APP`) | Disables cloud room-photogrammetry capture app without affecting Guardian. |
| 14 | **Oculus Retail Kiosk Mode Panel** *(Deep Scan)* | `com.oculus.panelapp.kiosk` | Retail Store Demo Kiosk | Unused kiosk stub • `20 KB` ROM | Disables commercial/retail store demo kiosk mode panel. |
| 15 | **Microsoft Windows App (RDP Stub)** *(Deep Scan)* | `com.microsoft.rdc.androidx` | Microsoft Remote Desktop Client | App only • **`90.5 MiB` on `/data/app`** (`UPDATED_SYSTEM_APP`) | Disables preinstalled Microsoft Remote Desktop client stub. |
| 16 | **Android Work Profile Provisioning** *(Deep Scan)* | `com.android.managedprovisioning` | Corporate Work Profile MDM | **`~95 MB` cached PSS** • `5.6 MiB` ROM | Stops corporate BYOD work-profile provisioning wizard in RAM. |
| 17 | **Android TV Live Channels Provider** *(Deep Scan)* | `com.android.providers.tv` | Unused Android TV Tuner DB | **`~99 MB` cached PSS** • `104 KB` ROM | Stops unused Android TV broadcast tuner database waking in RAM. |
| 18 | **Android Calendar Storage Provider** *(Deep Scan)* | `com.android.providers.calendar` | Android Calendar Sync DB | **`~99 MB` cached PSS** • `244 KB` ROM | Stops background calendar storage provider waking in RAM. |
| 19 | **Android Health Connect Controller** *(Deep Scan)* | `com.android.healthconnect.controller` | Phone Health Connect UI | Unused phone health UI • `6.1 MiB` APEX | Disables Android 14 phone fitness/health data sharing controller. |
| 20 | **Android Health Connect Backup** *(Deep Scan)* | `com.android.health.connect.backuprestore` | Phone Health Connect Backup | Unused cloud sync • `20 KB` APEX | Disables background cloud backup helper for Health Connect. |
| 21 | **Android Legacy Bookmark Provider** *(Deep Scan)* | `com.android.bookmarkprovider` | Legacy AOSP Bookmark DB | Unused DB stub • `28 KB` ROM | Disables obsolete AOSP browser bookmark content provider. |
| 22 | **Android Call Blocked Number Provider** *(Deep Scan)* | `com.android.providers.blockednumber` | Phone Call-Blocking DB | Unused telephony DB • `44 KB` ROM | Disables cellular call/SMS blocking database (Quest has no cellular radio). |
| 23 | **Android Nearby Share Halfsheet** *(Deep Scan)* | `com.android.nearby.halfsheet` | Nearby Share Pop-up Stub | Unused Android phone UI • `3.2 MiB` APEX | Disables Google Nearby device pairing pop-up sheet stub. |
| 24 | **OVR Metrics Stub Service** *(Pass 3)* | `com.oculus.ovrmonitormetricsservice` | Developer Metrics Factory Stub | Stub `1.0` • `12 KB` ROM | Disables empty factory stub for the optional developer OVR Metrics Tool (`/system_ext/app/OVRMetricsTool`). |
| 25 | **Link Tabula Stub Viewer** *(Pass 3)* | `com.meta.linktabula.viewer` | Unused Internal LinkTabula Stub | Stub activity • `288 KB` ROM | Disables internal Meta viewer exposing only `.StubActivity`. |

---

### D. Medium-Risk (`MODERATE` & `CAUTION`) Deep Guide (Reddit/Forum & `dumpsys` Analysis)

These **26 packages** (`15 MODERATE` + `11 CAUTION`) are **NOT disabled by the Safe preset** because turning them off trades off real Quest 3 functionality, UI panels, or stability on Android 14 Horizon OS. Community threads across `r/OculusQuest`, `r/sidequest`, and `r/QuestPiracy` plus live `dumpsys package` / `ps` inspection on `2G0YC5ZH0L0126` clarify the exact risks and tradeoffs:

#### 1. `MODERATE` Tier (`15` packages — Included in `PCVR / Minimalist` Preset)

| Package ID | Component | Affects (UAD Subsystem) | Measured Live RAM / Disk | Forum & `dumpsys` Risk Notes |
|---|---|---|---|---|
| `com.oculus.socialplatform` | Oculus Social Platform | Parties, Voice Chat & Friends RAM | `140 MB` main RSS (up to **`547 MB` total** with `PeopleTab` / `graphql`) • `44.0 MiB` `/data/app` | `r/OculusQuest` notes disabling removes **"People you may know" / Discover People**, but breaks **Parties, Quest voice calls, invites & Friends tabs**. Individual `.PeopleShelf` component disable fails without root (`Shell cannot change component state ... to 3`). |
| `com.oculus.presence` | Live User Presence Service | Friends Online Status & App Presence | **`167.7–228.2 MB` RSS** • `55.6 MiB` APEX | Older firmware users reported **`com.oculus.presence keeps stopping`** toast loops when disabled alongside active social hooks; keep enabled unless disabling all Meta social services together. |
| `com.meta.rl.trust.service` | Reality Labs Consent & Privacy UI | On-Boot ToS / Consent Prompts | **`120.1–138.3 MB` RSS** • `6.7 MiB` ROM | Handles regional privacy/ToS prompts after initial setup/OTA updates. Safe for PCVR once setup is complete; re-enable temporarily if an OS update requests ToS acceptance. |
| `com.oculus.avatareditor` | Meta Avatar Editor | 3D Meta Avatar Customization | Heavy on launch | Breaks editing 3D Meta Avatars; games needing avatar SDK read synced avatar assets via `horizon`. |
| `com.oculus.tv` | Meta Quest TV | Quest TV 180/360 Streaming App | **`~146 MB` RSS** when open | Removes built-in 180/360 VR video catalog. Zero impact on 3rd-party players (SkyBox, Bigscreen, DeoVR). |
| `com.oculus.remotedesktop` | Meta Remote Desktop | Workrooms Remote Desktop Panel | **`~144.7–186.5 MB` RSS** | Built-in desktop viewer for Meta Workrooms. Safe to disable when using Virtual Desktop, Steam Link, or Quest Link. |
| `com.oculus.mrds` | Meta Remote Desktop Service (MRDS) | Built-in Meta Remote Desktop Companion | `4.5 MiB` `/data/app` | Companion daemon for `com.oculus.remotedesktop` / Workrooms; unneeded for Virtual Desktop / Steam Link users. |
| `com.oculus.horizonmediaplayer` | Meta Horizon Media Player | Built-in Local 2D/VR Video Player | **`~147.4 MB` RSS** (`~125 MB` PSS) | Disabling disables the stock file viewer inside Quest Home, but third-party media apps continue working normally. |
| `com.oculus.captionservice` | Horizon OS Live Captions | System Speech-to-Text Captions | `7.0 MiB` ROM | Disables system accessibility closed-captions overlays; completely safe if you do not use Live Captions. |
| `com.oculus.accountscenter` | Meta Accounts Center App | Meta / IG / FB Account Linking | UI Settings only | Hides cross-app Meta/Facebook/Instagram account linking UI; keeps core device login intact via `companion.server` & `identitymanagement`. |
| `com.meta.AccountsCenter.pwa` | Meta Accounts Center PWA | Accounts Center Web Wrapper | Companion PWA | Web wrapper paired with `accountscenter`. |
| `com.meta.shell.env.vista.calming` | Home Env: Calming | Extra Vista Skybox Theme | Static theme asset | Safe to disable when keeping default `haven2025` home environment. |
| `com.meta.shell.env.vista.focused` | Home Env: Focused | Extra Vista Skybox Theme | Static theme asset | Safe to disable when keeping default `haven2025` home environment. |
| `com.meta.shell.env.vista.horror` | Home Env: Horror | Extra Vista Skybox Theme | Static theme asset | Safe to disable when keeping default `haven2025` home environment. |
| `com.meta.shell.env.vista.oceanarium` | Home Env: Oceanarium | Extra Vista Skybox Theme | Static theme asset | Safe to disable when keeping default `haven2025` home environment. |

#### 2. `CAUTION` Tier (`11` packages — Manual Toggles Only)

| Package ID | Component | Affects (UAD Subsystem) | Measured Live RAM / Disk | Forum & `dumpsys` Risk Notes |
|---|---|---|---|---|
| `com.oculus.store` | Meta Horizon Store | Official Store UI & In-VR Purchases | **`~167–274 MB` resident RSS** | Largest single resident UI app, but disabling prevents browsing the Store, updating official Meta apps, or launching Store deep links. |
| `com.oculus.systemintelligence` | System Intelligence & TTS | Voice Dictation, TTS & `DeviceConfig` | **`~208 MB` RSS** • `57.3 MiB` `/data/app` | Hosts TTS (`InstallTtsDataActivity`/`CheckTtsDataActivity`) and `DeviceConfigClientReceiver`. Marked `UPDATED_SYSTEM_APP` (`v211+`) and noted in recent Quest 3 threads (`r/OculusQuest` `v2.3+`) — breaks OS dictation/TTS and can re-update over OTAs. |
| `com.oculus.metacam` | Horizon Camera & Recording UI | Screenshots, Video Capture & Live Sharing | **`~139.4–166.3 MB` RSS** • `17.5 MiB` `/data/app` | Hosts `CapturePreviewPopupActivity`, `SharingPanelActivity`, and livestream overlays. Disabling breaks built-in VR screenshots and video recording. |
| `com.oculus.appsafety` | App Safety Integrity Monitor | Sideloaded App / Entitlement Safety | **`~117.8–121.9 MB` RSS** • `3.2 MiB` ROM | Scanned by legacy Oculess setups (`r/QuestPiracy`); monitors unsigned/sideloaded APKs for Meta policy warnings. Disabling removes background checks but leaves OS-level installer rules untouched. |
| `com.oculus.websafety` | Web Safety Filter | Quest Browser Malicious URL Filter | `2.2 MiB` ROM | Safe-browsing URL checker invoked by `com.oculus.browser`. |
| `com.oculus.hzosgallery` | Horizon OS Media Gallery | VR Screenshots & Spatial Gallery UI | **`~115.8–157.4 MB` RSS** | Disabling removes the built-in VR Files/Media gallery viewer (photos/videos stay readable over USB/MTP). |
| `com.oculus.notification_proxy` | Notification Proxy *(Oculess Legacy)* | Phone Notification Mirror & Alerts | **`PERSISTENT`** (`584 KB` ROM) | Legacy `Oculess` scripts suggested disabling it (`r/QuestPiracy`), but on Android 14 Horizon OS (`v209+`) it carries `PERSISTENT` system flags — disabling can fail with `SecurityException` or silence system notifications. |
| `com.oculus.magicislandcastingservice` | Meta Native Casting Service | Built-in Browser/Phone Casting | Casting daemon | Breaks native Meta cast receiver to phones/browser (`oculus.com/casting`); ADB/scrcpy casting (e.g., VRmoo) is unaffected. |
| `com.oculus.xrstreamingclient` | Meta Quest Link / Air Link | Wired USB Link & Wireless Air Link | Streaming client | Disables official Meta Quest Link PCVR streaming. Only turn off if you use **Virtual Desktop**, **Steam Link**, or **ALVR** exclusively. |
| `com.meta.pclinkservice.server` | Meta PC Link Server | PC Link USB/Wi-Fi Discovery | Companion server | Required alongside `xrstreamingclient` for automatic Meta Quest Link discovery. |
| `com.oculus.browser` | Meta Quest Browser | Built-in WebXR Chromium Browser | Browser app | Disabling removes default WebXR / OAuth webview fallback if 3rd-party apps open an external browser tab. |

> **Why Legacy Reddit Lists Differ on v76+ / Android 14:** Older `r/sidequest` / `r/QuestPiracy` guides sometimes targeted `com.oculus.ocms` or `com.oculus.updater`. On modern Horizon OS (`v76+` / Android 14), **`ocms` breaks the app Library and DRM checks** (and Meta re-enables Android Settings toggles across reboots), while **`com.oculus.updater` throws `SecurityException`** from non-root ADB — use `pm disable-user --user 0` only on vetted packages and block `updater` / telemetry at the DNS level (`configs/adguard-meta-telemetry.txt`) instead. Core tracking/auth daemons (`com.oculus.horizon`, `com.facebook.spatial_persistence_service`, `com.oculus.linefrequencyservice`, `com.oculus.bodyapiservice`, `com.oculus.connectivity.bluetooth`, `com.oculus.vralertservice`, `com.oculus.integrity`) are **hard-locked in Tier 4 (`PROTECTED`)**.

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
│   ├── packages-catalog.json             # 100-package tiered Horizon OS catalog
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
  - **Safe Recommended (50)** *(already applied to your headset)* — Telemetry, ads, promo upsells, QA/enterprise agents, social wrappers, demo bloat, and unused phone/TV stubs.
  - **Telemetry & Ads (21)** — Strictly invisible background tracking, experimentation, pre-shutdown dumpsys, QA automation, & promotional services only.
  - **PCVR / Minimalist (65)** — Adds Social Platform (`com.oculus.socialplatform`), Presence, Avatar Editor, Quest TV, Remote Desktop + MRDS, Horizon Media Player (`com.oculus.horizonmediaplayer`), Live Captions, Reality Labs Consent (`rl.trust.service`), Accounts Center, and 4 extra Vista home themes (keeps Haven 2025, Store, Browser, and Virtual Desktop/OpenXR untouched).
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
