## Commands
### Generating C# grpc source code
- `protoc -I ./pb --csharp_out ./Grpc/Messages ./pb/messages.proto --grpc_out ./Grpc/Messages --plugin=protoc-gen-grpc=./Grpc/packages/Grpc.Tools.1.2.0/tools/windows_x64/grpc_csharp_plugin.exe`