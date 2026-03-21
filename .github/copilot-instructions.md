# Hosepipe – Copilot Instructions

## Project Overview

**Hosepipe** is a .NET library for inspecting and retrying failed messages in RabbitMQ.

When a message fails processing, the messaging abstraction layer wraps it in an **error envelope** containing:
- The original message payload
- Error context (exception details, failure reason, timestamps, etc.)
- Metadata about the source queue the message originated from

Hosepipe provides HTTP endpoints that allow operators to:
- **Inspect errors** – browse error envelopes and their associated error context
- **Inspect messages** – view the original message payload inside an envelope
- **Retry messages** – republish the original message back to its source queue for reprocessing

## Architecture

- This is a **library** (not a standalone application). Consumers integrate it into their own ASP.NET Core host.
- Endpoints are exposed via ASP.NET Core Minimal APIs or controller-based routing, registered through extension methods (e.g. `IServiceCollection` and `IEndpointRouteBuilder`).
- The library is queue-broker agnostic at its core but has a RabbitMQ-specific implementation layer.
- **All interaction with error envelopes must go through an abstraction** (`IErrorEnvelopeReader` or similar). The core library never assumes a concrete envelope schema — it only works against the interface. This allows consumers to provide their own implementation that matches their messaging library's envelope format.

## Key Concepts

| Term | Meaning |
|---|---|
| **Error envelope** | The wrapper object produced by the messaging abstraction when a message fails; contains the original message and error context |
| **Dead letter queue** | The RabbitMQ queue where failed/rejected messages land |
| **Retry** | Republishing the original message payload back to its origin queue so it can be processed again |
| **Error context** | Metadata attached by the messaging layer describing why the message failed |

## Envelope Abstraction

Different messaging libraries produce different envelope schemas. Hosepipe must never hardcode knowledge of any specific schema.

**Pattern:**
- Define an `IErrorEnvelopeReader<TEnvelope>` interface in the core library. It is responsible for extracting well-known pieces of information (original message body, source queue, error reason, etc.) from a raw envelope of type `TEnvelope`.
- All internal Hosepipe logic that needs data from an envelope must call through this interface — never access envelope fields directly.
- The `Hosepipe.RabbitMQ` project provides a default implementation for envelopes produced by the RabbitMQ messaging abstraction it targets.
- Consumers using a different messaging library register their own `IErrorEnvelopeReader<T>` implementation via the DI extension methods.

**What the interface must expose (at minimum):**
- Original message payload (raw bytes or string)
- Source queue / exchange the message came from
- Error reason / exception message
- Any additional error context properties (as a dictionary is acceptable)

**Never:**
- Deserialize directly into a concrete envelope type inside core library code
- Couple endpoint logic to a specific envelope implementation

## Coding Conventions

- Target **.NET 10** and use C# 14 language features where appropriate.
- Use **async/await** throughout — no blocking calls.
- Prefer **extension methods** for all public integration surface (e.g. `AddHosepipe(...)`, `MapHosepipeEndpoints(...)`).
- Keep abstractions thin — define interfaces for broker communication so RabbitMQ can be swapped or mocked in tests.
- Use **`Result<T>`-style returns** or explicit exception types rather than swallowing errors silently.
- All public API surface should be **XML-documented**.
- Follow the existing naming convention: the project/namespace root is `Hosepipe`.
- Always make namespaces match the folder structure
- Prefer returning immutable data structures (e.g. `IReadOnlyDictionary<string, string>` ) to prevent accidental modification.

## Tests
- Unit tests should cover all core library logic, especially the envelope reader abstraction and endpoint handlers.
- Use XUnitV3 with the new Microsoft.Testing.Platform for test projects.
- Prefer stubs over mocks for dependencies in unit tests. For example, create a simple `StubErrorEnvelopeReader` that returns hardcoded values for testing endpoint logic.
- Prefer integration tests using an actual RabbitMQ instance (e.g. via Testcontainers) to verify the RabbitMQ adapter works correctly with real envelopes and queues.

## Project Structure (intended)

```
Hosepipe/
  src/
    Hosepipe/                  # Core library (abstractions + endpoint registration)
    Hosepipe.RabbitMQ/         # RabbitMQ-specific implementation
  tests/
    Hosepipe.Tests/            # Unit tests
    Hosepipe.RabbitMQ.Tests/   # Integration tests for the RabbitMQ adapter
```
