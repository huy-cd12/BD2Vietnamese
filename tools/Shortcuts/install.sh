#!/usr/bin/env bash
set -euo pipefail

SOURCE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="$HOME/.local/bin"
BASHRC="$HOME/.bashrc"

mkdir -p "$BIN_DIR"
install -m 0755 "$SOURCE_DIR/bd2" "$BIN_DIR/bd2"

if ! grep -Fq 'export PATH="$HOME/.local/bin:$PATH"' "$BASHRC" 2>/dev/null; then
    {
        echo
        echo '# BD2 helper commands'
        echo 'export PATH="$HOME/.local/bin:$PATH"'
    } >> "$BASHRC"
fi

sed -i \
    -e '/^alias bd2clear=/d' \
    -e '/^alias bd2pull=/d' \
    -e '/^alias bd2raw=/d' \
    -e '/^alias bd2active=/d' \
    -e '/^alias bd2editraw=/d' \
    -e '/^alias bd2edit=/d' \
    -e '/^alias bd2check=/d' \
    -e '/^alias bd2push=/d' \
    -e '/^alias bd2status=/d' \
    -e '/^alias bd2backup=/d' \
    -e '/^alias bd2path=/d' \
    -e '/^alias bd2open=/d' \
    -e '/^alias bd2exports=/d' \
    -e '/^alias bd2log=/d' \
    "$BASHRC"

cat >> "$BASHRC" <<'EOF'

# BD2 shortcut aliases v1.3
alias bd2clear="bd2 clear"
alias bd2pull="bd2 pull"
alias bd2raw="bd2 raw"
alias bd2active="bd2 active"
alias bd2editraw="bd2 editraw"
alias bd2edit="bd2 edit"
alias bd2check="bd2 check"
alias bd2push="bd2 push"
alias bd2status="bd2 status"
alias bd2backup="bd2 backup"
alias bd2path="bd2 path"
alias bd2open="bd2 open"
alias bd2exports="bd2 exports"
alias bd2log="bd2 log"
EOF

echo "Đã cài BD2 Shortcuts v1.3"
echo 'Chạy: source "$HOME/.bashrc"'
echo "Sau đó: bd2 help"
