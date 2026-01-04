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

## Project Structure

```
OTPWidget/
├── apple/                      # Apple platforms (Native Swift)
│   ├── project.yml            # XcodeGen configuration
│   ├── OtpAuthenticator/      # macOS/iOS app source
│   ├── OtpWidgetExtension/    # Widget extension
│   └── Shared/                # Shared Swift code
│
├── windows/                    # Windows platform (.NET)
│   ├── OtpAuthenticator.App/  # WinUI 3 application
│   ├── OtpAuthenticator.Core/ # Core library
│   ├── OtpAuthenticator.Core.Windows/  # Windows services
│   └── OtpAuthenticator.Widget/        # Windows widget
│
├── docs/                       # Shared specifications
│   ├── SPEC.md                # OTP algorithm spec
│   ├── DATA_FORMAT.md         # Data format spec
│   └── BACKUP_FORMAT.md       # Backup format spec
│
├── shared/                     # Shared resources
│   └── (icons, localization)
│
├── tests/                      # Unit tests
│
└── OtpAuthenticator.Windows.sln  # Windows solution
```

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
- **Visual Studio 2022** with:
  - .NET Desktop Development
  - Windows App SDK (C# Templates)
- **.NET 8.0 SDK**
- **Windows 10 SDK (10.0.19041.0+)**

---

### Apple (macOS / iOS)

```bash
# Navigate to Apple project
cd apple

# Generate Xcode project
xcodegen generate

# Open in Xcode
open OtpAuthenticator.xcodeproj
```

**Build:**
- macOS: Select `OtpAuthenticator-macOS` scheme → My Mac → Cmd+R
- iOS: Select `OtpAuthenticator-iOS` scheme → Simulator → Cmd+R

---

### Windows

```bash
# Open solution in Visual Studio
start OtpAuthenticator.Windows.sln

# Or build via CLI
dotnet restore OtpAuthenticator.Windows.sln
dotnet build windows/OtpAuthenticator.App/OtpAuthenticator.App.csproj -p:Platform=x64
```

**Run:**
```bash
# After building
windows\OtpAuthenticator.App\bin\x64\Debug\net8.0-windows10.0.22621.0\OtpAuthenticator.exe
```

---

## Key Technologies

### Apple
- **SwiftUI** - Declarative UI
- **Vision** - QR code detection
- **WidgetKit** - Home screen widgets
- **Keychain** - Secure storage

### Windows
- **WinUI 3** - Modern Windows UI
- **CommunityToolkit.Mvvm** - MVVM pattern
- **ZXing.NET** - QR code handling
- **H.NotifyIcon** - System tray

---

## Documentation

- [OTP Algorithm Specification](docs/SPEC.md)
- [Data Format Specification](docs/DATA_FORMAT.md)
- [Backup Format Specification](docs/BACKUP_FORMAT.md)

---

## Security

- Secrets stored in platform-native secure storage:
  - **Apple**: Keychain Services
  - **Windows**: Windows PasswordVault / DPAPI
- Backup files encrypted with AES-256-GCM
- Clipboard auto-clear after configurable timeout

---

## License

MIT License - See [LICENSE](LICENSE) for details.

---

## Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request
