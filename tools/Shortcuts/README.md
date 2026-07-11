# BD2 Shortcuts v1.5

Bản này thêm hai quy trình tự động, không yêu cầu chạy `bd2check`.

## Cài đặt

```bash
cd "$HOME/Downloads"
rm -rf BD2Shortcuts_v1.5
unzip -o BD2Shortcuts_v1.5.zip
cd BD2Shortcuts_v1.5
chmod +x install.sh
./install.sh
source "$HOME/.bashrc"
```

## Skill tự động

Trong game:

```text
F11 → F9 → mở skill +0 đến +5 → F9
```

Trong Terminal:

```bash
bd2skill
```

Lệnh tự chạy:

```text
pull
→ đọc RAW trước khi edit
→ mở Nano
→ đọc RAW sau khi edit
→ push vào game
```

Khi phần đọc mở bằng `less`, nhấn:

```text
q
```

để sang bước kế tiếp.

Sau cùng quay lại game:

```text
F6 → đóng và mở lại cửa sổ skill
```

## Tooltip tự động

Trong game:

```text
mở popup → F7 một lần
```

Trong Terminal:

```bash
bd2tooltip
```

Lệnh tự chạy:

```text
pull tooltip
→ đọc RAW trước khi edit
→ mở Nano
→ đọc RAW sau khi edit
→ push vào game
```

Sau cùng quay lại game:

```text
F6 → đóng và mở lại popup
```

## Không có bước check riêng

Hai lệnh `bd2skill` và `bd2tooltip` không gọi `bd2check`.

Các lệnh thủ công cũ vẫn giữ nguyên:

```text
bd2pull / bd2editraw / bd2push
bd2tpull / bd2tedit / bd2tpush
```
