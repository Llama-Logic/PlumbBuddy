<h1>PlumbBuddy</h1>
The friendly helper for Sims 4 mods—crafted with care, open for all.

<!-- TOC -->

- [Building](#building)
  - [Windows](#windows)
    - [Prerequisites](#prerequisites)
    - [Clone the Repository](#clone-the-repository)
    - [Build Instructions](#build-instructions)
    - [Running the App](#running-the-app)
    - [Common Issues and Fixes](#common-issues-and-fixes)
      - [Missing Dependencies](#missing-dependencies)
      - [Build Errors](#build-errors)
      - [Missing Assets or Incorrect Layout](#missing-assets-or-incorrect-layout)
  - [macOS](#macos)
    - [Prerequisites](#prerequisites-1)
    - [Setup Steps](#setup-steps)
    - [Common Issues and Fixes](#common-issues-and-fixes-1)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)
  - [Frameworks](#frameworks)
  - [Libraries](#libraries)

<!-- /TOC -->

![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Llama-Logic/PlumbBuddy/total)
![Version Badge](https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FLlama-Logic%2FPlumbBuddy%2Frefs%2Fheads%2Fmain%2FPlumbBuddy%2FPlumbBuddy.csproj&query=%2FProject%2FPropertyGroup%2FApplicationVersion%2Ftext()&label=version)
![GitHub Tag](https://img.shields.io/github/v/tag/Llama-Logic/PlumbBuddy?label=latest%20tag)
![Creator Themes Badge](https://img.shields.io/badge/creator%20themes-4-blue)
![GitHub language count](https://img.shields.io/github/languages/count/Llama-Logic/PlumbBuddy)
![GitHub top language](https://img.shields.io/github/languages/top/Llama-Logic/PlumbBuddy)
![GitHub License](https://img.shields.io/github/license/Llama-Logic/LlamaLogic)

# Building

## Windows

### Prerequisites
Before building the app, make sure your system meets the following requirements:
1. Operating System: Windows 10 version 1903 or higher.
2. Development Tools:
   * [Visual Studio](https://visualstudio.microsoft.com/) (latest version recommended).
   * Workload: `.NET Multi-platform App UI Development (MAUI)`.
   * Optional but helpful: ASP.NET and web development workload for Blazor support.
3. Dependencies:
   * Install the .NET 8 (or later) SDK.
   * Add the `Windows App SDK` extension during Visual Studio installation.
4. Git: [Install Git](https://git-scm.com/) for version control if not already installed.

### Clone the Repository
1. Open a terminal (Command Prompt, PowerShell, or Git Bash).
2. Run the following command to clone the repository:
    ```batch
    git clone https://github.com/Llama-Logic/PlumbBuddy.git
    cd PlumbBuddy
    ```

### Build Instructions
1. Open the **PlumbBuddy.sln** file in Visual Studio.
2. Set the Startup Project:
   * Right-click the project named `PlumbBuddy` and select **Set as Startup Project**.
3. Select the **Windows Machine** target:
   * Use the dropdown menu in the Visual Studio toolbar to choose the `Windows` platform.
4. Restore dependencies:
   * Open the **Package Manager Console** (or terminal) and run:
        ```batch
        dotnet restore
        ```
5. Build the project:
   * Press `Ctrl + Shift + B` or select **Build** > **Build Solution** from the menu.

### Running the App
1. To run the app:
   * Press `F5` to start the app in Debug mode.
   * Or press Ctrl + F5 to run the app without debugging.
2. The app should launch in a Windows desktop window.

### Common Issues and Fixes

#### Missing Dependencies
* Ensure all required workloads (MAUI, ASP.NET) and SDKs are installed.

#### Build Errors
* Double-check that you've restored NuGet packages using `dotnet restore`.
* If you encounter issues with Windows App SDK, try repairing the Visual Studio installation and ensuring the **Windows App SDK** is up to date.

#### Missing Assets or Incorrect Layout
* Confirm that the **Resources** folder contains the app icon and splash screen in the correct formats

## macOS

### Prerequisites
Ensure your system meets the following requirements:
1. Operating System: macOS 14 (Sonoma) or higher.
2. Development Tools:
   * **.NET 8 SDK**: Download and install from the [.NET website](https://dotnet.microsoft.com/download/dotnet/8.0).
   * **Visual Studio Code**: Install the latest version from the [official site](https://code.visualstudio.com/).
   * **.NET MAUI Extension for VS Code**: Install from the [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-maui). This extension also installs the C# Dev Kit and C# extensions for debugging and building .NET projects. 
3. Apple Developer Tools:
   * **Xcode**: Install the latest stable version via the Mac App Store.
   * **Command Line Tools for Xcode**: Install by running `xcode-select --install` in Terminal.

### Setup Steps
1. **Install .NET MAUI Workloads**:
   * Open Terminal and run:
        ```zsh
        dotnet workload install maui
        ```
     This command ensures your system is ready to build and run MAUI projects. 
2. **Clone the Repository**:
   * In Terminal, execute:
        ```zsh
        git clone https://github.com/Llama-Logic/PlumbBuddy.git
        cd PlumbBuddy
        ```
3. **Open the Project in VS Code**:
   * Launch Visual Studio Code.
   * Open the cloned PlumbBuddy folder.
4. **Restore Dependencies**:
   * In VS Code's integrated terminal, run:
        ```zsh
        dotnet restore
        ```
5. **Select Target Framework**:
   * In the VS Code status bar, click on the target framework selector (usually displays the current framework).
   * Choose `.NET 8.0` from the dropdown.
6. **Select Debug Target**:
   * Hover over the curly braces `{}` in the status bar and select "Debug Target".
   * Choose `Mac Catalyst` to run the app as a native macOS application.
7. Build and Run the App:
   * Press `F5` to start debugging.
   * The app should launch as a macOS Catalyst app in a native macOS window.

### Common Issues and Fixes
* **Missing Dependencies**:
  * Ensure all required workloads and SDKs are installed.
  * Verify that Xcode is installed and up to date.
* **Build Errors**:
  * Confirm that dependencies are restored using `dotnet restore`.

# License
[MIT License](LICENSE)

# Contributing
[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

## Frameworks
PlumbBuddy relies on the following frameworks:
* [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
* [Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2) (on Windows)
* [.NET](https://dotnet.microsoft.com/)
* [.NET Multi-platform App UI (MAUI)](https://dotnet.microsoft.com/apps/maui)
* [WebKit](https://webkit.org/) (on macOS)

## Libraries
PlumbBuddy uses the following libraries:
* [AsyncEx](https://github.com/StephenCleary/AsyncEx)
* [Autofac](https://autofac.org/)
* [Humanizer](https://github.com/Humanizr/Humanizer)
* [INI File Parser](https://github.com/rickyah/ini-parser)
* [LlamaLogic Packages](https://github.com/Llama-Logic/LlamaLogic)
* [Markdown component of MudBlazor](https://github.com/MyNihongo/MudBlazor.Markdown)
* [MaterialDesignIcons for MudBlazor](https://github.com/bromix/Bromix.MudBlazor.MaterialDesignIcons)
* [MudBlazor](https://github.com/MudBlazor/MudBlazor)
* [MudBlazor Extensions](https://github.com/CodeBeamOrg/CodeBeam.MudBlazor.Extensions)
* [.NET MAUI Community Toolkit](https://github.com/CommunityToolkit/Maui)
* [Octokit](https://github.com/octokit)
* [PolySharp](https://github.com/Sergio0694/PolySharp)
* [Serilog](https://github.com/serilog/serilog)
* [Theme Manager / Generator for MudBlazor](https://github.com/MudBlazor/ThemeManager)
* [Vdf.NET](https://github.com/shravan2x/Gameloop.Vdf)
* [YamlDotNet](https://github.com/aaubry/YamlDotNet)