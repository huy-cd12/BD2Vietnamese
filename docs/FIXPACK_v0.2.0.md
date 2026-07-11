# BD2 Vietnamese Fix Pack v0.2.0

Gói này sửa đồng thời plugin dịch skill và plugin dịch tooltip.

## Lỗi đã xác định

File skill cũ có tên skill cấp `+5`:

```text
10038025 = It's the Protection of the Oni! (Highest)
```

nhưng thiếu mô tả tương ứng:

```text
20038025
```

Vì vậy khi đang xem `+5`, game vẫn hiển thị mô tả tiếng Anh dù các ID
`20038020` đến `20038024` đã được dịch.

Gói này thêm `20038025` mà không xóa hoặc ghi đè các key hiện có.

## Skill plugin 0.2.0

- Giữ nguyên định dạng JSON cũ: `{ "ID": "text" }`.
- Hỗ trợ key `ID:field` như trước.
- Tự reload JSON khi file thay đổi.
- F6 vẫn reload thủ công.
- Nếu file bị xóa, plugin xóa bản dịch trong RAM thay vì giữ dữ liệu cũ.
- Log rõ ID đã áp dụng và ID còn thiếu.
- Build script tự tìm DLL Unity, không phụ thuộc tên thư mục `_Data`.

## Tooltip plugin 0.1.4

- Giữ nguyên định dạng JSON cũ:
  `{ "English title": { "title": "...", "info": "..." } }`.
- Tự reload JSON khi file thay đổi.
- F6 vẫn reload thủ công và áp dụng lại popup đang mở.
- Nếu xóa `TooltipText_EN.json`, plugin KHÔNG tạo lại file.
- Khi file bị xóa, bản dịch tooltip bị vô hiệu hóa.
- Build script chỉ cài file mẫu nếu chưa có file khi chạy installer.

## Cài đặt

Tắt game trước, sau đó:

```bash
cd "$HOME/Downloads"
rm -rf BD2VietnameseFixPack_v0.2.0
unzip -o BD2VietnameseFixPack_v0.2.0.zip
cd BD2VietnameseFixPack_v0.2.0
chmod +x install_all.sh diagnose.sh
./install_all.sh
```

Sau khi hoàn tất:

```text
Thoát game hoàn toàn
→ mở lại game
→ mở Oni +5
```

Mô tả `It's the Protection of the Oni! (Highest)` phải dùng key
`20038025`.

## Kiểm tra

```bash
./diagnose.sh
```

Log tốt sẽ có:

```text
Loading [BD2 Vietnamese Skill Text 0.2.0]
Loading [BD2 Vietnamese Tooltip 0.1.4]
Applied skill translation: key=20038025
```

Nếu một ID khác thiếu, log sẽ hiện:

```text
Missing skill translation: ID=..., field=..., table=...
```

## Tương thích file cũ

Không cần đổi cấu trúc file cũ.

Installer:

- sao lưu file hiện tại;
- chỉ bổ sung key còn thiếu từ bản cũ đã sửa;
- không ghi đè key đang tồn tại;
- giữ nguyên các bản dịch mới hơn có trên máy.
