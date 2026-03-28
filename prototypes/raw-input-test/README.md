# Raw Input Prototype

This prototype area is reserved for investigating whether Windows can distinguish physical keyboards and mice closely enough for the planned application.

## Objective

Determine whether Raw Input can provide device-specific activity information that is reliable enough to map physical input devices to logical zones.

## What Will Be Tested

- detection of multiple keyboards
- detection of multiple mice
- availability of per-device identifiers
- consistency of identifiers across repeated runs and reconnects
- behaviour differences across wired, wireless, and mixed-device setups where available

## Evidence To Collect

- observed device identifiers
- example input event traces
- notes on devices that cannot be distinguished reliably
- notes on reconnect and reboot behaviour

## Success Criteria

- the prototype can distinguish the target input devices with useful consistency
- the information exposed is sufficient to support a future device registry
- any important limitations are clearly documented for design decisions

No production implementation belongs here yet; this folder is only for a focused feasibility spike.
