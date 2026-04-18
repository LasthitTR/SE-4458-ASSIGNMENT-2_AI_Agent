# SE4458 Assignment 2 - Airbnb Agent Chat

## Student Information

Group 2 (Airbnb/Listing) - Emre Akar.

## Project Overview

This project is an AI Agent Chat application that interprets natural language requests and routes them to Midterm Airbnb APIs through MCP.

The backend uses LangChain with a local Ollama model for reasoning and tool calling. MCP exposes `query_listings` and `book_stay` tools through an MCP Server. The frontend provides a chat UI that sends message text, `sessionId`, and an optional JWT token.

## Architecture & Flow

Agent -> MCP Client -> MCP Server -> API Gateway -> Midterm APIs

- Agent: LangChain `ChatOllama` processes user intent and tool calls.
- MCP Client: `@modelcontextprotocol/sdk/client` dynamically fetches and invokes tools.
- MCP Server: Exposes `query_listings` and `book_stay`.
- API Gateway: All HTTP calls go to `https://emre-gateway-vize.azurewebsites.net`.
- Midterm APIs: Listing and booking business logic is executed there.

## Design Decisions & Assumptions

- MCP Server and MCP Client communicate in the same Node.js backend process via `StdioClientTransport`.
- Tool metadata is fetched dynamically at runtime (not hardcoded).
- Booking requires a guest JWT token; frontend sends it as `authToken`.
- Session-based memory is maintained using `sessionId` sent from frontend.
- LLM is fully local: Ollama model (`llama3.1` by default). No cloud LLM deployment is required.

## Issues Encountered

- LangChain dependency/version conflicts were resolved by replacing OpenAI integration with Ollama integration.
- Runtime 500s were fixed by correcting MCP tool binding behavior and improving backend fallback handling.
- UI accessibility contrast warnings were resolved by style adjustments.

## Integration Test Evidence

File: `test-integration.js`

Run:

```bash
cd AirbnbAgent
node test-integration.js
```

Expected: `HTTP Status: 200`

If Ollama is not running on `http://localhost:11434`, backend returns a graceful assistant message explaining the local model connection issue.

## Video Presentation

[Insert Video Link Here]

## How to Run

### Prerequisites

- Node.js 18+
- npm
- Ollama installed locally and running in background
- Pulled local model (recommended): `llama3.1`

Example:

```bash
ollama pull llama3.1
ollama serve
```

### 1) Backend Setup

```bash
cd AirbnbAgent/backend
npm install
copy .env.example .env
npm run dev
```

`backend/.env`:

```env
PORT=4000
OLLAMA_BASE_URL=http://localhost:11434
OLLAMA_MODEL=llama3.1
GATEWAY_BASE_URL=https://emre-gateway-vize.azurewebsites.net
DEFAULT_CLIENT_ID=airbnb-agent
```

### 2) Frontend Setup (second terminal)

```bash
cd AirbnbAgent/frontend
npm install
npm run dev
```

Frontend runs at `http://localhost:5173`.

### 3) Optional Integration Test (third terminal)

```bash
cd AirbnbAgent
node test-integration.js
```

This project runs fully local for LLM inference with Ollama and does not require cloud LLM deployment.
