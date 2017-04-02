## Commands
### Generating C# grpc source code
- `C:/Users/Administrator/Documents/GitHub/grpc-csharp/Grpc/packages/Grpc.Tools.1.0.1/tools/windows_x64/protoc -I ./pb --csharp_out ./Grpc/Messages ./pb/messages.proto --grpc_out ./Grpc/Messages --plugin=protoc-gen-grpc=./Grpc/packages/Grpc.Tools.1.0.1/tools/windows_x64/grpc_csharp_plugin.exe`