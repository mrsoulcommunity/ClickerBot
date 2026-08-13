# ClickerBot

A Windows desktop automation tool that repeats a key press, a mouse click, or both, at configurable intervals. Built with .NET 8 and Windows Forms — every control is custom-drawn, so the interface looks the same whether you're on light or dark, and a live rhythm strip shows a run's actual pacing instead of just a spinning counter.

--- 

## Table of contents

- [Features](#features)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Usage](#usage)
- [Appearance](#appearance)
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
| **Three action modes** | Key + click, key only, or click only — the fields that don't apply to the chosen mode disable themselves rather than disappear, so the layout never jumps. |
| **Any key supported** | Letters, digits, function keys, `Tab`, `Enter`, `Space`, punctuation, arrows, and numpad keys. Press `Esc` in a key field to clear it. |
| **Left, right or middle click, single or double** | The mouse button and whether each iteration double-clicks are both configurable. |
| **Fixed point or the live cursor** | Click a captured screen coordinate, or click wherever the cursor already is so you can steer a run by hand. |
| **Click scatter** | A random pixel radius applied around the click target, so every click doesn't land on the exact same coordinate. |
| **Fixed or randomized delays** | Each delay is either a fixed millisecond value or a random value re-drawn from a `min–max` range on every iteration. |
| **Three ways to stop** | An iteration count, a time limit, or run until you stop it by hand. |
| **Start delay** | An optional countdown before the first action, so you have time to click into the target window. |
| **Pause and resume** | Hold a run in place without losing its iteration count or elapsed time, then continue exactly where it left off. |
| **Live cadence strip** | A running strip of ticks, one per completed iteration, laid out on a real time axis — an even comb for a fixed delay, a ragged one for a random range. It's the fastest way to tell a run is behaving the way you configured it. |
| **Position capture** | Click **Pick** — or press its hotkey from anywhere — to store the current cursor position as the click target. |
| **Four global hotkeys** | Start, Pause, Stop, and Pick point are all registered system-wide and work while other applications have focus. All four are configurable per profile and must be distinct from each other and from the automated key. |
| **Profiles** | Create, rename, duplicate, delete, import, and export named configurations. Every setting is part of the profile. |
| **Import / export** | Share a profile file between machines, or keep a backup outside `%APPDATA%`, without touching the rest of your saved profiles. |
| **Keep above other windows** | Optionally pins ClickerBot on top so the run panel stays visible over the window being automated. |
| **Hide to the notification area while running** | Drops the window out of the way for the duration of a run and restores it automatically when the run ends. The tray icon itself shows whether a run is active. |
| **Auto-save** | Changes are persisted to disk automatically a moment after you make them — nothing to remember to save. |
| **Light & dark themes** | One click on the header switch, with an animated transition. The title bar follows too. See [Appearance](#appearance). |
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
git clone https://github.com/mrsoulcommunity/ClickerBot.git
```

### The manual way

```bash
dotnet build ClickerBot/ClickerBot.csproj -c Release
```

```bash
dotnet run --project ClickerBot/ClickerBot.csproj -c Release
```

---

## Usage

1. **Choose a mode** in the *Action* card: **Key + click**, **Key only**, or **Click only**. The fields that don't apply grey out.
2. **Pick a key** (if the mode uses one). Click the **Key** field, then press the key you want automated.
3. **Set up the click** (if the mode uses one): pick the button, whether it double-clicks, and whether it clicks **a fixed point** or **the cursor**'s live position. For a fixed point, move your cursor to the target and press the **Pick** hotkey (or click **Pick**), or type the X/Y coordinates directly. **Scatter** adds a random pixel radius around that point so clicks don't land on the exact same spot every time.
4. **Tune the timing** in the *Timing* card:
   - **After the key press, before the click** — only used by Key + click.
   - **Between one iteration and the next.**
   - Tick **Random** on either one to draw a fresh value from a `min–max` range each iteration.
   - **Start delay** — an optional countdown before the first action, to give you time to click into the target window.
5. **Choose how the run ends** in the *Repeat* card: a **Count** of iterations, a **Duration**, or **Until stopped**.
6. **Press Start** — or your Start hotkey. The settings panel locks while a run is in progress; the cadence strip and readouts in the run bar update live.
7. **Pause and resume** with the run bar's Pause button or your Pause hotkey — the iteration count and elapsed time hold in place and continue from there.
8. **Press Stop** — or your Stop hotkey — to cancel at any time.

The switch in the top-right corner flips between light and dark at any time; see [Appearance](#appearance).

> **Note**
> The automated key cannot be one of the four hotkeys. Synthesized key presses trigger registered hotkeys just like real ones, so the run would stop itself, restart itself, pause itself, or quietly move the click target out from under itself. The app blocks these combinations and tells you which one you hit.

---

## Appearance

The switch in the top-right corner of the header toggles between the light and dark themes. The change is immediate and animated, and it covers the whole window — including the title bar, which is repainted through the Desktop Window Manager rather than left as a light strip above a dark app.

- **First run** starts on whatever appearance Windows itself is set to, read from `AppsUseLightTheme`.
- **Your choice is remembered** in the same file as your profiles, as an application-wide setting. Switching profiles never changes the theme.
- **Nothing else changes.** Themes are purely visual; every automation setting is untouched.

### How theming is built

`Theme` exposes the active `Palette` and raises `Changed` when it is swapped. Controls read colors inside `OnPaint` rather than caching them at construction, so a switch is mostly just a repaint. Stock WinForms controls do cache their colors, so those are wrapped in themed variants that implement `IThemedControl`, and `ThemeManager` walks the control tree calling `ApplyTheme()` on each one — with painting suspended so the change lands in a single frame.

Three controls are drawn from scratch instead of using the framework versions, because Windows paints those itself and they stay light no matter what colors are assigned: the checkbox glyph, the numeric field's spin buttons, and the confirmation dialog. Adding a third appearance would mean adding one `Palette` instance and changing nothing else.

---

## Hotkeys

| Hotkey | Action | Configurable |
| --- | --- | --- |
| `F7` | Start the current profile | Yes — per profile |
| `F8` | Pause or resume the current run | Yes — per profile |
| `F9` | Stop the current run | Yes — per profile |
| `F10` | Capture the cursor position as the click target | Yes — per profile |

Hotkeys are registered globally through the Win32 `RegisterHotKey` API, so they fire even when ClickerBot is in the background — including while it's hidden to the notification area. If another application already owns a key, the status bar tells you which binding could not be registered — pick a different one.

All four hotkeys must be distinct from each other, and from the automated key when the current mode uses one.

---

## Profiles and data storage

Profiles hold every configurable value: the mode, key, mouse button, click target and scatter, both delay settings, the start delay, the repeat mode, and all four hotkeys. Switching profiles re-registers that profile's hotkeys immediately. The chosen theme and the two window options (always-on-top, hide-to-tray) are stored alongside them but apply to the whole application rather than to a single profile.

Everything is stored as indented JSON at:

```
%APPDATA%\ClickerBot\profiles.json
```

The file is written automatically shortly after any change and again on exit. Each save is written alongside the real file and swapped in, so an interrupted write cannot leave a truncated file where your profiles were. If the file is ever missing or corrupt, the app falls back to a single fresh **Default** profile rather than failing to start; values outside the ranges the inputs allow are pulled back into range on load, so hand-editing it cannot put the app into a state it will not run.

**Import** reads profiles from another JSON file — one you exported earlier, or another machine's `profiles.json` — and adds them to your existing list under unique names, so importing never overwrites what's already there. **Export** writes your current profiles to a file you choose, without the application-wide settings, so it's portable between machines.

The app was previously called ClickerApp. If nothing is found at the path above, profiles are read once from the old `%APPDATA%\ClickerApp\profiles.json` and saved forward to the new location, so an existing setup carries over on its own. The old folder is left in place and can be deleted whenever you like.

---

## Project structure

```
.
├── ClickerBot/
│   ├── Automation/
│   │   ├── AutomationRunner.cs   # The async action loop: mode-aware, pausable, time-or-count bounded
│   │   └── RunProgress.cs        # Run phase snapshot + the pause gate the UI holds
│   ├── Input/
│   │   ├── HotkeyManager.cs      # Global hotkey registration (RegisterHotKey)
│   │   ├── KeyNames.cs           # Human-readable key labels
│   │   └── NativeInput.cs        # SendInput wrapper for synthetic key/mouse input
│   ├── Models/
│   │   ├── ActionMode.cs         # Mode / button / repeat-mode / click-target enums
│   │   ├── DelaySetting.cs       # Fixed or random-range delay
│   │   ├── Limits.cs             # The valid range of every numeric setting
│   │   ├── Profile.cs            # One named configuration
│   │   └── ProfileStore.cs       # JSON load/save, plus import/export
│   ├── Ui/
│   │   ├── Controls/
│   │   │   ├── CadenceMeter.cs     # Live per-iteration rhythm strip
│   │   │   ├── Card.cs             # Titled section container
│   │   │   ├── ConfirmDialog.cs    # Themed replacement for MessageBox
│   │   │   ├── DelayEditor.cs      # Fixed/random delay control
│   │   │   ├── FlatButton.cs       # Owner-drawn button (primary/secondary/danger)
│   │   │   ├── KeyCaptureBox.cs    # Field that records the next key pressed
│   │   │   ├── NumberBox.cs        # Owner-drawn numeric field with steppers
│   │   │   ├── ProfileListBox.cs   # Owner-drawn profile list
│   │   │   ├── RunPanel.cs         # Run bar: status, readouts, cadence strip, transport
│   │   │   ├── Segmented.cs        # Owner-drawn segmented choice control
│   │   │   ├── SurfacePanel.cs     # Panel that tracks the surface color
│   │   │   ├── ThemeToggle.cs      # Animated light/dark switch
│   │   │   ├── ThemedCheckBox.cs   # Owner-drawn checkbox
│   │   │   ├── ThemedLabel.cs      # Label that stores a role, not a color
│   │   │   └── ThemedTextBox.cs    # Palette-aware text box
│   │   ├── Theming/
│   │   │   ├── Palette.cs          # One complete set of colors
│   │   │   ├── Theme.cs            # Active palette, fonts, drawing helpers
│   │   │   ├── ThemeManager.cs     # Pushes the theme through the control tree
│   │   │   ├── ThemeMode.cs        # Light / Dark
│   │   │   └── WindowChrome.cs     # Dark title bar + system preference
│   │   ├── AppIcon.cs             # Drawn window/tray icon, amber when a run is active
│   │   ├── MainForm.cs            # Wiring: profiles, settings, hotkeys, run control, tray
│   │   ├── MainForm.Layout.cs     # Where every control is placed
│   │   └── UiFactory.cs           # Small control factory helpers
│   ├── app.manifest              # DPI awareness, execution level, OS support
│   ├── ClickerBot.csproj
│   └── Program.cs                # Entry point
├── .gitattributes                # Line-ending rules (CRLF for .bat)
├── .gitignore
├── LICENSE                       # MIT
├── README.md
└── Run.bat                       # One-click build-and-launch script
```

---

## How it works

`AutomationRunner.RunAsync` drives the loop while the UI thread stays responsive:

1. Wait out the start delay, if any, reporting a countdown each second.
2. If a key is part of the mode, synthesize a key-down/key-up pair via `SendInput`; if the mode is **Key + click**, wait the *after key press* delay.
3. If a click is part of the mode, move the cursor (unless the target is the live cursor position) and synthesize the configured button — twice, for a double-click. A **scatter** radius, if set, offsets the point by a random amount inside that radius on every click, sampled evenly across the disc rather than bunched toward the center.
4. Report progress to the UI and mark the iteration on the cadence strip.
5. Check the stop condition — iteration count or elapsed duration — and stop if it's met, without waiting out one more delay first.
6. Wait the *between iterations* delay, then repeat.

Each wait calls `DelaySetting.Next()`, so a randomized delay produces a different value on every pass. Cancellation is cooperative through a `CancellationTokenSource`, checked before each synthesized input and honored by every `Task.Delay`, so **Stop** takes effect within one delay interval at most.

**Pause** goes through a separate `PauseGate` rather than cancellation: the loop parks on it between iterations, and the elapsed-time clock stops with it, so a duration-limited run doesn't burn its budget while paused. Resuming picks the loop back up with its iteration count and remaining time exactly where they were.

Extended keys (arrows, `Insert`, `Delete`, `Home`, `End`, `Page Up`/`Page Down`, numpad `/`, right `Ctrl`/`Alt`, and others) are flagged with `KEYEVENTF_EXTENDEDKEY` so target applications receive them correctly.

---

## Building a standalone release

To produce a single self-contained executable that runs without the .NET runtime installed:

```bash
dotnet publish ClickerBot/ClickerBot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The result lands in `ClickerBot/bin/Release/net8.0-windows/win-x64/publish/`.

For a much smaller build that requires the .NET 8 Desktop Runtime on the target machine:

```bash
dotnet publish ClickerBot/ClickerBot.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

---

## Troubleshooting

**A hotkey does nothing, and the status bar mentions another app.**
Windows grants a global hotkey to the first application that asks for it. Another running program owns that key — choose a different one in the *Hotkeys* card.

**"Could not move the cursor … " or "SendInput failed … Input may be blocked by an elevated window."**
Windows blocks synthetic input from a normal-rights process to a process running as administrator. Either run the target application without elevation, or run ClickerBot as administrator. To make elevation permanent, change `asInvoker` to `requireAdministrator` in [`ClickerBot/app.manifest`](ClickerBot/app.manifest) and rebuild.

**The click lands in the wrong place.**
Coordinates are absolute screen pixels across the whole virtual desktop. Re-capture the position with the Pick hotkey if you changed your display scaling, resolution, or monitor arrangement — or set **Click at** to **The cursor** if you'd rather aim it by hand each time.

**`Run.bat` reports that the .NET SDK was not found.**
Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then run the file again.

**My settings disappeared.**
Check that `%APPDATA%\ClickerBot\profiles.json` exists and is readable. A corrupt file is ignored on startup and replaced with a fresh default profile.

---

## Responsible use

This tool synthesizes real keyboard and mouse input at the operating-system level. Use it for legitimate automation — repetitive data entry, testing your own software, accessibility assistance, and similar tasks. Many online games and web services prohibit automated input in their terms of service, and using this tool against them may get your account suspended. You are responsible for how you use it.

---

## License

Released under the [MIT License](LICENSE). You are free to use, modify, and distribute this software, including commercially, provided the copyright notice and license text are retained. The software is provided as-is, without warranty of any kind.
