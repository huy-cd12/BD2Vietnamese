# Hướng dẫn đóng góp

## Quy tắc

- Không đổi ID.
- Không xóa token động hoặc thẻ màu.
- Không thay số động bằng số cố định.
- Giữ câu dịch ngắn để tránh tràn giao diện.
- Mỗi Pull Request nên tập trung vào một nhân vật hoặc một nhóm costume.
- Ghi rõ đã kiểm tra cấp skill nào: `+0` đến `+5`.
- Không commit file game, log, save, font hoặc thông tin tài khoản.

## Quy trình Git

```bash
git checkout -b translate/liberta
# sửa file
python3 -m json.tool translations/SkillTextTable_EN.raw.json >/dev/null
git add translations/
git commit -m "Translate Liberta skill levels"
git push -u origin translate/liberta
```

Sau đó mở Pull Request vào branch `main`.

## Khi cùng một ID có nhiều field

Có thể dùng:

```json
{
  "20038034": "Bản text",
  "20038034:nodeAddText": "Bản nodeAddText"
}
```
