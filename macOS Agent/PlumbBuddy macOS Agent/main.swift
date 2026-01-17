import Cocoa

func ensureAccessibility(prompt: Bool) -> Bool {
    let options = [
        kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: prompt
    ]
    return AXIsProcessTrustedWithOptions(options as CFDictionary)
}

func createEventTap() -> CFMachPort? {
    let mask =
        (1 << CGEventType.keyDown.rawValue) |
        (1 << CGEventType.keyUp.rawValue)

    return CGEvent.tapCreate(
        tap: .cgSessionEventTap,
        place: .headInsertEventTap,
        options: .defaultTap,
        eventsOfInterest: CGEventMask(mask),
        callback: { _, type, event, _ in
            let keyCode = event.getIntegerValueField(.keyboardEventKeycode)
            let isDown = type == .keyDown
            let prefix = isDown ? "D" : "U"
            print("\(prefix) \(keyCode)")
            fflush(stdout)
            return Unmanaged.passRetained(event)
        },
        userInfo: nil
    )
}

// Step 1: Prompt if needed
if !ensureAccessibility(prompt: true) {
    print("WAITING_FOR_ACCESSIBILITY")
    fflush(stdout)

    // Stay alive until the user grants permission
    while !ensureAccessibility(prompt: false) {
        RunLoop.current.run(until: Date(timeIntervalSinceNow: 0.5))
    }
}

// Step 2: Now we are trusted — create the tap
guard let tap = createEventTap() else {
    print("FAILED_TO_CREATE_EVENT_TAP")
    fflush(stdout)
    CFRunLoopRun()
    fatalError("Unreachable")
}

let source = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
CFRunLoopAddSource(CFRunLoopGetCurrent(), source, .commonModes)
CGEvent.tapEnable(tap: tap, enable: true)

// Step 3: Normal operation
print("EVENT_TAP_ACTIVE")
fflush(stdout)

CFRunLoopRun()