# Brown Dust II Vietnamese Translation

Dự án cộng đồng dịch tên và mô tả skill của Brown Dust II PC sang tiếng Việt.

# Synaeeeeeee from BDX is 90% of this project.

## Thành phần

```text
src/Translator/       Plugin hiển thị bản dịch trong game
src/Exporter/         Plugin lấy ID và text skill
src/FontFix/          Plugin thử nghiệm sửa font tiếng Việt
tools/Shortcuts/      Bộ lệnh hỗ trợ quy trình dịch
translations/
  SkillTextTable_EN.json       Bản đã duyệt, dùng để phát hành
  SkillTextTable_EN.raw.json   Bản làm việc/chưa duyệt
```

## Quy trình đóng góp

1. Fork repository.
2. Tạo branch riêng.
3. Chỉnh `translations/SkillTextTable_EN.raw.json`.
4. Giữ nguyên ID, token động và thẻ màu.
5. Kiểm tra JSON.
6. Mở Pull Request.

```bash
python3 -m json.tool translations/SkillTextTable_EN.raw.json >/dev/null
```

Sau khi bản dịch được kiểm tra trong game, nội dung được gộp vào:

```text
translations/SkillTextTable_EN.json
```

## Token không được sửa

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

## Tuyên bố

Đây là dự án fan-made, không chính thức. Tên game, nội dung gốc và tài sản liên quan thuộc về chủ sở hữu tương ứng. Không tải lên repository các DLL, font, hình ảnh, save hoặc tài sản lấy từ game. Đây cũng không phải là công cụ để gian lận game cũng như tài nguyên game.

