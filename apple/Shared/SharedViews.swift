import SwiftUI

// MARK: - Initial Circle

struct InitialCircle: View {
    let initial: String
    let color: String
    let size: CGFloat

    var body: some View {
        ZStack {
            Circle()
                .fill(Color(hex: color) ?? .blue)
                .frame(width: size, height: size)

            Text(initial)
                .font(.system(size: size * 0.4, weight: .bold))
                .foregroundColor(.white)
        }
    }
}

// MARK: - Color Extension

extension Color {
    init?(hex: String) {
        var hexSanitized = hex.trimmingCharacters(in: .whitespacesAndNewlines)
        hexSanitized = hexSanitized.replacingOccurrences(of: "#", with: "")

        var rgb: UInt64 = 0

        guard Scanner(string: hexSanitized).scanHexInt64(&rgb) else {
            return nil
        }

        self.init(
            red: Double((rgb & 0xFF0000) >> 16) / 255.0,
            green: Double((rgb & 0x00FF00) >> 8) / 255.0,
            blue: Double(rgb & 0x0000FF) / 255.0
        )
    }

    static var cardBackground: Color {
        #if os(macOS)
        return Color(nsColor: .windowBackgroundColor)
        #else
        return Color(uiColor: .systemBackground)
        #endif
    }
}

// MARK: - Preview

#Preview {
    HStack(spacing: 16) {
        InitialCircle(initial: "G", color: "#4285F4", size: 48)
        InitialCircle(initial: "A", color: "#EA4335", size: 48)
        InitialCircle(initial: "M", color: "#34A853", size: 48)
    }
    .padding()
}
