import Cocoa

// MARK: - Stdout must be unbuffered when piped
setbuf(stdout, nil)

// MARK: - Accessibility

func ensureAccessibility(prompt: Bool) -> Bool {
    let options: NSDictionary = [
        kAXTrustedCheckOptionPrompt.takeUnretainedValue() as NSString: prompt
    ]
    return AXIsProcessTrustedWithOptions(options)
}

// MARK: - Event Tap

func createEventTap() -> CFMachPort? {
    let mask =
        (1 << CGEventType.keyDown.rawValue) |
        (1 << CGEventType.keyUp.rawValue)

    return CGEvent.tapCreate(
        tap: .cgSessionEventTap,
        place: .headInsertEventTap,
        options: .defaultTap,
        eventsOfInterest: CGEventMask(mask),
        callback: eventTapCallback,
        userInfo: nil
    )
}

func eventTapCallback(
    proxy: CGEventTapProxy,
    type: CGEventType,
    event: CGEvent,
    userInfo: UnsafeMutableRawPointer?
) -> Unmanaged<CGEvent>? {

    guard type == .keyDown || type == .keyUp else {
        return Unmanaged.passUnretained(event)
    }

    let keyCode = UInt16(event.getIntegerValueField(.keyboardEventKeycode))
    let prefix = (type == .keyDown) ? "D" : "U"

    print("\(prefix) \(keyCode)")

    // IMPORTANT: never retain the event
    return Unmanaged.passUnretained(event)
}

// MARK: - Main

// Step 1: Request accessibility if needed
if !ensureAccessibility(prompt: true) {
    print("WAITING_FOR_ACCESSIBILITY")

    while !ensureAccessibility(prompt: false) {
        RunLoop.current.run(until: Date(timeIntervalSinceNow: 0.5))
    }
}

// Step 2: Create the event tap
guard let tap = createEventTap() else {
    print("FAILED_TO_CREATE_EVENT_TAP")
    exit(1)
}

// Step 3: Install run loop source
let source = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
CFRunLoopAddSource(CFRunLoopGetCurrent(), source, .commonModes)
CGEvent.tapEnable(tap: tap, enable: true)

// Step 4: Signal readiness
print("EVENT_TAP_ACTIVE")

// Step 5: Run forever
CFRunLoopRun()