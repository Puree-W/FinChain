# FinChain

An integrated LLM platform for analyzing financial and economic documents using Retrieval-Augmented Generation (RAG) techniques — both Simple RAG and Graph RAG — with a primary focus on financial news and economic content.

## Personal Goals
- I have been working with Vue 2 Options API for a while, but I am not yet comfortable with the Composition API, so I am trying to improve this skill to be compatible with other stacks in the future.
- I want to use this project to summarize what I am learning from work.

## Project Goals

FinChain aims to build a system that can:
- Connect LLMs with financial/economic document repositories
- Retrieve relevant information from documents using multiple RAG techniques
- Provide grounded responses backed by real source data
- Support relationship analysis across data through Graph RAG

## Tech Stack

**Frontend**
- Vue 3 (Composition API) + Vite
- Vuetify 3 (UI Framework)

**Backend**
- ASP.NET Core (C#)
- Supabase (Database / Storage)

**LLM**
- OpenThaiGPT (and other models via custom API)

## Project Structure

```
FinChain/
├── Controllers/        # ASP.NET API Controllers
├── Function/           # Business logic / LLM processing
├── Model/              # Data models
├── Repository/         # Data access layer (Supabase)
├── src/                # Frontend (Vue 3)
│   ├── api/            # API client
│   ├── components/     # Vue components
│   ├── composables/    # Shared reactive logic
│   └── page/           # Page-level views
└── package.json
```

## Installation & Running

### Prerequisites
- Node.js 18+
- .NET SDK 8.0+
- Supabase project (for database)

### Frontend

```bash
npm install
npm run dev
```

### Backend

```bash
dotnet restore
dotnet run
```

## Current Status

Currently developing **core LLM chat features**, which include:

- [x] Streaming chat response (SSE)
- [x] Conversation history persistence (topic & message logging)
- [x] Retrieve all conversation history
- [x] Rename topic
- [x] Delete topic
- [x] Multi-turn conversation context

## Roadmap

### Phase 1 — Core Chat (In Progress)
Build a stable, production-ready foundation for communicating with LLMs.

### Phase 2 — Custom LLM API & Configuration
- Connect to multiple LLM providers via a custom API layer
- Configurable prompt templates
- Adjustable base parameters (temperature, max_tokens, top_p, etc.)

### Phase 3 — RAG Management
- **Simple RAG** — embedding-based retrieval from financial/economic documents
- **Graph RAG** — build and query knowledge graphs from documents
- Document ingestion and indexing pipeline
