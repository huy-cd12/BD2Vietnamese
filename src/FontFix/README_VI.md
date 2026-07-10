# BD2 Vietnamese Font Fix v0.2.0

Bản này sửa lỗi `TMP_FontAsset.CreateFontAsset` chạy quá sớm.

Cơ chế mới:

1. Chờ TextMeshPro tải xong.
2. Quét font asset có sẵn trong game.
3. Chọn font có đủ ký tự tiếng Việt.
4. Chỉ khi không có font phù hợp mới thử tạo từ font hệ thống.
5. Nhấn `F7` để quét và tạo lại sau khi đã mở màn hình skill.

## Cài đặt

```bash
cd "$HOME/Downloads/BD2VietnameseFontFix"
chmod +x build.sh
./build.sh
```

## Kiểm tra log

```bash
grep -iE "BD2 Vietnamese Font Fix|TMP font candidate|Selected loaded TMP font|Created TMP font|No usable Vietnamese|font applied" "$GAME/BepInEx/LogOutput.log" |
tail -n 120
```

Kết quả tốt phải có một trong hai:

```text
Selected loaded TMP font asset: ...
```

hoặc:

```text
Created TMP font asset from system font: ...
```

Sau đó phải có:

```text
Vietnamese font applied to text objects: ...
```
