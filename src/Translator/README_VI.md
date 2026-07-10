# BD2 Vietnamese Skill Text

## Bản 0.1.1

Bổ sung tham chiếu `UnityEngine.InputLegacyModule.dll` để sửa lỗi build:

```text
CS0103: The name 'Input' does not exist in the current context
```


Plugin BepInEx thử nghiệm để thay nội dung `SkillTextTable_EN` bằng tiếng Việt.

## Cách cài

1. Tắt Brown Dust II.
2. Chạy:

```bash
cd "$HOME/Downloads/BD2VietnameseSkillText"
chmod +x build.sh
./build.sh
```

3. Mở game qua Steam.
4. Vào trang kỹ năng của nhân vật.

File dịch được cài tại:

```text
BepInEx/config/BD2Vietnamese/SkillTextTable_EN.json
```

Sau khi sửa JSON trong lúc game đang chạy, nhấn `F6` để nạp lại.

## Định dạng khóa

Khóa thông thường:

```json
{
  "20038015": "Nội dung dịch"
}
```

Khi cùng một ID có nhiều cột, có thể chỉ định theo cột:

```json
{
  "20038034:text": "Bản thường",
  "20038034:nodeAddText": "Bản có dấu |"
}
```

Khóa `ID:cột` được ưu tiên trước khóa chỉ có `ID`.

## Lưu ý

Giữ nguyên các token như:

```text
<#ffa024>
</color>
[3[
<6<
{6{
|
```

Plugin chưa được kiểm thử trên mọi phiên bản game. Nếu log hiện `Patched methods: 0`,
cần gửi `BepInEx/LogOutput.log`.
