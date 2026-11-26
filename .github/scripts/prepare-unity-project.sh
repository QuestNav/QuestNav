#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "$0")/../.." && pwd)

printf 'Generating C# protobufs\n'
(
  cd "$repo"
  mkdir -p unity/Assets/QuestNav/Generated
  shopt -s globstar
  protoc --proto_path=protos --csharp_out=unity/Assets/QuestNav/Generated protos/**/*.proto
  find unity/Assets/QuestNav/Generated -maxdepth 1 -name '*.cs' -printf '%f\n' | sort
)

printf 'Building the web UI\n'
(
  cd "$repo/questnav-web-ui"
  npm ci --no-audit --no-fund
  npm run build
)
test -s "$repo/unity/Assets/StreamingAssets/ui/index.html"

printf 'Restoring Unity NuGet packages\n'
nugetforunity restore "$repo/unity" 2>&1 | tee "${1:?Expected NuGet log path}"
