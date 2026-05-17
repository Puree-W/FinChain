# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project Overview

**FinChain** — an integrated LLM platform for analyzing financial and economic documents using Retrieval-Augmented Generation (RAG), both Simple RAG and Graph RAG, focused on financial news and economic content.

### Core Functions

1. **LLM Chat (Phase 1 — in progress)**
   - Streaming chat responses via Server-Sent Events (SSE)
   - Conversation history persistence (topics + messages)
   - Retrieve full conversation history
   - Rename topic (implemented), delete topic (in progress)
   - Multi-turn conversation context (planned)

2. **Custom LLM API & Configuration (Phase 2 — planned)**
   - Connect multiple LLM providers through a unified custom API layer
   - Configurable prompt templates
   - Adjustable base parameters (temperature, max_tokens, top_p, etc.)

3. **RAG Management (Phase 3 — planned)**
   - **Simple RAG** — embedding-based retrieval over financial/economic documents
   - **Graph RAG** — build and query knowledge graphs from documents
   - Document ingestion and indexing pipeline

### Tech Stack

- **Frontend:** Vue 3 (Composition API) + Vite, Vuetify 3
- **Backend:** ASP.NET Core (C#)
- **Storage:** Supabase
- **LLM:** OpenThaiGPT and other models via custom API

### Project Structure

```
FinChain/
├── Controllers/   # ASP.NET API Controllers
├── Function/      # Business logic / LLM processing
├── Model/         # Data models
├── Repository/    # Data access layer (Supabase)
└── src/           # Frontend (Vue 3)
    ├── api/         # API client
    ├── components/  # Vue components
    ├── composables/ # Shared reactive logic
    └── page/        # Page-level views
```

## Color Theme

All UI work must use the palette defined in `src/style.css`. Do not introduce new colors — reuse these tokens.

### Dark mode (default — `:root`)

| Token              | Value                      | Usage                          |
| ------------------ | -------------------------- | ------------------------------ |
| Text (primary)     | `rgba(255, 255, 255, 0.87)`| Body text on dark background   |
| Background         | `#222222`                  | App background                 |
| Surface (button)   | `#1a1a1a`                  | Button / elevated surface      |
| Accent (link)      | `#646cff`                  | Links, focus, hover border     |
| Accent (link hover)| `#535bf2`                  | Link hover state               |

### Light mode (`@media (prefers-color-scheme: light)`)

| Token              | Value      | Usage                        |
| ------------------ | ---------- | ---------------------------- |
| Text (primary)     | `#213547`  | Body text on light background|
| Background         | `#ffffff`  | App background               |
| Surface (button)   | `#f9f9f9`  | Button / elevated surface    |
| Accent (link hover)| `#747bff`  | Link hover state             |

### Typography

- Font stack: `'Noto Sans Thai', system-ui, Avenir, Helvetica, Arial, sans-serif`
- Applied globally in `body` (excluding Material Design icons: `.mdi`, `.mdi-set`, `.v-icon`)
- Base weight: `400`, line height: `1.5`

### Rules

- When styling new components, pull values from `src/style.css` rather than hard-coding new hex codes.
- The accent `#646cff` is the brand/interaction color — use it for primary CTAs, active states, and focus rings.
- Respect `color-scheme: light dark` — components should work in both modes.
- Preserve the Material Design icon font exception when extending the global font rule.

## Development

```bash
# Frontend
npm install
npm run dev

# Backend
dotnet restore
dotnet run
```

### Conventions

- Frontend uses Vue 3 **Composition API** (`<script setup>`), not Options API.
- Message roles use `user` / `assistant` (standardized — do not reintroduce `U` / `B`).
- API responses follow the `ApiReturnModel<T>` envelope on the backend.
- HTTP verbs in `src/api` client must match the operation (e.g., `apiPut` for PUT).
