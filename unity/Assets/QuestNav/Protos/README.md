# Generating Protobufs for C# Side

First, ensure you have the Protobuf Compiler installed.

Next, generate the protobufs
```bash
# Navigate to unity directory
cd QuestNav/unity

# Build Protobufs with protoc
protoc -I="../protos" --csharp_out=Assets/QuestNav/Protos/ "../protos/*.proto"
```