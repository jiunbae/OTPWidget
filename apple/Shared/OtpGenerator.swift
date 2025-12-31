import Foundation
import CryptoKit

/// TOTP/HOTP 코드 생성기
public class OtpGenerator {

    public enum Algorithm: String, Codable {
        case sha1 = "SHA1"
        case sha256 = "SHA256"
        case sha512 = "SHA512"
    }

    public enum OtpType: String, Codable {
        case totp = "TOTP"
        case hotp = "HOTP"
    }

    /// TOTP 코드 생성
    public static func generateTOTP(
        secret: String,
        algorithm: Algorithm = .sha1,
        digits: Int = 6,
        period: Int = 30,
        date: Date = Date()
    ) -> String? {
        guard let secretData = base32Decode(secret) else {
            return nil
        }

        let counter = UInt64(date.timeIntervalSince1970) / UInt64(period)
        return generateOTP(secret: secretData, counter: counter, algorithm: algorithm, digits: digits)
    }

    /// HOTP 코드 생성
    public static func generateHOTP(
        secret: String,
        counter: UInt64,
        algorithm: Algorithm = .sha1,
        digits: Int = 6
    ) -> String? {
        guard let secretData = base32Decode(secret) else {
            return nil
        }

        return generateOTP(secret: secretData, counter: counter, algorithm: algorithm, digits: digits)
    }

    /// 남은 시간 계산 (초)
    public static func getRemainingSeconds(period: Int = 30, date: Date = Date()) -> Int {
        let seconds = Int(date.timeIntervalSince1970)
        return period - (seconds % period)
    }

    /// 진행률 계산 (0.0 ~ 1.0)
    public static func getProgress(period: Int = 30, date: Date = Date()) -> Double {
        let remaining = Double(getRemainingSeconds(period: period, date: date))
        return remaining / Double(period)
    }

    // MARK: - Private Methods

    private static func generateOTP(
        secret: Data,
        counter: UInt64,
        algorithm: Algorithm,
        digits: Int
    ) -> String? {
        // Counter를 big-endian 바이트로 변환
        var counterBigEndian = counter.bigEndian
        let counterData = Data(bytes: &counterBigEndian, count: 8)

        // HMAC 계산
        let hmacData: Data
        switch algorithm {
        case .sha1:
            let hmac = HMAC<Insecure.SHA1>.authenticationCode(for: counterData, using: SymmetricKey(data: secret))
            hmacData = Data(hmac)
        case .sha256:
            let hmac = HMAC<SHA256>.authenticationCode(for: counterData, using: SymmetricKey(data: secret))
            hmacData = Data(hmac)
        case .sha512:
            let hmac = HMAC<SHA512>.authenticationCode(for: counterData, using: SymmetricKey(data: secret))
            hmacData = Data(hmac)
        }

        // Dynamic truncation
        let offset = Int(hmacData[hmacData.count - 1] & 0x0f)
        let truncatedHash = hmacData.withUnsafeBytes { ptr -> UInt32 in
            let start = ptr.baseAddress!.advanced(by: offset)
            return start.withMemoryRebound(to: UInt32.self, capacity: 1) { $0.pointee.bigEndian }
        }

        let code = (truncatedHash & 0x7fffffff) % UInt32(pow(10.0, Double(digits)))

        return String(format: "%0\(digits)d", code)
    }

    /// Base32 디코딩
    private static func base32Decode(_ string: String) -> Data? {
        let alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"
        let uppercased = string.uppercased().replacingOccurrences(of: " ", with: "").replacingOccurrences(of: "-", with: "")

        var bits = ""
        for char in uppercased {
            if char == "=" { continue }
            guard let index = alphabet.firstIndex(of: char) else { return nil }
            let value = alphabet.distance(from: alphabet.startIndex, to: index)
            bits += String(value, radix: 2).leftPadding(toLength: 5, withPad: "0")
        }

        var bytes = [UInt8]()
        for i in stride(from: 0, to: bits.count, by: 8) {
            let endIndex = min(i + 8, bits.count)
            let byteString = String(bits[bits.index(bits.startIndex, offsetBy: i)..<bits.index(bits.startIndex, offsetBy: endIndex)])
            if byteString.count == 8, let byte = UInt8(byteString, radix: 2) {
                bytes.append(byte)
            }
        }

        return Data(bytes)
    }
}

// MARK: - String Extension

private extension String {
    func leftPadding(toLength: Int, withPad character: Character) -> String {
        let stringLength = self.count
        if stringLength < toLength {
            return String(repeating: character, count: toLength - stringLength) + self
        } else {
            return self
        }
    }
}
