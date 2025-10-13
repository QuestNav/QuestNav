# API Reference

> **For Contributors**  
> This API reference is intended for developers contributing to the QuestNav project. If you're an FRC team looking to use QuestNav, please refer to the [Quick Start Guide](/docs/getting-started/quick-start) instead.

Welcome to the QuestNav API Reference documentation. This section provides comprehensive documentation for all QuestNav APIs and interfaces.

## Available Documentation

### 📋 Protocol Buffers
**[View Protocol Buffer Documentation →](/api/proto/)**

Complete documentation for all QuestNav protocol buffer definitions, including:
- **Command System**: Messages for sending commands to the Quest device
- **Data Structures**: Frame data, device status, and tracking information  
- **2D Geometry**: WPILib-compatible 2D geometric primitives
- **3D Geometry**: Advanced 3D geometric representations with quaternions

The protocol buffers define the communication interface between robot code and the Quest headset.

### ☕ Java API
**[View Java API Documentation →](/api/java/)**

Javadoc documentation for the QuestNav Java library, including:
- **QuestNav Class**: Main interface for communicating with Quest devices
- **PoseFrame**: Data structure for Quest pose information
- **Protocol Buffer Integration**: WPILib-compatible protobuf wrappers
- **Network Communication**: NetworkTables integration for robot communication

Use this library to integrate QuestNav into your FRC robot code.

### 🔷 C# API  
**[View C# API Documentation →](/api/csharp/)**

DocFX documentation for the Unity C# components, including:
- **Core Systems**: Quest tracking and pose estimation
- **UI Components**: User interface and visualization systems
- **Network Integration**: Communication with robot systems
- **Native Interfaces**: Low-level Quest device integration

This documentation covers the Unity application that runs on the Quest headset.

## Documentation Generation

All API documentation is automatically generated from source code:
- **Protocol Buffers**: Generated using `protoc-gen-doc` from `.proto` files with detailed comments
- **Java API**: Generated using Gradle Javadoc from Java source with comprehensive documentation
- **C# API**: Generated using DocFX from C# source with XML documentation comments

The documentation is kept up-to-date with each release and reflects the current state of the APIs.
