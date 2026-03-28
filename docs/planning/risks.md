# Risk Register

This register captures the main technical and product-delivery risks known at the setup stage. Likelihood and impact are deliberately lightweight and should be revisited after feasibility work.

| Risk | Description | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| Raw Input may not distinguish devices reliably | Windows may expose keyboard and mouse events inconsistently across devices, drivers, or connection methods. | Medium | High | Prioritise a dedicated feasibility prototype and record findings against multiple device types before committing to the production design. |
| Programmatic display switching may be unreliable | Display switching behaviour may vary by GPU driver, Windows version, monitor state, or HDMI/TV wake timing. | Medium | High | Build a separate switching prototype, test on the target hardware, and document known constraints and fallback behaviour. |
| Device identifiers may not remain stable | Useful device identifiers may change after reconnects, reboots, Bluetooth re-pairing, or receiver changes. | High | High | Investigate which identifiers remain stable in practice and design registry logic to tolerate remapping or re-enrolment. |
| HDMI or disconnected display edge cases may break switching | The desired display may be sleeping, disconnected, unavailable, or reported differently by Windows at runtime. | Medium | High | Capture edge-case evidence during feasibility work and design recovery behaviour before declaring the MVP complete. |
| Lock screen support may be infeasible | Secure desktop boundaries may prevent the application from observing input or switching displays when the workstation is locked. | Medium | Medium | Treat lock screen support as a research item rather than an MVP requirement. |
| False switching or thrashing may harm usability | Incidental activity from another zone could trigger an unwanted change or repeated switching. | Medium | High | Plan for cooldowns, priorities, explicit state tracking, and evaluation of false-switch rate as core quality measures. |
| Feasibility findings may force scope changes | Early technical evidence may invalidate some assumptions about architecture or desired features. | Medium | Medium | Keep the design modular, mark provisional decisions clearly, and allow roadmap updates based on prototype evidence. |
