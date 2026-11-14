#!/usr/bin/env bash
# Builds the native shared library into native/out/.
# Linux x86_64 (gcc) -> libinventory_native.so ; macOS (cc) -> libinventory_native.dylib
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p out
case "$(uname -s)" in
  Darwin) cc -std=c11 -O2 -Wall -Wextra -fPIC -dynamiclib -o out/libinventory_native.dylib inventory_native.c ;;
  *)      gcc -std=c11 -O2 -Wall -Wextra -fPIC -shared   -o out/libinventory_native.so    inventory_native.c ;;
esac
echo "native build OK -> $(ls out)"