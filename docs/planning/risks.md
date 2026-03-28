# Technical Risks

## 1. Physical device differentiation
Windows may expose device-specific input inconsistently depending on device type, driver behaviour, and connection method.

## 2. Device identity persistence
Useful identifiers may change across reconnects, reboots, or receiver changes.

## 3. Display switching reliability
Display switching may behave differently depending on GPU drivers, Windows version, display state, and HDMI/TV wake status.

## 4. False switching
Incidental input from a lower-priority zone could trigger unwanted switching unless strong safeguards exist.

## 5. Lock screen feasibility
Secure desktop/session boundaries may limit what can be done from a normal desktop process.

## 6. Recovery scenarios
Disconnected or sleeping displays may require fallback behaviour to avoid leaving the user stranded.
