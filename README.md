# com.dreamy.datasave

Versioned JSON save/load package for Dreamy internal Unity projects.

## Requirements

- Unity 6000.0+
- `com.unity.nuget.newtonsoft-json`

When using private Git URL packages, list this package in the game template manifest. Unity registry dependencies such as Newtonsoft can resolve by version.

## Install

```json
{
  "dependencies": {
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.dreamy.datasave": "https://github.com/Dreamy-Game-Foundation/com.dreamy.datasave.git#v0.2.0"
  }
}
```

## Usage

```csharp
[Serializable]
public sealed class PlayerSave : SaveData
{
    public int Coins;
    public Dictionary<string, int> Items = new();
}

var service = new DatasaveService();
var player = service.Load<PlayerSave>();
player.Coins += 100;
service.Save(player);
```

By default, loading a missing save creates and writes a complete JSON envelope
using the default values from the `SaveData` type. Set
`DatasaveOptions.CreateFileOnFirstLoad` to `false` to keep creation in memory.

Register it from the game template composition root if `com.dreamy.core` is installed:

```csharp
ServiceLocator.Register<IDatasaveService>(new DatasaveService());
```

## Save Format

Each save file stores a typed envelope with:

- save format version
- data type
- data version
- UTC timestamp
- JSON payload

Writes use a temp file, preserve the previous file as a last-known-good backup,
then replace the current file. If the current file cannot be decoded or
validated, load automatically validates the backup and restores it when valid.

The loader validates envelope format, stored data type, data version, and
payload before deserialization. Unsupported future versions fail with a
`DatasaveException` instead of being interpreted by an older client.

`AesSaveCodec` writes authenticated AES payloads using HMAC-SHA256 and remains
able to read the legacy unauthenticated format. XOR is obfuscation only and
must not be treated as secure storage.

`SaveAll()` snapshots loaded entries before writing, so updating the internal
cache during each save does not invalidate dictionary enumeration. Null entries
are skipped with a warning.

## Editor Tools

This package owns its save-data editor menu:

- `Tools/Dreamy/Save/Open Save Folder`
- `Tools/Dreamy/Save/Clear Save Data`
