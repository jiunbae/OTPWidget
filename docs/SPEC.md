# OTP Algorithm Specification

This document defines the OTP algorithms used across all platforms to ensure compatibility.

## Supported Algorithms

### TOTP (Time-Based One-Time Password)
- **RFC**: [RFC 6238](https://tools.ietf.org/html/rfc6238)
- **Default Period**: 30 seconds
- **Supported Periods**: 30, 60 seconds

### HOTP (HMAC-Based One-Time Password)
- **RFC**: [RFC 4226](https://tools.ietf.org/html/rfc4226)
- **Counter**: 64-bit unsigned integer

## Hash Algorithms

| Algorithm | HMAC Output Size | Supported |
|-----------|------------------|-----------|
| SHA1      | 160 bits         | ✅ Default |
| SHA256    | 256 bits         | ✅ |
| SHA512    | 512 bits         | ✅ |

## OTP Generation

### Parameters
- **Secret**: Base32-encoded string (RFC 4648)
- **Digits**: 6 (default), 7, or 8
- **Algorithm**: SHA1 (default), SHA256, SHA512
- **Period**: 30 seconds (default) for TOTP
- **Counter**: Starting from 0 for HOTP

### Formula (TOTP)
```
T = floor((Current Unix Time) / Period)
HMAC = HMAC-Algorithm(Secret, T)
Offset = HMAC[19] & 0x0F
Code = (HMAC[Offset:Offset+4] & 0x7FFFFFFF) mod 10^Digits
```

### Formula (HOTP)
```
HMAC = HMAC-Algorithm(Secret, Counter)
Offset = HMAC[19] & 0x0F
Code = (HMAC[Offset:Offset+4] & 0x7FFFFFFF) mod 10^Digits
Counter = Counter + 1
```

## URI Format

Standard otpauth:// URI format (Google Authenticator compatible):

```
otpauth://TYPE/LABEL?PARAMETERS

TYPE: totp | hotp
LABEL: Issuer:AccountName (URL-encoded)

PARAMETERS:
  secret    = Base32 encoded secret (required)
  issuer    = Service name (recommended)
  algorithm = SHA1 | SHA256 | SHA512 (default: SHA1)
  digits    = 6 | 7 | 8 (default: 6)
  period    = 30 | 60 (TOTP only, default: 30)
  counter   = N (HOTP only, required)
```

### Example URIs

```
# TOTP with defaults
otpauth://totp/Example:alice@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Example

# TOTP with SHA256
otpauth://totp/GitHub:user?secret=ABCDEFGH&issuer=GitHub&algorithm=SHA256

# HOTP
otpauth://hotp/Service:account?secret=ABCDEFGH&issuer=Service&counter=0
```

## Test Vectors

### TOTP SHA1 (RFC 6238 Test Vectors)
| Time (Unix)  | Code     |
|--------------|----------|
| 59           | 94287082 |
| 1111111109   | 07081804 |
| 1111111111   | 14050471 |
| 1234567890   | 89005924 |
| 2000000000   | 69279037 |

Secret: `12345678901234567890` (ASCII) / `GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ` (Base32)

### HOTP (RFC 4226 Test Vectors)
| Counter | Code   |
|---------|--------|
| 0       | 755224 |
| 1       | 287082 |
| 2       | 359152 |
| 3       | 969429 |
| 4       | 338314 |

Secret: `12345678901234567890` (ASCII)
