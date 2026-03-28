# Display Switching Prototype

This prototype area is reserved for investigating whether the target Windows setup can switch display profiles programmatically and reliably enough for the planned application.

## Objective

Determine whether display switching can be triggered in a repeatable way on the intended hardware and operating system configuration.

## What Will Be Tested

- switching between the target monitor and TV profiles
- repeatability across multiple runs
- behaviour when a display is asleep, waking, or disconnected
- any timing or driver-related quirks that affect reliability

## Evidence To Collect

- success and failure rates across repeated trials
- notes on timing, wake-up, and reconnect behaviour
- relevant logs or command outputs
- constraints that should influence the MVP design

## Success Criteria

- the target profiles can be switched programmatically with acceptable reliability
- major edge cases are identified early
- known limitations are documented clearly enough to guide design and evaluation

No production switching implementation belongs here yet; this folder is only for a focused feasibility spike.
