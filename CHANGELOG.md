# Changelog - com.dreamy.datasave

All notable changes to this package will be documented in this file.

## [0.2.0] - 2026-06-15

### Added

- Save envelope validation and domain-specific `DatasaveException`
- Last-known-good backup recovery when the current save is invalid
- Authenticated AES encoding with legacy payload compatibility
- Runtime tests for recovery, invalid envelopes, and tamper detection

### Changed

- Removed reflection from `SaveAll()`
- Preserved the previous valid save after successful writes
- Deleted main, backup, and temp files together

## [0.1.0] - 2026-06-06

### Added

- `SaveData` base class with version and migration hooks
- `IDatasaveService` and `DatasaveService`
- Atomic JSON file storage
- Plain, XOR, and AES codecs
- Auto-save MonoBehaviour helper
- Save folder open/clear editor menu items
- Sample `PlayerSave` data class
