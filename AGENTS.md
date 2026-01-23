# CyanTooth Engineer Guide

This document provides essential context, commands, and guidelines for AI agents and developers working on the CyanTooth codebase.

## 1. Environment & Build

- **Framework**: .NET 8.0 (Desktop)
- **UI Framework**: WPF with [WPF-UI](https://github.com/lepoco/wpfui)
- **Architecture**: MVVM (CommunityToolkit.Mvvm), 3-Layer (UI -> Core -> Platform)

### Commands

Run these commands from the solution root (`D:\Github\CyanTooth`).

| Action | Command |
|--------|---------|
| **Restore** | `dotnet restore CyanTooth.sln` |
| **Build** | `dotnet build CyanTooth.sln --configuration Debug --no-restore` |
| **Test (All)** | `dotnet test --configuration Debug --no-build` |
| **Test (Single)**| `dotnet test --filter "FullyQualifiedName=CyanTooth.Tests.BluetoothDeviceTests.MacAddress_FormatsCorrectly"` |
| **Run App** | `dotnet run --project src/CyanTooth/CyanTooth.csproj` |

> **Note**: Always use `Debug` configuration for development.

## 2. Project Structure

- `src/CyanTooth` (UI): WPF views, ViewModels, Converters, Resources.
- `src/CyanTooth.Core` (Business): Models, Services, Event definitions. **No UI dependencies.**
- `src/CyanTooth.Platform` (Infra): Native P/Invoke, Bluetooth APIs, Audio/Battery logic.
- `tests/CyanTooth.Tests`: xUnit tests for Core and logic.

## 3. Code Style & Conventions

### General
- **Namespaces**: Use file-scoped namespaces (`namespace CyanTooth.Core.Services;`).
- **Nullability**: Nullable reference types are **enabled**. Use `?` explicitly.
- **Async**: Async methods MUST end with `Async` suffix and return `Task` or `Task<T>`.
- **Formatting**: Standard C# conventions (K&R braces, 4-space indentation).

### Naming
- **Private Fields**: `_camelCase` (e.g., `_bluetoothService`).
- **Properties/Methods**: `PascalCase`.
- **Event Handlers**: `On[EventName]` (e.g., `OnDeviceAdded`).

### MVVM (CommunityToolkit)
- Inherit ViewModels from `ObservableObject`.
- Use `[ObservableProperty]` for backing fields (generates public property).
- Use `[RelayCommand]` for command methods.
- **Threading**: UI updates must happen on the Dispatcher:
  ```csharp
  App.Current.Dispatcher.Invoke(() => { ... });
  ```

### Logging
- Use `DebugLogger.Log($" Message")` (from `CyanTooth.Platform.Helpers`) for internal tracing.

### Error Handling
- Do not suppress exceptions silently unless expected.
- Log exceptions via `DebugLogger`.

## 4. Testing Guidelines

- **Framework**: xUnit.
- **Pattern**: Arrange / Act / Assert.
- **Naming**: `MethodName_Condition_Result` (e.g., `MacAddress_ZeroAddress_ReturnsEmpty`).
- **Scope**: Focus on unit testing `Core` logic. UI logic (ViewModels) can be tested if dependencies are mocked.

## 5. Agent Behavior Rules

1.  **Dependency Injection**: Always use Constructor Injection for services.
2.  **Platform Isolation**: Keep Windows API calls (P/Invoke) strictly in `CyanTooth.Platform`.
3.  **UI Thread Safety**: Be extremely careful with events from background threads (Bluetooth/Audio APIs) updating the UI. Always Marshal to Dispatcher.
4.  **No "Magic" Strings**: Use constants or `nameof()` where possible.
5.  **Git Commit Language**: All git commit messages MUST be written in **Chinese**.
6.  **Release Verification**: NEVER push a new version tag (triggering a release) without verifying the build locally. Ensure the codebase is stable before publishing.

## 6. Implementation Workflow (Simulated)

When asked to implement a feature:
1.  **Explore**: Understand relevant services in `Core` and `Platform`.
2.  **Plan**: Update `Core` interfaces first, then `Platform` implementation, then `UI`.
3.  **Verify**: Run `dotnet build` and relevant tests.
