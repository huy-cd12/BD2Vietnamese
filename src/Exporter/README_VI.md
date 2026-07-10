# BD2 Skill Text Exporter

## Sửa lỗi build trên BepInEx 5

Bản v0.1.1 bổ sung tham chiếu `UnityEngine.dll` để sửa lỗi:

```text
CS0012: The type 'MonoBehaviour' is defined in an assembly that is not referenced.
```


Plugin BepInEx 5 dành cho BrownDust II bản PC.

## Mục tiêu

Plugin **không sửa database, catalog hoặc bundle của game**. Nó tìm các hàm có khả năng dùng để tra cứu ngôn ngữ trong `Assembly-CSharp` và BrownDustX, sau đó ghi lại:

- tên table suy đoán;
- ID/key suy đoán;
- nội dung gốc mà game trả về;
- hàm nguồn;
- số lần xuất hiện.

Bản đầu tiên là công cụ thu thập/kiểm chứng. Vì tên lớp hoặc hàm của game có thể bị obfuscate, không bảo đảm bắt đúng ngay trong lần đầu.

## Build và cài

Mở Terminal trong thư mục dự án:

```bash
chmod +x build.sh
./build.sh
```

Script mặc định dùng đường dẫn game:

```text
~/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001
```

Nếu game nằm chỗ khác:

```bash
GAME="/đường/dẫn/thư/mục/game" ./build.sh
```

Yêu cầu: `dotnet --version` phải chạy được.

## Cách thu thập riêng skill nhân vật

1. Mở game và vào màn hình nhân vật.
2. Nhấn **F9** để bật capture.
3. Mở lần lượt:
   - tên skill;
   - mô tả skill;
   - costume skill;
   - tooltip hiệu ứng/buff liên quan;
   - cấp nâng skill nếu cần.
4. Không mở story hoặc menu khác trong lúc capture.
5. Nhấn **F9** lần nữa để dừng. Plugin tự export.
6. **F10** export thủ công.
7. **F11** xóa dữ liệu trong RAM trước một lượt test mới.

## Vị trí kết quả

```text
BepInEx/exports/BD2SkillText/
```

Mỗi lượt export có dạng:

```text
skill_capture_YYYYMMDD_HHMMSS/
├── all_entries.json
├── collisions.json
├── unknown_id_entries.json
├── README.txt
└── tables/
    ├── SomeTable.json
    └── AnotherTable.json
```

`tables/*.json` là file ứng viên theo dạng:

```json
{
  "10001": "Original skill text",
  "10002": "Another skill text"
}
```

## Cần kiểm tra trước khi dịch

- `table` chỉ là tên suy đoán từ tham số hoặc class.
- `<unknown>` nghĩa là plugin bắt được text nhưng chưa xác định được ID thật.
- `collisions.json` nghĩa là cùng table + ID trả về nhiều text khác nhau.
- Chưa dùng trực tiếp các file này làm đầu vào BrownDustX cho đến khi xác minh tên table/file.

## Nếu không bắt được dữ liệu

Gửi lại hai file:

```text
BepInEx/exports/BD2SkillText/patched_methods.txt
BepInEx/LogOutput.log
```

và một lượt export trống hoặc `unknown_id_entries.json`. Từ đó có thể sửa phiên bản 0.2 để hook đúng hàm thực tế của game.


## Thay đổi trong v0.2.0

- Nhận diện các giá trị database như `DB_COMMON` là **table**, không còn dùng nhầm làm ID.
- Ưu tiên tham số số làm ID khi tên tham số đã bị obfuscate.
- Ghi thêm `arguments` và `idSource` trong `all_entries.json` để chẩn đoán ID thật.
- Gộp các wrapper obfuscate khi chúng trả cùng `table + ID + text`.
- Không gọi Harmony unpatch khi game đóng, tránh cảnh báo `IL Compile Error`.

Sau khi cài v0.2.0, cần tạo một lượt capture mới. Không dùng các file JSON của v0.1.0 làm bản dịch vì `DB_COMMON` trong kết quả cũ là tên database, không phải ID nội dung.
