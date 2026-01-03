import Foundation

/// 계정 저장소 (App Group 공유)
public class AccountStore {

    public static let shared = AccountStore()

    private let accountsKey = "otp_accounts"
    private let foldersKey = "otp_folders"
    private let userDefaults: UserDefaults?

    private init() {
        // App Group UserDefaults 사용 (실패시 standard로 폴백)
        if let groupDefaults = UserDefaults(suiteName: "group.com.otpauthenticator") {
            userDefaults = groupDefaults
        } else {
            userDefaults = UserDefaults.standard
        }
    }

    // MARK: - Folder Operations

    /// 모든 폴더 조회
    public func getAllFolders() -> [OtpFolder] {
        guard let data = userDefaults?.data(forKey: foldersKey) else {
            return []
        }

        do {
            let folders = try JSONDecoder().decode([OtpFolder].self, from: data)
            return folders.sorted { $0.sortOrder < $1.sortOrder }
        } catch {
            print("Failed to decode folders: \(error)")
            return []
        }
    }

    /// 폴더 저장
    public func saveFolders(_ folders: [OtpFolder]) {
        do {
            let data = try JSONEncoder().encode(folders)
            userDefaults?.set(data, forKey: foldersKey)
        } catch {
            print("Failed to encode folders: \(error)")
        }
    }

    /// 폴더 추가
    public func addFolder(_ folder: OtpFolder) {
        var folders = getAllFolders()
        folders.append(folder)
        saveFolders(folders)
    }

    /// 폴더 업데이트
    public func updateFolder(_ folder: OtpFolder) {
        var folders = getAllFolders()
        if let index = folders.firstIndex(where: { $0.id == folder.id }) {
            folders[index] = folder
            saveFolders(folders)
        }
    }

    /// 폴더 삭제
    public func deleteFolder(id: String) {
        var folders = getAllFolders()
        folders.removeAll { $0.id == id }
        saveFolders(folders)

        // 해당 폴더의 계정들 폴더 ID 제거
        var accounts = getAllAccounts()
        for i in 0..<accounts.count {
            if accounts[i].folderId == id {
                accounts[i].folderId = nil
            }
        }
        saveAccounts(accounts)
    }

    /// 폴더 내 계정 조회
    public func getAccounts(inFolder folderId: String?) -> [OtpAccount] {
        return getAllAccounts().filter { $0.folderId == folderId }
    }

    // MARK: - Account Operations

    /// 모든 계정 조회
    public func getAllAccounts() -> [OtpAccount] {
        guard let data = userDefaults?.data(forKey: accountsKey) else {
            return []
        }

        do {
            let accounts = try JSONDecoder().decode([OtpAccount].self, from: data)
            return accounts.sorted { $0.sortOrder < $1.sortOrder }
        } catch {
            print("Failed to decode accounts: \(error)")
            return []
        }
    }

    /// 계정 저장
    public func saveAccounts(_ accounts: [OtpAccount]) {
        do {
            let data = try JSONEncoder().encode(accounts)
            userDefaults?.set(data, forKey: accountsKey)

            // 비밀키는 Keychain에 별도 저장
            for account in accounts {
                KeychainHelper.shared.saveString(account.secretKey, for: "secret_\(account.id)")
            }
        } catch {
            print("Failed to encode accounts: \(error)")
        }
    }

    /// 계정 추가
    public func addAccount(_ account: OtpAccount) {
        var accounts = getAllAccounts()
        accounts.append(account)
        saveAccounts(accounts)
    }

    /// 계정 업데이트
    public func updateAccount(_ account: OtpAccount) {
        var accounts = getAllAccounts()
        if let index = accounts.firstIndex(where: { $0.id == account.id }) {
            accounts[index] = account
            saveAccounts(accounts)
        }
    }

    /// 계정 삭제
    public func deleteAccount(id: String) {
        var accounts = getAllAccounts()
        accounts.removeAll { $0.id == id }
        saveAccounts(accounts)

        // Keychain에서도 삭제
        KeychainHelper.shared.delete("secret_\(id)")
    }

    /// ID로 계정 조회
    public func getAccount(id: String) -> OtpAccount? {
        return getAllAccounts().first { $0.id == id }
    }

    /// 즐겨찾기 계정 조회
    public func getFavoriteAccounts() -> [OtpAccount] {
        return getAllAccounts().filter { $0.isFavorite }
    }

    /// 첫 번째 계정 조회 (위젯용)
    public func getFirstAccount() -> OtpAccount? {
        let favorites = getFavoriteAccounts()
        if !favorites.isEmpty {
            return favorites.first
        }
        return getAllAccounts().first
    }

    // MARK: - Import/Export

    /// 백업 데이터 내보내기
    public func exportAccounts() -> Data? {
        let accounts = getAllAccounts()
        return try? JSONEncoder().encode(accounts)
    }

    /// 백업 데이터 가져오기
    public func importAccounts(from data: Data, merge: Bool = true) -> Bool {
        do {
            let importedAccounts = try JSONDecoder().decode([OtpAccount].self, from: data)

            if merge {
                var existingAccounts = getAllAccounts()
                let existingIds = Set(existingAccounts.map { $0.id })

                for account in importedAccounts {
                    if !existingIds.contains(account.id) {
                        existingAccounts.append(account)
                    }
                }

                saveAccounts(existingAccounts)
            } else {
                saveAccounts(importedAccounts)
            }

            return true
        } catch {
            print("Failed to import accounts: \(error)")
            return false
        }
    }
}
