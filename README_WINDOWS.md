# Hướng dẫn Windows

## 1. Mod dịch

Không cần cài .NET SDK và không cần tải source code.

1. Vào mục **Releases** của repository.
2. Tải gói có tên gần giống:

```text
BD2VietnameseSkillText-vX.Y.Z-Windows.zip
```

3. Tắt Brown Dust II.
4. Giải nén gói vào thư mục chứa:

```text
Brown Dust II.exe
```

Kết quả cần có:

```text
Brown Dust II.exe
BepInEx\
  plugins\
    BD2VietnameseSkillText\
      BD2VietnameseSkillText.dll
  config\
    BD2Vietnamese\
      SkillTextTable_EN.json
```

5. Mở game.
6. Vào Character → Skill để kiểm tra.

Nếu game đang mở và chỉ thay JSON, nhấn `F6`, sau đó đóng và mở lại cửa sổ skill.

## 2. Góp bản dịch

Cách dễ nhất là dùng GitHub Desktop.

1. Fork repository.
2. Chọn **Code → Open with GitHub Desktop**.
3. Clone fork về máy.
4. Tạo branch:

```text
translate/ten-nhan-vat
```

5. Sửa:

```text
translations\SkillTextTable_EN.raw.json
```

6. Kiểm tra JSON trong PowerShell:

```powershell
Get-Content `
  ".\translations\SkillTextTable_EN.raw.json" `
  -Raw |
ConvertFrom-Json |
Out-Null

Write-Host "JSON hợp lệ"
```

7. Commit, push và tạo Pull Request.

Không cần build plugin nếu chỉ góp bản dịch.

## 3. Build plugin

Yêu cầu:

- .NET SDK
- Brown Dust II PC
- BepInEx đã cài trong thư mục game

Mở PowerShell tại repository.

### Build Translator

```powershell
.\src\Translator\build.ps1 `
  -GameDir "C:\Neowiz\Browndust2\Browndust2_10000001"
```

### Build Exporter

```powershell
.\src\Exporter\build.ps1 `
  -GameDir "C:\Neowiz\Browndust2\Browndust2_10000001"
```

### Build FontFix

```powershell
.\src\FontFix\build.ps1 `
  -GameDir "C:\Neowiz\Browndust2\Browndust2_10000001"
```

Đường dẫn game có thể nằm trên ổ khác. Chỉ cần trỏ đến thư mục chứa `Brown Dust II.exe`.

Nếu PowerShell chặn script:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
```

Sau đó chạy lại `build.ps1`.

## 4. File nào được game đọc?

Game đọc:

```text
BepInEx\config\BD2Vietnamese\SkillTextTable_EN.json
```

Game không đọc:

```text
BepInEx\config\BD2Vietnamese\SkillTextTable_EN.raw.json
```

## 5. Gỡ cài đặt

Xóa:

```text
BepInEx\plugins\BD2VietnameseSkillText\
BepInEx\config\BD2Vietnamese\
```

## 6. Báo lỗi Windows

Đính kèm:

- Phiên bản game
- Phiên bản BepInEx
- Phiên bản plugin
- `BepInEx\LogOutput.log`
- Ảnh màn hình skill bị lỗi

Không đăng save, tài khoản, token hoặc dữ liệu cá nhân.
