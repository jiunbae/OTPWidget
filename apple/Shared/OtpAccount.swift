import Foundation

/// 폴더 모델
public struct OtpFolder: Codable, Identifiable, Hashable {
    public let id: String
    public var name: String
    public var icon: String
    public var color: String
    public var sortOrder: Int
    public var createdAt: Date

    public init(
        id: String = UUID().uuidString,
        name: String,
        icon: String = "folder.fill",
        color: String = "#007AFF",
        sortOrder: Int = 0,
        createdAt: Date = Date()
    ) {
        self.id = id
        self.name = name
        self.icon = icon
        self.color = color
        self.sortOrder = sortOrder
        self.createdAt = createdAt
    }
}

/// OTP 계정 모델
public struct OtpAccount: Codable, Identifiable, Hashable {
    public let id: String
    public var issuer: String
    public var accountName: String
    public var secretKey: String
    public var type: OtpGenerator.OtpType
    public var algorithm: OtpGenerator.Algorithm
    public var digits: Int
    public var period: Int
    public var counter: Int64
    public var isFavorite: Bool
    public var sortOrder: Int
    public var color: String
    public var createdAt: Date
    public var lastUsedAt: Date?
    public var folderId: String?  // 폴더 ID

    public init(
        id: String = UUID().uuidString,
        issuer: String,
        accountName: String,
        secretKey: String,
        type: OtpGenerator.OtpType = .totp,
        algorithm: OtpGenerator.Algorithm = .sha1,
        digits: Int = 6,
        period: Int = 30,
        counter: Int64 = 0,
        isFavorite: Bool = false,
        sortOrder: Int = 0,
        color: String = "#512BD4",
        createdAt: Date = Date(),
        lastUsedAt: Date? = nil,
        folderId: String? = nil
    ) {
        self.id = id
        self.issuer = issuer
        self.accountName = accountName
        self.secretKey = secretKey
        self.type = type
        self.algorithm = algorithm
        self.digits = digits
        self.period = period
        self.counter = counter
        self.isFavorite = isFavorite
        self.sortOrder = sortOrder
        self.color = color
        self.createdAt = createdAt
        self.lastUsedAt = lastUsedAt
        self.folderId = folderId
    }

    /// 표시 이름
    public var displayName: String {
        if issuer.isEmpty {
            return accountName
        }
        return "\(issuer) (\(accountName))"
    }

    /// 이니셜 (아이콘용)
    public var initial: String {
        let source = issuer.isEmpty ? accountName : issuer
        return String(source.prefix(1)).uppercased()
    }

    /// 현재 OTP 코드 생성
    public func generateCode(at date: Date = Date()) -> String? {
        switch type {
        case .totp:
            return OtpGenerator.generateTOTP(
                secret: secretKey,
                algorithm: algorithm,
                digits: digits,
                period: period,
                date: date
            )
        case .hotp:
            return OtpGenerator.generateHOTP(
                secret: secretKey,
                counter: UInt64(counter),
                algorithm: algorithm,
                digits: digits
            )
        }
    }

    /// otpauth:// URI 파싱
    public static func parse(uri: String) -> OtpAccount? {
        guard uri.lowercased().hasPrefix("otpauth://"),
              let url = URL(string: uri) else {
            return nil
        }

        let typeString = url.host?.lowercased() ?? "totp"
        let type: OtpGenerator.OtpType = typeString == "hotp" ? .hotp : .totp

        // 레이블 파싱
        var path = url.path
        if path.hasPrefix("/") {
            path = String(path.dropFirst())
        }
        path = path.removingPercentEncoding ?? path

        var issuer = ""
        var accountName = path

        if path.contains(":") {
            let parts = path.split(separator: ":", maxSplits: 1)
            if parts.count == 2 {
                issuer = String(parts[0])
                accountName = String(parts[1])
            }
        }

        // 쿼리 파라미터 파싱
        guard let components = URLComponents(url: url, resolvingAgainstBaseURL: false),
              let queryItems = components.queryItems else {
            return nil
        }

        var params: [String: String] = [:]
        for item in queryItems {
            params[item.name.lowercased()] = item.value
        }

        guard let secret = params["secret"] else {
            return nil
        }

        if let paramIssuer = params["issuer"], !paramIssuer.isEmpty {
            issuer = paramIssuer
        }

        let algorithm: OtpGenerator.Algorithm
        switch params["algorithm"]?.uppercased() {
        case "SHA256": algorithm = .sha256
        case "SHA512": algorithm = .sha512
        default: algorithm = .sha1
        }

        let digits = Int(params["digits"] ?? "6") ?? 6
        let period = Int(params["period"] ?? "30") ?? 30
        let counter = Int64(params["counter"] ?? "0") ?? 0

        return OtpAccount(
            issuer: issuer,
            accountName: accountName,
            secretKey: secret.uppercased().replacingOccurrences(of: " ", with: ""),
            type: type,
            algorithm: algorithm,
            digits: digits,
            period: period,
            counter: counter
        )
    }
}
