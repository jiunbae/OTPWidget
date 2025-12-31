import WidgetKit
import Foundation

/// 위젯 타임라인 프로바이더
struct OtpTimelineProvider: TimelineProvider {

    func placeholder(in context: Context) -> OtpEntry {
        return OtpEntry.placeholder
    }

    func getSnapshot(in context: Context, completion: @escaping (OtpEntry) -> Void) {
        let entry = createEntry(for: Date())
        completion(entry)
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<OtpEntry>) -> Void) {
        var entries: [OtpEntry] = []
        let currentDate = Date()

        // 5분 동안 1초 단위로 엔트리 생성
        for secondOffset in stride(from: 0, to: 300, by: 1) {
            let entryDate = Calendar.current.date(byAdding: .second, value: secondOffset, to: currentDate)!
            let entry = createEntry(for: entryDate)
            entries.append(entry)
        }

        // 5분 후에 타임라인 새로고침
        let refreshDate = Calendar.current.date(byAdding: .minute, value: 5, to: currentDate)!
        let timeline = Timeline(entries: entries, policy: .after(refreshDate))
        completion(timeline)
    }

    // MARK: - Private Methods

    private func createEntry(for date: Date) -> OtpEntry {
        let account = AccountStore.shared.getFirstAccount()

        guard let account = account else {
            return OtpEntry.empty
        }

        let code = account.generateCode(at: date) ?? "------"
        let progress = OtpGenerator.getProgress(period: account.period, date: date)
        let remainingSeconds = OtpGenerator.getRemainingSeconds(period: account.period, date: date)

        return OtpEntry(
            date: date,
            account: account,
            code: code,
            progress: progress,
            remainingSeconds: remainingSeconds
        )
    }
}
