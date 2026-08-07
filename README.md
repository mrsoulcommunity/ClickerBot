# Auto Key & Click

A lightweight Windows desktop automation tool that repeats a **key press → mouse click** sequence at configurable intervals. Built with .NET 8 and Windows Forms, with a clean flat UI, saved profiles, and global hotkeys so you can start and stop it without leaving the window you're working in.

--- 

## Table of contents

- [Features](#features)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Usage](#usage)
- [Hotkeys](#hotkeys)
- [Profiles and data storage](#profiles-and-data-storage)
- [Project structure](#project-structure)
- [How it works](#how-it-works)
- [Building a standalone release](#building-a-standalone-release)
- [Troubleshooting](#troubleshooting)
- [Responsible use](#responsible-use)
- [License](#license)

---

## Features

| Feature | Description |
| --- | --- |
| **Key + click automation** | Presses a key, waits, left-clicks a fixed screen coordinate, waits, repeats. |
| **Any key supported** | Letters, digits, function keys, `Tab`, `Enter`, `Space`, punctuation, arrows, and numpad keys. Press `Esc` in a key field to clear it. |
| **Fixed or randomized delays** | Each delay is either a fixed millisecond value or a random value re-drawn from a `min–max` range on every iteration. |
| **Finite or infinite runs** | Set an exact repetition count, or tick **Infinite** to run until you stop it. |
| **Position capture** | Click **Capture** — or press `F9` from anywhere — to store the current cursor position as the click target. |
| **Global hotkeys** | Start and stop hotkeys are registered system-wide and work while other applications have focus. Both are configurable per profile. |
| **Profiles** | Create, rename, duplicate, and delete named configurations. Every setting is part of the profile. |
| **Auto-save** | Changes are persisted to disk automatically a moment after you make them — nothing to remember to save. |
| **Live status** | The footer shows the current iteration, completion, errors, and hotkey conflicts. |
| **High-DPI aware** | Per-monitor V2 DPI awareness, so the UI stays sharp on scaled and mixed-DPI displays. |

---

## Requirements

- **Windows 10 or Windows 11**
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** — required to build. Only the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) is needed to run an already-built binary.

The app runs with normal user rights (`asInvoker`). See [Troubleshooting](#troubleshooting) if you need to drive an elevated window.

---

## Getting started

### The easy way

1. Clone or download this repository.
2. Double-click **`Run.bat`** in the repository root.

That's it. On the first run `Run.bat` builds the project in Release mode (once), then launches the app. Every run after that starts the app immediately — no terminal required.

```bash
git clone https://github.com/mrsoulcommunity/ClickerApp.git
```

### The manual way

```bash
dotnet build ClickerApp/ClickerApp.csproj -c Release
```

```bash
dotnet run --project ClickerApp/ClickerApp.csproj -c Release
```

---

## Usage

1. **Pick a key.** Click the **Key** field in the *Action* card, then press the key you want automated.
2. **Set the click position.** Move your cursor to the target and press `F9` (or click **Capture**). You can also type the X and Y screen coordinates directly.
3. **Tune the delays** in the *Delays* card:
   - **After key press** — the pause between the key press and the mouse click.
   - **Between clicks** — the pause after the click, before the next iteration's key press.
   - Tick **Random** on either one to draw a fresh value from a `min–max` range each iteration.
4. **Choose how long to run** in the *Repeat & Hotkeys* card: a repetition count, or **Infinite**.
5. **Press Start** — or your start hotkey. The settings panel locks while a run is in progress.
6. **Press Stop** — or your stop hotkey — to cancel at any time.

> **Note**
> The automated key cannot be the same as your **Stop** hotkey. Synthesized key presses trigger registered hotkeys too, so the run would immediately stop itself. The app blocks this combination and tells you why.

---

## Hotkeys

| Hotkey | Action | Configurable |
| --- | --- | --- |
| `F7` | Start the current profile | Yes — per profile |
| `F8` | Stop the current run | Yes — per profile |
| `F9` | Capture the cursor position as the click target | No |

Hotkeys are registered globally through the Win32 `RegisterHotKey` API, so they fire even when Auto Key & Click is in the background. If another application already owns a key, the status bar tells you which binding could not be registered — pick a different one.

Start and Stop must be different keys.

---

## Profiles and data storage

Profiles hold every configurable value: the key, click coordinates, both delay settings, the repetition mode, and the start/stop hotkeys. Switching profiles re-registers that profile's hotkeys immediately.

Everything is stored as indented JSON at:

```
%APPDATA%\ClickerApp\profiles.json
```

The file is written automatically shortly after any change and again on exit. If it is ever missing or corrupt, the app falls back to a single fresh **Default** profile rather than failing to start.

---

## Project structure

```
.
├── ClickerApp/
│   ├── Automation/
│   │   └── AutomationRunner.cs   # The async press → wait → click → wait loop
│   ├── Input/
│   │   ├── HotkeyManager.cs      # Global hotkey registration (RegisterHotKey)
│   │   ├── KeyNames.cs           # Human-readable key labels
│   │   └── NativeInput.cs        # SendInput wrapper for synthetic key/mouse input
│   ├── Models/
│   │   ├── DelaySetting.cs       # Fixed or random-range delay
│   │   ├── Profile.cs            # One named configuration
│   │   └── ProfileStore.cs       # JSON load/save of the profile collection
│   ├── Ui/
│   │   ├── Card.cs               # Titled section container
│   │   ├── DelayEditor.cs        # Fixed/random delay control
│   │   ├── FlatButton.cs         # Owner-drawn button (primary/secondary/danger)
│   │   ├── KeyCaptureBox.cs      # Field that records the next key pressed
│   │   ├── MainForm.cs           # Main window, wiring, and run control
│   │   ├── ProfileListBox.cs     # Owner-drawn profile list
│   │   ├── Theme.cs              # Colors and fonts
│   │   └── UiFactory.cs          # Small control factory helpers
│   ├── app.manifest              # DPI awareness, execution level, OS support
│   ├── ClickerApp.csproj
│   └── Program.cs                # Entry point
├── .gitattributes                # Line-ending rules (CRLF for .bat)
├── .gitignore
├── README.md
└── Run.bat                       # One-click build-and-launch script
```

---

## How it works

`AutomationRunner.RunAsync` drives the loop on the thread pool while the UI thread stays responsive:

1. Synthesize a key-down/key-up pair for the configured key via `SendInput`.
2. Wait for the *after key press* delay.
3. Move the cursor to the target coordinate and synthesize a left click.
4. Report progress to the UI.
5. Wait for the *between clicks* delay, then repeat.

Each wait calls `DelaySetting.Next()`, so a randomized delay produces a different value on every pass. Cancellation is cooperative through a `CancellationTokenSource`, checked before each synthesized input and honored by every `Task.Delay`, so **Stop** takes effect within one delay interval at most.

Extended keys (arrows, `Insert`, `Delete`, `Home`, `End`, `Page Up`/`Page Down`, numpad `/`, right `Ctrl`/`Alt`, and others) are flagged with `KEYEVENTF_EXTENDEDKEY` so target applications receive them correctly.

---

## Building a standalone release

To produce a single self-contained executable that runs without the .NET runtime installed:

```bash
dotnet publish ClickerApp/ClickerApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The result lands in `ClickerApp/bin/Release/net8.0-windows/win-x64/publish/`.

For a much smaller build that requires the .NET 8 Desktop Runtime on the target machine:

```bash
dotnet publish ClickerApp/ClickerApp.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## Troubleshooting

**A hotkey does nothing, and the status bar mentions another app.**
Windows grants a global hotkey to the first application that asks for it. Another running program owns that key — choose a different one in the *Repeat & Hotkeys* card.

**"SendInput failed … Input may be blocked by an elevated window."**
Windows blocks synthetic input from a normal-rights process to a process running as administrator. Either run the target application without elevation, or run Auto Key & Click as administrator. To make elevation permanent, change `asInvoker` to `requireAdministrator` in [`ClickerApp/app.manifest`](ClickerApp/app.manifest) and rebuild.

**The click lands in the wrong place.**
Coordinates are absolute screen pixels across the whole virtual desktop. Re-capture the position with `F9` if you changed your display scaling, resolution, or monitor arrangement.

**`Run.bat` reports that the .NET SDK was not found.**
Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then run the file again.

**My settings disappeared.**
Check that `%APPDATA%\ClickerApp\profiles.json` exists and is readable. A corrupt file is ignored on startup and replaced with a fresh default profile.

---

## Responsible use

This tool synthesizes real keyboard and mouse input at the operating-system level. Use it for legitimate automation — repetitive data entry, testing your own software, accessibility assistance, and similar tasks. Many online games and web services prohibit automated input in their terms of service, and using this tool against them may get your account suspended. You are responsible for how you use it.

---

## License

No license has been specified yet. Until one is added, all rights are reserved and the code is provided as-is, without warranty of any kind.
