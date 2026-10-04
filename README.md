# Dreamy Datasave

Package thuộc Dreamy Game Studio. Hướng dẫn dưới đây mô tả cấu trúc, cách cài vào project và tích hợp ở root/scene.

## Cài package

Dùng Unity 6000.0 trở lên. Sandbox đã tham chiếu package bằng `file:../LocalPackages/com.dreamy.datasave`. Project khác dùng Package Manager > + > Install package from disk và chọn package.json, hoặc Git URL của repository nội bộ. Cài cả dependency Dreamy/Git vào manifest của game; version dependency không tự cấu hình registry riêng.

Dependency trực tiếp theo package.json:

- `com.unity.nuget.newtonsoft-json` (3.2.1)

## Cấu trúc và asmdef

| Assembly | Reference | Phạm vi |
| --- | --- | --- |
| `Dreamy.Datasave.Editor` | Dreamy.Datasave.Runtime | Chỉ Editor |
| `Dreamy.Datasave.Runtime` | Unity.Newtonsoft.Json | Runtime |

Trong asmdef của game, thêm assembly chứa API trực tiếp sử dụng. Code bootstrap reference thêm Core/DataConfig/Datasave/Economy theo nhu cầu; code async reference UniTask. Code gọi type sample reference assembly sample. Giữ Editor reference trong asmdef Editor-only.

## Cấu trúc và cài service

Runtime chứa SaveData, DatasaveService, options, envelope, codec và xử lý migration/backup. Editor chứa menu mở/xóa save. Package không phụ thuộc Core; game có thể dùng service trực tiếp hoặc đăng ký tại root.

```csharp
using Dreamy.Datasave;
using Dreamy.Core;

var datasave = new DatasaveService();
ServiceLocator.Register<IDatasaveService>(datasave);
```

Nếu dùng ServiceLocator, cài/reference Dreamy.Core.Runtime ở game. Tạo service một lần trong GameInstaller trước wallet và các feature có save.

```csharp
using System;
using Dreamy.Datasave;

[Serializable]
public sealed class PlayerSave : SaveData
{
    public int Coins;
}

// Trong method của game, với datasave đã khởi tạo:
var player = datasave.Load<PlayerSave>();
player.Coins += 100;
datasave.Save(player);
```

Game gọi SaveAll khi pause/quit theo nhu cầu và unregister IDatasaveService khi teardown. Lần load đầu mặc định tạo file với giá trị mặc định; DatasaveOptions.CreateFileOnFirstLoad=false chỉ giữ trong bộ nhớ đến khi save.

## An toàn dữ liệu và công cụ

File dùng envelope với version/type/time/payload. Ghi qua file tạm và backup; nếu file chính lỗi, loader kiểm tra backup trước khi phục hồi. Dữ liệu/version không hỗ trợ trả DatasaveException. Thay đổi SaveData phải có chiến lược migration; giữ key/type ổn định sau khi phát hành.

AesSaveCodec dùng payload xác thực; XOR chỉ là làm rối dữ liệu. Game quản lý key và options codec. Tools/Dreamy/Save/Open Save Folder mở nơi lưu; Clear Save Data xóa dữ liệu local phục vụ thử nghiệm.
## Sample

Manifest hiện không khai báo sample để import qua Package Manager.

## Addressables

Package này không có panel cần đăng ký vào Addressables Group. Việc đặt address của prefab/asset thuộc game hoặc package UI/Assets; không dùng Addressables thay bước đăng ký service/config/save.
