# 🚀 Workspace Launcher (Multiple App Opener)

A sleek, native Windows productivity desktop utility designed to organize and launch entire multi-application workspaces with a single click. Configure custom profiles for coding, gaming, content creation, or client meetings.

### 🌐 Web Showcase: [https://milospavlovic7.github.io/multiple-app-opener/](https://milospavlovic7.github.io/multiple-app-opener/)

---

## ✨ Key Features

- **Multi-App Orchestration**: Launch combinations of native `.exe` executables, browser URLs, files, shell scripts, and directories simultaneously.
- **Session Capture**: One-click detection of currently running desktop windows and applications to quickly save your active workspace as a preset profile.
- **Staggered Delays & Arguments**: Configure sequential launch order and startup delay timers (in milliseconds) to prevent system freezing or race conditions.
- **Custom Branding**: Assign custom hex accent colors, descriptions, and icon identifiers to each profile.
- **Pin & Usage Metrics**: Pin your favorite daily workspaces to the top of your list and track total launch frequency and last-opened timestamps.
- **Clean WPF Aesthetic**: Built with modern Windows Presentation Foundation (WPF) UI standards and responsive theme palettes.

---

## 🎮 How to Use

1. **Create a Workflow**: Click the `+` button to create a new profile (e.g. "Web Dev", "Audio Studio", "Streaming").
2. **Add Actions**:
   - **Applications**: Select any installed program or executable path.
   - **URLs**: Add web links to open in your default browser.
   - **Arguments & Delay**: Set command-line flags and optional delay timers.
3. **Launch**: Click **Run** on any card to fire all targets in sequence.

---

## 🚀 Quick Start

### Option 1: Download Pre-compiled Binary
Grab the latest standalone Windows build from the Releases tab:  
👉 **[Releases on GitHub](https://github.com/milospavlovic7/multiple-app-opener/releases)**

### Option 2: Build from Source (.NET 8/9)
```bash
# Clone the repository
git clone https://github.com/milospavlovic7/multiple-app-opener.git
cd multiple-app-opener

# Restore & build solution
dotnet build WorkspaceLauncher.slnx
```
Or open `WorkspaceLauncher.slnx` directly in Visual Studio 2022+ and press **F5**.

---

## 🛠️ Tech Stack

- **Platform**: Windows Desktop (.NET / WPF)
- **Language**: C#
- **Architecture**: MVVM (Model-View-ViewModel) with XAML data binding
- **Testing**: xUnit (`WorkspaceLauncher.Tests`)

---

## 📜 License

MIT License. Free for personal and commercial use.
