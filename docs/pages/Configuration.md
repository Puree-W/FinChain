# Configuration page

The **Configuration** page lets the user manage how FinChain talks to LLM
providers and the chat-side templates that control base parameters
(temperature, max tokens, system prompt, etc.).

It is reachable from the gear icon at the bottom of the navbar, or directly
at `/configuration`.

## Layout

```
/configuration            → redirects to /configuration/llm
/configuration/llm        → LLM Configuration panel
/configuration/model      → Model Setting panel
```

The page renders a left rail with the two sub-pages and a content pane on
the right. The navbar is preserved so the user can return to chat history
at any time. The gear icon highlights while any `/configuration/*` route
is active; clicking it again toggles back to `/`.

## Sub-pages

### LLM Configuration (`/configuration/llm`)

CRUD over the `ai_config` and `embedding_config` tables. The page has two
tabs:

- **LLM endpoints** — endpoints the chat path uses for completions.
- **Embedding endpoints** — endpoints the future RAG ingestion pipeline
  will use (Phase 3).

Each row shows the name, endpoint URL, masked API key, auth header name,
and an active toggle. Soft-delete only — disabling a row sets
`active_flag = false`; templates that reference a disabled endpoint will
fail with a clear error from the chat path.

### Model Setting (`/configuration/model`)

CRUD over the `model_template` table. Each template is:

- **A name + description** — what the user sees in the chat dropdown.
- **An `ai_config_id`** — which LLM endpoint to call.
- **Base params** — `temperature`, `max_tokens`, `top_p`,
  `frequency_penalty`, `presence_penalty`, `system_prompt`.
- **`is_default`** — exactly one template is the chat default at any time;
  saving a template with `is_default = true` clears the flag on all
  others.

The first template the user creates is auto-flagged as the default so the
chat page always has something to send.

## Data flow

```
chat send →
  useChat.sendMessage(text)
    └─ POST /api/Chat/ChatPost
          { messages, templateId, topicId }
        └─ ChatProcessor.StreamChatMessageAsync
              ├─ resolve template (by id, or default)
              ├─ load template.ai_config
              ├─ build body = ai_config.json_request
                              + { messages, temperature, max_tokens,
                                  top_p, frequency_penalty,
                                  presence_penalty, stream,
                                  stream_options.include_usage }
              ├─ optionally prepend system prompt
              ├─ POST → ai_config.endpoint
                       with header { ai_config.auth_header_name:
                                     ai_config.api_key }
              └─ stream chunks back to the client (SSE,
                 `[DONE]` sentinel, final usage block recorded
                 in log_message)
```

## Schema

```sql
ALTER TABLE public.ai_config        ADD COLUMN api_key character varying;
ALTER TABLE public.ai_config        ADD COLUMN auth_header_name character varying DEFAULT 'api-key';
ALTER TABLE public.embedding_config ADD COLUMN api_key character varying;
ALTER TABLE public.embedding_config ADD COLUMN auth_header_name character varying DEFAULT 'api-key';

CREATE TABLE public.model_template (
  id                 uuid NOT NULL DEFAULT gen_random_uuid(),
  name               character varying NOT NULL,
  description        text,
  ai_config_id       bigint NOT NULL,
  temperature        real    DEFAULT 0.3,
  max_tokens         integer DEFAULT 2048,
  top_p              real    DEFAULT 1.0,
  frequency_penalty  real    DEFAULT 0,
  presence_penalty   real    DEFAULT 0,
  system_prompt      text,
  is_default         boolean DEFAULT false,
  active_flag        boolean DEFAULT true,
  created_at         timestamp with time zone NOT NULL DEFAULT now(),
  updated_at         timestamp with time zone,
  CONSTRAINT model_template_pkey PRIMARY KEY (id),
  CONSTRAINT model_template_ai_config_id_fkey
    FOREIGN KEY (ai_config_id) REFERENCES public.ai_config(id)
);
```

Run `DBrelation/table.txt`'s migration block in Supabase before deploying
Phase 2 against an existing database.

## API shapes

Every LLM endpoint declares an **API shape**, selected via the dropdown in
the editor. Two shapes are supported today:

| Shape | URL form | Body uses | Token limit field | Stream event |
|---|---|---|---|---|
| `chat_completions` (default) | `/v1/chat/completions` | `messages` | `max_tokens` | `choices[].delta.content` + trailing `usage` |
| `responses` | `/v1/responses` | `input` | `max_output_tokens` | `response.output_text.delta` + `response.completed.usage` |

The chat path branches on this column when building the body and parsing
the stream. Putting `"input": "..."` in a Chat-Completions endpoint's
`json_request` (or `"messages"` in a Responses endpoint's) is silently
stripped before sending — pick the right shape with the dropdown
instead.

## Authoring an OpenAI endpoint

OpenAI offers two APIs. Pick the matching shape:

- **Chat Completions** — `https://api.openai.com/v1/chat/completions`,
  shape `chat_completions`. Use the **OpenAI** preset chip.
- **Responses** — `https://api.openai.com/v1/responses`, shape
  `responses`. Use the **OpenAI Responses** preset chip. Required for
  some newer models that don't ship Chat Completions support.

1. Go to **Configuration → LLM Configuration → LLM endpoints**, click
   **New endpoint**, and pick the appropriate preset.
2. Fill in:
   - **Name**: any human label.
   - **Endpoint URL**: filled by the preset; pick Chat Completions or
     Responses to match the model you're using.
   - **API shape**: filled by the preset; must match the URL.
   - **Auth header name**: `Authorization`.
   - **API key**: `Bearer sk-...` — **include the `Bearer ` prefix**.
     The value is sent verbatim as the header.
   - **JSON request template**: `{ "model": "gpt-4o" }` (or
     `gpt-4o-mini`, `gpt-4-turbo`, `gpt-4.1`, etc.).
3. Don't put `temperature`, `max_tokens`, `max_output_tokens`, `top_p`,
   `messages`, or `input` in `json_request` — the chat path injects
   them based on the API shape and the template's slider values. Only
   provider dispatch fields like `model` belong there.

## Authoring an Azure OpenAI endpoint

1. Go to **Configuration → LLM Configuration**, pick the **LLM endpoints**
   tab, click **New endpoint**.
2. Fill in:
   - **Name**: any human label, e.g. `Azure OpenAI · gpt-4o-mini`.
   - **Endpoint URL**:
     `https://{resource}.openai.azure.com/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21`
   - **Auth header name**: `api-key` (the default).
   - **API key**: paste the key from the Azure portal.
   - **JSON request template**: leave blank — Azure encodes the model in
     the URL. (For non-Azure OpenAI-compatible endpoints, put
     `{ "model": "gpt-4o-mini" }` or whatever the provider expects.)
3. Save. The endpoint appears in the list with the API key masked
   (`••••abcd`). To rotate the key later, edit the row and retype it in
   the **API key** field; leaving the field blank keeps the existing
   value.

## Authoring a template

1. Go to **Configuration → Model Setting**, click **New template**.
2. Fill in:
   - **Name**: shown in the chat dropdown.
   - **LLM endpoint**: pick from the dropdown of active endpoints.
   - **Description**: optional one-liner shown beneath the name in chat.
   - **Sliders**: temperature / top_p / frequency / presence — the current
     value is shown next to each label.
   - **Max tokens**: integer (1–32768).
   - **System prompt**: optional. Prepended to every chat as a `system`
     message unless the conversation already starts with one.
   - **Default**: when on, this template is preselected in the chat
     dropdown. Saving with this on clears the default flag on all other
     templates.
3. Save. The template appears in the list and immediately in the chat
   dropdown on the home page.

## Security note

API keys never leave the backend in plain text:

- POST/PUT bodies carry the raw key from the user's keyboard to the
  server.
- GET responses return `apiKeyMasked` (`••••` plus the last four
  characters) and a `hasApiKey` boolean. The raw value is never echoed
  back.
- On PUT, sending `apiKey: null` (the frontend default when the user
  doesn't retype) preserves the stored value. This is what enables the
  "Leave blank to keep existing" UX without exposing the secret.
- Soft-delete keeps the key in the row but flips `active_flag` to false;
  the chat path refuses to use disabled endpoints.

If you need to rotate or revoke a leaked key, edit the endpoint and
retype the new key — there is no way to retrieve an old one through the
API.
