#!/usr/bin/env bash
#
# MenuCraft — setup.sh / deploy.sh で共有するヘルパー
#
# 実行せず source して使うこと:
#   source "$(dirname "${BASH_SOURCE[0]}")/common.sh"
#
# .NET のバージョンをスクリプトにハードコードせず、常に csproj と
# 発行成果物から導出するためのユーティリティを提供する。

# csproj の TargetFramework から必要な .NET バージョンを取得する (例: 8.0)。
menucraft_target_dotnet_version() {
  local csproj="$1"
  [[ -f "${csproj}" ]] || return 1
  sed -n 's|.*<TargetFramework>net\([0-9][0-9]*\.[0-9][0-9]*\)</TargetFramework>.*|\1|p' \
    "${csproj}" | head -1
}

# 発行成果物の runtimeconfig.json が要求する .NET バージョンを取得する (例: 8.0)。
# ビルドに使った SDK が新しくても、実行にはこのバージョンのランタイムが要る。
menucraft_required_runtime_version() {
  local runtimeconfig="$1"
  [[ -f "${runtimeconfig}" ]] || return 1
  sed -n 's|.*"version"[[:space:]]*:[[:space:]]*"\([0-9][0-9]*\.[0-9][0-9]*\)\.[0-9].*|\1|p' \
    "${runtimeconfig}" | head -1
}

# 指定バージョンの Microsoft.NETCore.App ランタイムが導入済みか判定する。
menucraft_has_dotnet_runtime() {
  local dotnet_bin="$1" version="$2"
  [[ -x "${dotnet_bin}" && -n "${version}" ]] || return 1
  "${dotnet_bin}" --list-runtimes 2>/dev/null \
    | grep -q "^Microsoft\.NETCore\.App ${version//./\\.}\."
}

# .NET SDK が 1 つ以上導入済みか判定する（ビルドに必要）。
menucraft_has_dotnet_sdk() {
  local dotnet_bin="$1"
  [[ -x "${dotnet_bin}" ]] || return 1
  [[ -n "$("${dotnet_bin}" --list-sdks 2>/dev/null)" ]]
}

# ランタイム不足時に表示する復旧手順。
menucraft_runtime_install_hint() {
  local version="$1" install_dir="$2"
  printf 'curl -fsSL https://dot.net/v1/dotnet-install.sh | sudo bash -s -- --channel %s --runtime dotnet --install-dir %s' \
    "${version}" "${install_dir}"
}
