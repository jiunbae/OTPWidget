# OTP Authenticator

A cross-platform OTP (One-Time Password) authenticator application supporting TOTP and HOTP protocols. Available for **Windows**, **macOS**, and **iOS**.

## Features

- **TOTP/HOTP Support**: RFC 6238 (TOTP) and RFC 4226 (HOTP) compliant
- **Multiple Hash Algorithms**: SHA1, SHA256, SHA512
- **QR Code Import**: Scan from screen, select area, or import from image file
- **Folder Organization**: Organize accounts into custom folders
- **Favorites**: Mark frequently used accounts for quick access
- **Click to Copy**: Click OTP code to copy with visual feedback
- **Secure Storage**: Platform-native secure storage (Keychain on Apple, DPAPI on Windows)
- **System Tray/Menu Bar**: Quick access without opening the main app
- **Backup & Restore**: Export and import accounts with encryption

## Platforms

| Platform | Status | Min Version |
|----------|--------|-------------|
| Windows | ✅ | Windows 10 1809+ |
| macOS | ✅ | macOS 14.0+ |
| iOS | ✅ | iOS 17.0+ |

---

## Build Instructions

### Prerequisites

#### For Apple (macOS/iOS)
- **Xcode 15.0+** with Command Line Tools
- **XcodeGen** (for project generation)
  ```bash
  brew install xcodegen
  ```

#### For Windows
- **Visual Studio 2022** with the following workloads:
  - .NET Desktop Development
  - Windows App SDK (C# Templates)
- **.NET 8.0 SDK**
- **Windows 10 SDK (10.0.19041.0+)**

---

### Apple (macOS / iOS)

#### 1. Navigate to the Apple project directory
```bash
cd apple
```

#### 2. Generate Xcode project using XcodeGen
```bash
xcodegen generate
```

#### 3. Open in Xcode
```bash
open OtpAuthenticator.xcodeproj
```

#### 4. Build and Run

**For macOS:**
1. Select `OtpAuthenticator-macOS` scheme
2. Select "My Mac" as the destination
3. Press `Cmd + R` to build and run

**For iOS:**
1. Select `OtpAuthenticator-iOS` scheme
2. Select a simulator or connected device
3. Press `Cmd + R` to build and run

#### 5. Archive for Distribution
```bash
# macOS
xcodebuild archive -scheme OtpAuthenticator-macOS -archivePath build/OtpAuthenticator-macOS.xcarchive

# iOS
xcodebuild archive -scheme OtpAuthenticator-iOS -archivePath build/OtpAuthenticator-iOS.xcarchive
```

---

### Windows

#### 1. Open the solution
```bash
# Using Visual Studio
start OtpAuthenticator.Windows.sln

# Or using dotnet CLI
cd src
```

#### 2. Restore NuGet packages
```bash
dotnet restore OtpAuthenticator.Windows.sln
```

#### 3. Build the project

**Using Visual Studio:**
1. Open `OtpAuthenticator.Windows.sln`
2. Select `Debug` or `Release` configuration
3. Press `F5` to build and run

**Using CLI:**
```bash
# Debug build
dotnet build src/OtpAuthenticator.App/OtpAuthenticator.App.csproj

# Release build
dotnet build src/OtpAuthenticator.App/OtpAuthenticator.App.csproj -c Release
```

#### 4. Run the application
```bash
dotnet run --project src/OtpAuthenticator.App/OtpAuthenticator.App.csproj
```

#### 5. Publish for Distribution
```bash
# Self-contained executable
dotnet publish src/OtpAuthenticator.App/OtpAuthenticator.App.csproj -c Release -r win-x64 --self-contained

# MSIX package (requires certificate)
dotnet publish src/OtpAuthenticator.App/OtpAuthenticator.App.csproj -c Release -p:Platform=x64
```

---

## Project Structure

```
OTPWidget/
├── apple/                          # Apple platforms (macOS, iOS)
│   ├── project.yml                 # XcodeGen configuration
│   ├── OtpAuthenticator/          # Main app source
│   │   ├── Views/                 # SwiftUI views
│   │   ├── OtpAuthenticatorApp.swift
│   │   └── ContentView.swift
│   ├── OtpWidgetExtension/        # Widget extension
│   └── Shared/                    # Shared code (models, services)
│       ├── OtpAccount.swift
│       ├── OtpGenerator.swift
│       └── AccountStore.swift
│
├── src/                            # Windows platform
│   ├── OtpAuthenticator.App/      # WinUI 3 application
│   │   ├── Views/                 # XAML views
│   │   ├── ViewModels/            # MVVM ViewModels
│   │   └── Converters/            # Value converters
│   ├── OtpAuthenticator.Core/     # Core library (cross-platform)
│   │   ├── Models/                # Data models
│   │   └── Services/              # Business logic
│   └── OtpAuthenticator.Core.Windows/  # Windows-specific services
│
└── OtpAuthenticator.Windows.sln   # Windows solution file
```

---

## Key Technologies

### Apple
- **SwiftUI** - Declarative UI framework
- **Vision** - QR code detection from images
- **WidgetKit** - Home screen widgets
- **Keychain** - Secure credential storage
- **App Groups** - Data sharing between app and widget

### Windows
- **WinUI 3** - Modern Windows UI framework
- **CommunityToolkit.Mvvm** - MVVM pattern implementation
- **ZXing.NET** - QR code encoding/decoding
- **H.NotifyIcon** - System tray support
- **Windows PasswordVault / DPAPI** - Secure storage

---

## Security

- Secret keys are stored in platform-native secure storage:
  - **Apple**: Keychain Services
  - **Windows**: Windows PasswordVault with DPAPI fallback
- Account metadata is encrypted before storage
- Backup files are encrypted with user-provided password (AES-256)
- Clipboard is automatically cleared after configurable timeout

---

## License

MIT License - See [LICENSE](LICENSE) for details.

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
