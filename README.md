# Brown Dust II Vietnamese

Dự án cộng đồng dịch tên và mô tả skill của Brown Dust II PC sang tiếng Việt.

# Synaeeeeeeeeee from BDX is 90% this project.

## Chọn nền tảng

- [Hướng dẫn Windows](README_WINDOWS.md)
- [Hướng dẫn Linux / Proton](README_LINUX.md)
- [Quy trình dịch skill](docs/TRANSLATION_WORKFLOW.md)
- [Hướng dẫn đóng góp](CONTRIBUTING.md)
- Link tải Bepinex: https://mega.nz/file/8v1xVLbR#p2gQyRnllcK96JA8hIGIFn94zXuML_CI_qTW1_QyQns

## Cấu trúc chính

```text
src/
  Translator/     Plugin hiển thị bản dịch
  Exporter/       Plugin lấy ID và text skill
  FontFix/        Plugin thử nghiệm sửa font tiếng Việt

translations/
  SkillTextTable_EN.json
  SkillTextTable_EN.raw.json

tools/
  Shortcuts/      Lệnh hỗ trợ quy trình dịch trên Linux
```

## Hai file bản dịch

```text
translations/SkillTextTable_EN.json
```

Bản đã duyệt, dùng để phát hành và để game đọc.

```text
translations/SkillTextTable_EN.raw.json
```

Bản làm việc hoặc bản chưa duyệt. Game không đọc file này.

## Hệ điều hành

```text
Linux / Proton: đã thử nghiệm
Windows: Chưa có chuột bạch
BrownDustX bắt buộc: không
```

## Lưu ý

Đây là dự án fan-made, không chính thức. Không đưa lên repository file game, DLL của game, font, save, log hoặc dữ liệu tài khoản. Khong phải có mod cheat ở đây.
