import AppKit
import Foundation

@main
struct LinkwiseDefaultHandler {
    static func main() async {
        guard #available(macOS 12.0, *) else {
            fail("Default-handler registration requires macOS 12 or later.", exitCode: 1)
        }

        guard CommandLine.arguments.count == 2 else {
            fail("Usage: Linkwise.DefaultHandler <Linkwise.app>", exitCode: 2)
        }

        let applicationURL = URL(
            fileURLWithPath: CommandLine.arguments[1],
            isDirectory: true
        )
        let infoPlistURL = applicationURL
            .appendingPathComponent("Contents", isDirectory: true)
            .appendingPathComponent("Info.plist", isDirectory: false)

        guard FileManager.default.fileExists(atPath: infoPlistURL.path) else {
            fail("The supplied path is not a macOS application bundle.", exitCode: 2)
        }

        do {
            try await NSWorkspace.shared.setDefaultApplication(
                at: applicationURL,
                toOpenURLsWithScheme: "http"
            )
            try await NSWorkspace.shared.setDefaultApplication(
                at: applicationURL,
                toOpenURLsWithScheme: "https"
            )
        } catch {
            fail(error.localizedDescription, exitCode: 1)
        }
    }

    private static func fail(_ message: String, exitCode: Int32) -> Never {
        FileHandle.standardError.write(Data("\(message)\n".utf8))
        exit(exitCode)
    }
}
