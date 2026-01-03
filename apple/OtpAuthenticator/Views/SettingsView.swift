import SwiftUI
import WidgetKit

struct SettingsView: View {
    @EnvironmentObject var appState: AppState
    @AppStorage("autoClipboard") private var autoClipboard = false
    @AppStorage("showInMenuBar") private var showInMenuBar = true
    @AppStorage("launchAtLogin") private var launchAtLogin = false
    @State private var showingWidgetSettings = false
    @State private var showingQRImport = false

    var body: some View {
        Form {
            Section("General") {
                Toggle("Copy code to clipboard on tap", isOn: $autoClipboard)

                #if os(macOS)
                Toggle("Show in menu bar", isOn: $showInMenuBar)
                Toggle("Launch at login", isOn: $launchAtLogin)
                #endif
            }

            Section("Widgets") {
                Button {
                    showingWidgetSettings = true
                } label: {
                    HStack {
                        Label("Widget Settings", systemImage: "square.grid.2x2")
                        Spacer()
                        Image(systemName: "chevron.right")
                            .foregroundColor(.secondary)
                    }
                }
                .buttonStyle(.plain)

                Button("Refresh All Widgets") {
                    WidgetCenter.shared.reloadAllTimelines()
                }
            }

            Section("Data") {
                #if os(macOS)
                Button {
                    showingQRImport = true
                } label: {
                    Label("Import from QR Image", systemImage: "qrcode.viewfinder")
                }
                #endif

                Button("Export Accounts") {
                    exportAccounts()
                }

                Button("Import Accounts") {
                    importAccounts()
                }
            }

            Section("About") {
                LabeledContent("Version", value: "1.0.0")
                LabeledContent("Accounts", value: "\(appState.accounts.count)")
            }

            #if DEBUG
            Section("Debug") {
                Button("Add Test Account") {
                    let account = OtpAccount(
                        issuer: "Test Service",
                        accountName: "test@example.com",
                        secretKey: "JBSWY3DPEHPK3PXP"
                    )
                    appState.addAccount(account)
                }

                Button("Clear All Accounts", role: .destructive) {
                    for account in appState.accounts {
                        appState.deleteAccount(account)
                    }
                }
            }
            #endif
        }
        .formStyle(.grouped)
        #if os(macOS)
        .frame(width: 400)
        .padding()
        #endif
        .sheet(isPresented: $showingWidgetSettings) {
            WidgetSettingsView()
        }
        #if os(macOS)
        .sheet(isPresented: $showingQRImport) {
            QRImageImportView()
        }
        #endif
    }

    private func exportAccounts() {
        guard let data = AccountStore.shared.exportAccounts() else { return }

        #if os(macOS)
        let panel = NSSavePanel()
        panel.allowedContentTypes = [.json]
        panel.nameFieldStringValue = "otp_backup.json"

        if panel.runModal() == .OK, let url = panel.url {
            try? data.write(to: url)
        }
        #else
        // iOS: Share sheet
        let activityVC = UIActivityViewController(
            activityItems: [data],
            applicationActivities: nil
        )
        if let windowScene = UIApplication.shared.connectedScenes.first as? UIWindowScene,
           let window = windowScene.windows.first,
           let rootVC = window.rootViewController {
            rootVC.present(activityVC, animated: true)
        }
        #endif
    }

    private func importAccounts() {
        #if os(macOS)
        let panel = NSOpenPanel()
        panel.allowedContentTypes = [.json]
        panel.allowsMultipleSelection = false

        if panel.runModal() == .OK, let url = panel.url,
           let data = try? Data(contentsOf: url) {
            _ = AccountStore.shared.importAccounts(from: data)
            appState.loadAccounts()
        }
        #else
        // iOS: Document picker would be implemented here
        #endif
    }
}

#if os(macOS)
import AppKit
#else
import UIKit
#endif

// MARK: - Preview

#Preview {
    SettingsView()
        .environmentObject(AppState())
}
