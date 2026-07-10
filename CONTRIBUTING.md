# Hướng dẫn đóng góp

## Người dịch trên Windows

1. Fork repository.
2. Clone bằng GitHub Desktop.
3. Tạo branch riêng.
4. Sửa `translations\SkillTextTable_EN.raw.json`.
5. Kiểm tra JSON bằng PowerShell.
6. Commit, push và mở Pull Request.

## Người dịch trên Linux

1. Fork hoặc clone repository.
2. Tạo branch riêng.
3. Dùng exporter và shortcut để lấy ID.
4. Sửa `translations/SkillTextTable_EN.raw.json`.
5. Kiểm tra JSON.
6. Commit, push và mở Pull Request.

## Quy tắc chung

- Không đổi ID.
- Không xóa token động hoặc thẻ màu.
- Không thay số động bằng số cố định.
- Không commit DLL hoặc tài sản của game.
- Không commit font, save, log hoặc dữ liệu tài khoản.
- Mỗi Pull Request nên tập trung vào một nhân vật hoặc một nhóm costume.
- Ghi rõ cấp đã kiểm tra: `+0` đến `+5`.
- Ghi rõ đã thử trong game hay chưa.

## Commit mẫu

```bash
git checkout -b translate/liberta
git add translations/
git commit -m "Translate Liberta skill levels"
git push -u origin translate/liberta
```
