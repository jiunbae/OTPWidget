import SwiftUI

@main
struct OtpAuthenticatorApp: App {
    @StateObject private var appState = AppState()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
        }
        #if os(macOS)
        .windowStyle(.hiddenTitleBar)
        .windowResizability(.contentSize)
        #endif

        #if os(macOS)
        Settings {
            SettingsView()
                .environmentObject(appState)
        }

        MenuBarExtra {
            MenuBarView()
                .environmentObject(appState)
        } label: {
            Image(systemName: "lock.shield.fill")
        }
        .menuBarExtraStyle(.window)
        #endif
    }
}

// MARK: - App State

class AppState: ObservableObject {
    @Published var accounts: [OtpAccount] = []
    @Published var folders: [OtpFolder] = []
    @Published var selectedAccount: OtpAccount?
    @Published var selectedFolderId: String?  // nil = All Accounts
    @Published var showingAddAccount = false
    @Published var showingQRScanner = false
    @Published var showingQRImageImport = false
    @Published var showingAddFolder = false
    @Published var searchText = ""

    private let store = AccountStore.shared
    private var timer: Timer?

    init() {
        loadAccounts()
        loadFolders()
        startTimer()
    }

    deinit {
        timer?.invalidate()
    }

    // MARK: - Account Operations

    func loadAccounts() {
        accounts = store.getAllAccounts()
    }

    func addAccount(_ account: OtpAccount) {
        store.addAccount(account)
        loadAccounts()
    }

    func updateAccount(_ account: OtpAccount) {
        store.updateAccount(account)
        loadAccounts()
    }

    func deleteAccount(_ account: OtpAccount) {
        store.deleteAccount(id: account.id)
        loadAccounts()
    }

    func toggleFavorite(_ account: OtpAccount) {
        var updated = account
        updated.isFavorite.toggle()
        updateAccount(updated)
    }

    func moveAccount(_ account: OtpAccount, toFolder folderId: String?) {
        var updated = account
        updated.folderId = folderId
        updateAccount(updated)
    }

    // MARK: - Folder Operations

    func loadFolders() {
        folders = store.getAllFolders()
    }

    func addFolder(_ folder: OtpFolder) {
        store.addFolder(folder)
        loadFolders()
    }

    func updateFolder(_ folder: OtpFolder) {
        store.updateFolder(folder)
        loadFolders()
    }

    func deleteFolder(_ folder: OtpFolder) {
        store.deleteFolder(id: folder.id)
        loadFolders()
        loadAccounts()
    }

    // MARK: - Computed Properties

    var filteredAccounts: [OtpAccount] {
        var result = accounts

        // 폴더 필터링
        if let folderId = selectedFolderId {
            result = result.filter { $0.folderId == folderId }
        }

        // 검색 필터링
        if !searchText.isEmpty {
            result = result.filter { account in
                account.issuer.localizedCaseInsensitiveContains(searchText) ||
                account.accountName.localizedCaseInsensitiveContains(searchText)
            }
        }

        return result
    }

    var favoriteAccounts: [OtpAccount] {
        filteredAccounts.filter { $0.isFavorite }
    }

    var regularAccounts: [OtpAccount] {
        filteredAccounts.filter { !$0.isFavorite }
    }

    var unfolderedAccounts: [OtpAccount] {
        accounts.filter { $0.folderId == nil }
    }

    func accountCount(inFolder folderId: String) -> Int {
        accounts.filter { $0.folderId == folderId }.count
    }

    private func startTimer() {
        timer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            self?.objectWillChange.send()
        }
    }
}
