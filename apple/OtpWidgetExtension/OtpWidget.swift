import WidgetKit
import SwiftUI

@main
struct OtpWidgetBundle: WidgetBundle {
    var body: some Widget {
        OtpWidget()
    }
}

struct OtpWidget: Widget {
    let kind: String = "OtpWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: OtpTimelineProvider()) { entry in
            OtpWidgetEntryView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
        }
        .configurationDisplayName("OTP Code")
        .description("Display your two-factor authentication code")
        .supportedFamilies([.systemSmall, .systemMedium])
        #if os(iOS)
        .supportedFamilies([.systemSmall, .systemMedium, .accessoryCircular, .accessoryRectangular])
        #endif
    }
}
