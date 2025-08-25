<p align="center">
  <img width="96" align="center" src="https://user-images.githubusercontent.com/62196528/206452068-ba9f900d-28f2-4415-b9a4-339515c1282a.png" />
</p>
<h1 align="center">
  Fluent Registry Editor
</h1>

Registry Valley is the next generation Registry editor that replace Regedit.exe. In addition to all Regiedit's features, it supports dark theme, powerful WinUI with fluent controls, and custom color themes.

We will be happy if you'd like to contribute this project. Also, we welcome complaints about the legacy app Regedit.exe and suggestions for newf features. Please file an issue in GitHub Issues page if you have encountered any unexpected behaviors.

## Disclaimer & Privacy policy

We do not compensate for any loss of data that occurs while using this app. Also, the app does not access any personal information without the user's permission. You can see all allowed registry changes in the log on the app settings page.

## Installation

### Microsoft Store

_Not yet_

### Building from source

**Prerequisites**

- Windows 10 1809 (10.0.17763.0) onwards with Developer Mode enabled in the Windows Settings
- [Git](https://git-scm.com/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) with following individual components:
  - Windows 11 (10.0.26100.0) SDK
  - Windows App SDK
  - .NET 9 SDK

### 2. Clone and build the project

- Clone the repo: `git clone https://github.com/0x5bfa/RegistryEditor`
- Open `RegistryEditor.slnx`.
- 'Set as Startup item' on `RegistryEditor.WinUI.Packaging` in the Solution Explorer.
- Build with `Debug`, `x64`, `RegistryEditor.WinUI.Packaging`.

## Screenshots

![image](https://user-images.githubusercontent.com/62196528/212941487-0da3d39d-5b55-4b3d-994b-9055d372aa76.png)

## Credit

- Huge thanks to [@zeealeid](https://twitter.com/zeealeid) for creating this app's logo and concept.
