import Foundation
import Security

/// Keychain 헬퍼 클래스
public class KeychainHelper {

    public static let shared = KeychainHelper()

    private let service = "com.otpauthenticator"
    private let accessGroup = "group.com.otpauthenticator"

    private init() {}

    // MARK: - Generic Keychain Operations

    /// 데이터 저장
    public func save(_ data: Data, for key: String) -> Bool {
        // 기존 항목 삭제
        delete(key)

        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecValueData as String: data,
            kSecAttrAccessible as String: kSecAttrAccessibleWhenUnlockedThisDeviceOnly,
            kSecAttrAccessGroup as String: accessGroup
        ]

        let status = SecItemAdd(query as CFDictionary, nil)
        return status == errSecSuccess
    }

    /// 데이터 조회
    public func load(key: String) -> Data? {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecReturnData as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne,
            kSecAttrAccessGroup as String: accessGroup
        ]

        var result: AnyObject?
        let status = SecItemCopyMatching(query as CFDictionary, &result)

        if status == errSecSuccess {
            return result as? Data
        }
        return nil
    }

    /// 데이터 삭제
    @discardableResult
    public func delete(_ key: String) -> Bool {
        let query: [String: Any] = [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: key,
            kSecAttrAccessGroup as String: accessGroup
        ]

        let status = SecItemDelete(query as CFDictionary)
        return status == errSecSuccess || status == errSecItemNotFound
    }

    // MARK: - String Convenience Methods

    /// 문자열 저장
    public func saveString(_ string: String, for key: String) -> Bool {
        guard let data = string.data(using: .utf8) else { return false }
        return save(data, for: key)
    }

    /// 문자열 조회
    public func loadString(key: String) -> String? {
        guard let data = load(key: key) else { return nil }
        return String(data: data, encoding: .utf8)
    }

    // MARK: - Codable Support

    /// Codable 객체 저장
    public func save<T: Encodable>(_ object: T, for key: String) -> Bool {
        guard let data = try? JSONEncoder().encode(object) else { return false }
        return save(data, for: key)
    }

    /// Codable 객체 조회
    public func load<T: Decodable>(key: String, as type: T.Type) -> T? {
        guard let data = load(key: key) else { return nil }
        return try? JSONDecoder().decode(type, from: data)
    }
}
