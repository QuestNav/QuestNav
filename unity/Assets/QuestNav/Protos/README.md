# Generating Protobufs for C# Side

First, ensure you have the Protobuf Compiler installed.

Next, generate the protobufs
```bash
# Navigate to repo base directory

# Build Protobufs with protoc
protoc --proto_path=protos --csharp_out=unity/Assets/QuestNav/Protos/Generated protos/data.proto protos/commands.proto protos/wpimath/protobuf/geometry3d.proto
```

## [See docs for more info](https://questnav.gg/docs/development/development-setup#step-7-build-protobufs)