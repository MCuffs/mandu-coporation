// Offline OCR regression check for the clipped Korean title / invisible start-button defect.
// Reads an existing capture only; never starts or activates Unity or a player.
import Foundation
import Vision
import ImageIO

let args = CommandLine.arguments
if args.count < 2 { fputs("Usage: swift Tools/verify_dad_title.swift <capture.png>\n", stderr); exit(64) }
let url = URL(fileURLWithPath: args[1])
let request = VNRecognizeTextRequest()
request.recognitionLevel = .accurate
request.recognitionLanguages = ["ko-KR", "en-US"]
request.usesLanguageCorrection = true
request.customWords = ["아빠 전쟁", "게임 시작", "이야기 다시 보기"]
do {
    try VNImageRequestHandler(url: url).perform([request])
    let rows: [[String: Any]] = (request.results ?? []).compactMap { result in
        guard let candidate = result.topCandidates(1).first else { return nil }
        return ["text": candidate.string, "confidence": candidate.confidence,
                "x": Double(result.boundingBox.midX), "y": Double(result.boundingBox.midY)]
    }
    func has(_ text: String, below: Double? = nil, above: Double? = nil) -> Bool {
        return rows.contains { row in
            let normalized = (row["text"] as! String).components(separatedBy: .whitespacesAndNewlines).joined()
            let y = row["y"] as! Double
            return normalized.contains(text) && (below == nil || y < below!) && (above == nil || y > above!)
        }
    }
    let checks: [String: Bool] = ["full_title": has("아빠전쟁", above: 0.6),
                                  "start_button": has("게임시작", below: 0.3),
                                  "replay_button": has("이야기다시보기", below: 0.2)]
    let passed = checks.values.allSatisfy { $0 }
    let result: [String: Any] = ["capture": url.path, "passed": passed, "checks": checks, "recognized": rows,
                               "limit": "Offline OCR of this returning-player capture. Not a device rendering or gesture test."]
    let output = try JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])
    print(String(data: output, encoding: .utf8)!)
    exit(passed ? 0 : 2)
} catch { fputs("OCR failed: \(error)\n", stderr); exit(1) }
