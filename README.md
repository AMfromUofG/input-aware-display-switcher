# Input Aware Display Switcher

A Windows desktop utility that automatically switches display profiles based on which physical input device is currently active.

This project is being developed as both:

- a genuinely useful Windows utility
- a technically substantial final year / dissertation-style software engineering project

## Overview

Windows display switching is often manual, awkward, and unreliable for multi-room or multi-zone setups.

This application aims to solve that by monitoring activity from specific physical keyboards and mice, mapping those devices to logical zones, and automatically switching the display profile to the most appropriate screen.

### Example use case

A PC is connected to:

- a desk monitor in one room
- a TV in another room via HDMI

Input devices are split by location:

- desk keyboard + mouse
- living room keyboard + optional mouse

When desk devices are active, the application switches to the monitor profile.  
When living room devices are active, the application switches to the TV profile.

The goal is to remove the need for manual `Win + P` switching and make multi-display, multi-room usage much more seamless.

---

## Core Objectives

- Detect activity from specific physical input devices
- Map devices to user-defined zones
- Map zones to display profiles
- Automatically switch display output based on current activity
- Prevent accidental or excessive switching with cooldowns and priority rules
- Provide a full Windows GUI for setup, control, diagnostics, and configuration

---

## Planned Feature Set

### Must-have

- Physical keyboard and mouse detection
- Device-to-zone mapping
- Display profile management
- Automatic switching engine
- Cooldown / anti-thrashing logic
- Zone priority system
- Manual lock / override
- GUI configuration application
- Persistent configuration
- Logging / diagnostics

### Good to have

- System tray mode
- Start with Windows
- Confidence thresholds
- Recovery / fallback profile
- Multi-zone scalability
- App-aware rules

### Stretch goals

- Lock screen support investigation
- Windows service companion
- Advanced weighted decision engine

---

## Proposed Tech Stack

- **Language:** C#
- **Platform:** .NET
- **GUI:** WPF
- **Configuration:** JSON
- **Logging:** structured logging
- **Platform target:** Windows only

---

## Proposed Architecture

The system is intended to be modular and split into the following areas:

- **Input Monitor** — captures device-specific keyboard/mouse activity
- **Device Registry** — stores known devices and metadata
- **Zone Mapper** — maps devices to logical user-defined zones
- **Decision Engine** — determines whether switching should occur
- **State Manager** — tracks active zone, cooldowns, locks, recent activity
- **Profile Manager** — stores and applies display profiles
- **Display Switcher** — triggers the actual Windows display/profile change
- **Config Storage** — persists rules, profiles, and device mappings
- **Logging / Telemetry** — records input events and switching decisions
- **GUI Application** — user-facing configuration and diagnostics interface

---

## Key Technical Risks

This project intentionally explores several non-trivial engineering challenges:

1. Reliably distinguishing physical keyboards and mice on Windows
2. Programmatically switching display profiles in a robust way
3. Preventing false switching and rapid profile thrashing
4. Handling disconnected displays and HDMI edge cases
5. Preserving meaningful device identity across reconnects / reboots
6. Investigating feasibility of lock screen or secure desktop support

---

## Project Status

Current focus:

- feasibility prototype for device-specific input detection
- feasibility prototype for programmatic display switching

The first major milestone is to prove that:
- multiple physical input devices can be differentiated reliably
- display profiles can be switched programmatically
- the two can be connected into a working MVP

---

## Repository Structure

```text
/src
/docs
/prototypes
/tests
