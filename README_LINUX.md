# Hướng dẫn Linux / Proton

## 1. Cấu hình đã thử nghiệm

Dự án được phát triển trên Pop!_OS với Brown Dust II chạy qua Steam/Proton.

Launch Options đã dùng:

```bash
WINEDLLOVERRIDES="winhttp.dll=n,b" %command% "browndust2:games/10000001?usn=0"
```

## 2. Đường dẫn game mẫu

```bash
GAME="$HOME/.steam/debian-installation/steamapps/compatdata/3667336694/pfx/drive_c/Neowiz/Browndust2/Browndust2_10000001"
```

Có thể đặt biến này trong Terminal trước khi build.

## 3. Build plugin

### Translator

```bash
cd src/Translator
chmod +x build.sh
./build.sh
```

### Exporter

```bash
cd src/Exporter
chmod +x build.sh
./build.sh
```

### FontFix

```bash
cd src/FontFix
chmod +x build.sh
./build.sh
```

## 4. Quy trình dịch bằng shortcut

```text
bd2clear
→ trong game F11
→ F9
→ mở skill +0 đến +5
→ F9
→ bd2pull
→ bd2editraw
→ bd2check
→ bd2push
→ trong game F6
```

Chi tiết xem:

```text
docs/TRANSLATION_WORKFLOW.md
```

## 5. Hai file song song

Game đọc:

```text
BepInEx/config/BD2Vietnamese/SkillTextTable_EN.json
```

Game không đọc:

```text
BepInEx/config/BD2Vietnamese/SkillTextTable_EN.raw.json
```

## 6. Kiểm tra log

```bash
grep -iE \
"BD2 Vietnamese Skill Text|BD2 Skill Text Exporter|Loaded.*Vietnamese|Patched methods|error|exception" \
"$GAME/BepInEx/LogOutput.log" |
tail -n 120
```
