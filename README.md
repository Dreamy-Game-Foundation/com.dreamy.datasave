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
    "com.dreamy.datasave": "https://github.com/Dreamy-Game-Foundation/com.dreamy.datasave.git#v0.1.0"
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

Writes are atomic: temp file first, backup current file, then replace.

## Editor Tools

This package owns its save-data editor menu:

- `Tools/Dreamy/Save/Open Save Folder`
- `Tools/Dreamy/Save/Clear Save Data`
