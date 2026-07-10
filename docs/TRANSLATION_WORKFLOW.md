# Quy trình dịch skill

## Mục tiêu

Lấy ID và text gốc từ game, dịch trong file RAW, kiểm tra rồi gộp vào file ACTIVE.

## Hai file

```text
SkillTextTable_EN.json
```

File ACTIVE. Game đang đọc.

```text
SkillTextTable_EN.raw.json
```

File RAW. Dùng để làm việc; game không đọc.

## Quy trình Linux bằng shortcut

### Bước 1: Xóa capture cũ

```bash
bd2clear
```

Trong game:

```text
F11
```

### Bước 2: Thu thập

Trong game:

```text
F9
→ mở skill +0
→ mở skill +1
→ mở skill +2
→ mở skill +3
→ mở skill +4
→ mở skill +5
→ F9
```

### Bước 3: Lấy ID vào RAW

```bash
bd2pull
```

Xem RAW:

```bash
bd2raw
```

### Bước 4: Dịch

```bash
bd2editraw
```

Chỉ sửa nội dung bên phải. Không sửa ID.

Giữ nguyên:

```text
<#ffa024>
<#91dc69>
<#ddc37a>
</color>
[3[
<6<
<5<
{6{
{5{
<1<
|
```

### Bước 5: Kiểm tra

```bash
bd2check
```

### Bước 6: Gộp vào file game

```bash
bd2push
```

### Bước 7: Nạp lại

Trong game:

```text
F6
```

Sau đó đóng và mở lại cửa sổ skill.

## Quy trình Windows cho người dịch

Người dùng Windows có thể sửa trực tiếp:

```text
translations\SkillTextTable_EN.raw.json
```

Kiểm tra trong PowerShell:

```powershell
Get-Content `
  ".\translations\SkillTextTable_EN.raw.json" `
  -Raw |
ConvertFrom-Json |
Out-Null
```

Sau khi Pull Request được kiểm tra trong game, maintainer gộp nội dung vào:

```text
translations\SkillTextTable_EN.json
```
