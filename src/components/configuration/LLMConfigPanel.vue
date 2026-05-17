<template>
    <div class="panel">
        <div class="panel-header">
            <div class="tabs">
                <button
                    class="tab-btn"
                    :class="{ active: activeTab === 'llm' }"
                    @click="setTab('llm')"
                >
                    <v-icon size="18">mdi-brain</v-icon>
                    LLM endpoints
                </button>
                <button
                    class="tab-btn"
                    :class="{ active: activeTab === 'embedding' }"
                    @click="setTab('embedding')"
                >
                    <v-icon size="18">mdi-vector-arrange-above</v-icon>
                    Embedding endpoints
                </button>
            </div>
            <v-btn class="add-btn" :ripple="false" @click="openEditor()">
                <v-icon size="18" class="mr-1">mdi-plus</v-icon>
                New endpoint
            </v-btn>
        </div>

        <p class="panel-tip">
            <v-icon size="16" class="tip-icon">mdi-shield-key-outline</v-icon>
            API keys are stored server-side and never returned in plain text — only the last 4 characters are shown back.
        </p>

        <div v-if="loading" class="loading-state">
            <v-progress-circular indeterminate color="#646cff" size="28" width="3" />
            <span>Loading endpoints…</span>
        </div>

        <div v-else-if="!rows.length" class="empty-state">
            <v-icon size="40" color="#646cff">mdi-server-off</v-icon>
            <p>No {{ activeTab === 'llm' ? 'LLM' : 'embedding' }} endpoints configured yet.</p>
            <v-btn class="confirm-btn" :ripple="false" @click="openEditor()">Create your first one</v-btn>
        </div>

        <div v-else class="row-list">
            <div v-for="row in rows" :key="row.id" class="row-card" :class="{ inactive: !row.activeFlag }">
                <div class="row-main">
                    <div class="row-title-line">
                        <span class="row-name">{{ row.name || 'Untitled endpoint' }}</span>
                        <span v-if="!row.activeFlag" class="badge badge-muted">Disabled</span>
                    </div>
                    <div class="row-endpoint" :title="row.endpoint">{{ row.endpoint || '—' }}</div>
                    <div class="row-meta">
                        <span class="meta-pill">
                            <v-icon size="13" class="meta-icon">mdi-key-variant</v-icon>
                            {{ row.hasApiKey ? row.apiKeyMasked : 'no key' }}
                        </span>
                        <span class="meta-pill">
                            <v-icon size="13" class="meta-icon">mdi-format-text</v-icon>
                            {{ row.authHeaderName || 'api-key' }}
                        </span>
                        <span v-if="activeTab === 'llm'" class="meta-pill">
                            <v-icon size="13" class="meta-icon">mdi-api</v-icon>
                            {{ row.apiShape === 'responses' ? 'Responses' : 'Chat Completions' }}
                        </span>
                    </div>
                </div>
                <div class="row-actions">
                    <v-switch
                        :model-value="row.activeFlag"
                        density="compact"
                        color="#646cff"
                        hide-details
                        @update:model-value="toggleActive(row, $event)"
                    ></v-switch>
                    <v-btn icon variant="text" :ripple="false" class="icon-btn" @click="openEditor(row)">
                        <v-icon size="18">mdi-pencil-outline</v-icon>
                    </v-btn>
                    <v-btn icon variant="text" :ripple="false" class="icon-btn icon-btn-danger" @click="confirmDelete(row)">
                        <v-icon size="18">mdi-trash-can-outline</v-icon>
                    </v-btn>
                </div>
            </div>
        </div>

        <!-- Editor dialog -->
        <v-dialog max-width="640" v-model="editorOpen" :persistent="saving">
            <v-card class="action-dialog" theme="dark">
                <v-card-title class="dialog-title">
                    {{ editingRow ? 'Edit endpoint' : 'New endpoint' }}
                </v-card-title>
                <v-card-text class="dialog-body">
                    <div class="preset-row">
                        <span class="preset-label">Quick start</span>
                        <div class="preset-chips">
                            <button
                                v-for="p in presets"
                                :key="p.id"
                                type="button"
                                class="preset-chip"
                                :class="{ active: activePreset === p.id }"
                                :disabled="saving"
                                @click="applyPreset(p)"
                            >{{ p.label }}</button>
                        </div>
                    </div>
                    <div class="field">
                        <label>Name</label>
                        <v-text-field
                            v-model="form.name"
                            variant="outlined"
                            base-color="#bdbdbd"
                            color="#ffffff"
                            density="comfortable"
                            placeholder="e.g. Azure OpenAI · GPT-4"
                            :disabled="saving"
                        ></v-text-field>
                    </div>
                    <div class="field-row">
                        <div class="field" style="flex: 2;">
                            <label>Endpoint URL</label>
                            <v-text-field
                                v-model="form.endpoint"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                placeholder="https://api.openai.com/v1/chat/completions"
                                :disabled="saving || activeTab !== 'llm'"
                            ></v-text-field>
                        </div>
                        <div v-if="activeTab === 'llm'" class="field" style="flex: 1;">
                            <label>API shape</label>
                            <v-select
                                v-model="form.apiShape"
                                :items="shapeOptions"
                                item-title="label"
                                item-value="value"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                :menu-props="{ class: 'finchain-select-menu' }"
                                :disabled="saving"
                            ></v-select>
                        </div>
                    </div>
                    <p v-if="activeTab === 'llm'" class="field-note">
                        <v-icon size="13" class="hint-icon">mdi-information-outline</v-icon>
                        <strong>Chat Completions</strong> reads <code>choices[].delta.content</code> from the SSE stream.
                        <strong>Responses</strong> reads typed <code>response.output_text.delta</code> events.
                        Parameter naming (e.g. <code>max_tokens</code> vs <code>max_output_tokens</code>, <code>stream_options</code>) is driven by your JSON request template below — only keys you write there are sent.
                    </p>
                    <div class="field-row">
                        <div class="field" style="flex: 1;">
                            <label>Auth header name</label>
                            <v-text-field
                                v-model="form.authHeaderName"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                placeholder="api-key"
                                :disabled="saving"
                            ></v-text-field>
                        </div>
                        <div class="field" style="flex: 1;">
                            <label class="label-with-hint">
                                API key
                                <span v-if="editingRow && editingRow.hasApiKey" class="current-key-pill">
                                    <v-icon size="11" class="pill-icon">mdi-key</v-icon>
                                    current: <code>{{ editingRow.apiKeyMasked }}</code>
                                </span>
                            </label>
                            <v-text-field
                                v-model="form.apiKey"
                                :type="showKey ? 'text' : 'password'"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                :placeholder="keyPlaceholder"
                                :disabled="saving"
                                autocomplete="new-password"
                                spellcheck="false"
                            >
                                <template v-slot:append-inner>
                                    <v-icon size="18" class="key-toggle" @click="showKey = !showKey">
                                        {{ showKey ? 'mdi-eye-off-outline' : 'mdi-eye-outline' }}
                                    </v-icon>
                                </template>
                            </v-text-field>
                        </div>
                    </div>
                    <p class="field-note">
                        <v-icon size="13" class="hint-icon">mdi-information-outline</v-icon>
                        OpenAI uses <code>Authorization</code> with <code>Bearer sk-...</code>.
                        Azure OpenAI uses <code>api-key</code> with the bare key. The value is sent verbatim as the header.
                        <template v-if="editingRow && editingRow.hasApiKey">Leave the field blank to keep the existing key.</template>
                    </p>
                    <div class="field">
                        <label>
                            JSON request template
                            <span class="label-required">required</span>
                        </label>
                        <textarea
                            v-model="form.jsonRequest"
                            class="json-textarea"
                            rows="6"
                            spellcheck="false"
                            :disabled="saving"
                            placeholder='{ "model": "gpt-4o" }'
                        ></textarea>
                        <p class="field-note">This is the request body for every call. Slider values from the template overwrite <em>only the keys you list here</em> (e.g. <code>temperature</code>, <code>top_p</code>, <code>max_tokens</code> / <code>max_output_tokens</code>). Anything you omit is never sent — so add <code>stream_options</code> only if your provider supports it.</p>
                    </div>
                </v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        :loading="saving"
                        :disabled="!form.name || !form.endpoint || !form.jsonRequest || saving"
                        :ripple="false"
                        @click="save"
                    >
                        <template v-slot:loader>
                            <v-progress-circular indeterminate color="white" size="20" width="2" />
                        </template>
                        Save
                    </v-btn>
                    <v-btn
                        class="cancel-btn"
                        :disabled="saving"
                        :ripple="false"
                        @click="editorOpen = false"
                        text="Cancel"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <!-- Delete confirm -->
        <v-dialog max-width="460" v-model="deleteOpen" :persistent="deleting">
            <v-card class="action-dialog" theme="dark">
                <v-card-title class="dialog-title">Disable this endpoint?</v-card-title>
                <v-card-text>
                    "{{ deleteTarget?.name }}" will be soft-deleted. Templates pointing to it will fail until you re-enable or re-point them.
                </v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn class="confirm-btn" :loading="deleting" :disabled="deleting" :ripple="false" @click="doDelete">
                        <template v-slot:loader>
                            <v-progress-circular indeterminate color="white" size="20" width="2" />
                        </template>
                        Confirm
                    </v-btn>
                    <v-btn class="cancel-btn" :disabled="deleting" :ripple="false" @click="deleteOpen = false" text="Cancel"></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <!-- Toast -->
        <v-dialog max-width="420" v-model="resultOpen">
            <v-card class="action-dialog result-card" theme="dark">
                <div class="result-body">
                    <v-icon :color="resultStatus === 'success' ? '#646cff' : '#ff5252'" size="48">
                        {{ resultStatus === 'success' ? 'mdi-check-circle-outline' : 'mdi-alert-circle-outline' }}
                    </v-icon>
                    <div class="result-text">
                        <p class="result-title">{{ resultStatus === 'success' ? 'Done' : 'Error' }}</p>
                        <p class="result-message">{{ resultMessage }}</p>
                    </div>
                </div>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn class="confirm-btn" :ripple="false" text="OK" @click="resultOpen = false"></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import {
    getAiConfigs, createAiConfig, updateAiConfig, deleteAiConfig,
    getEmbeddingConfigs, createEmbeddingConfig, updateEmbeddingConfig, deleteEmbeddingConfig,
} from '../../api/call.js'

const activeTab = ref('llm')
const rows = ref([])
const loading = ref(false)

const editorOpen = ref(false)
const saving = ref(false)
const editingRow = ref(null)
const showKey = ref(false)
const activePreset = ref(null)

const form = reactive({
    name: '',
    endpoint: '',
    authHeaderName: 'api-key',
    apiShape: 'chat_completions',
    apiKey: '',
    jsonRequest: '',
})

const shapeOptions = [
    { label: 'Chat Completions', value: 'chat_completions' },
    { label: 'Responses', value: 'responses' },
]

// Click-to-fill shapes for known providers. Each preset overwrites everything
// except the API key (which is the user's secret to type in).
// json_request below intentionally lists the slider keys (with placeholder zeros) the
// provider expects — the backend fills those keys with the template's slider values at
// request time. Anything not listed is never sent, which keeps providers like GPT-5.4
// (which reject `stream_options.include_usage`) from being poisoned by stray params.
const chatCompletionsBody =
    '{\n  "model": "gpt-4o",\n  "temperature": 0,\n  "top_p": 0,\n  "max_tokens": 0,\n  "frequency_penalty": 0,\n  "presence_penalty": 0,\n  "stream_options": { "include_usage": true }\n}'
const responsesBody =
    '{\n  "model": "gpt-4o",\n  "temperature": 0,\n  "top_p": 0,\n  "max_output_tokens": 0\n}'

const presets = [
    {
        id: 'openai',
        label: 'OpenAI',
        apply: () => ({
            name: 'OpenAI · GPT-4o',
            endpoint: 'https://api.openai.com/v1/chat/completions',
            authHeaderName: 'Authorization',
            apiShape: 'chat_completions',
            jsonRequest: chatCompletionsBody,
        }),
    },
    {
        id: 'openai-responses',
        label: 'OpenAI Responses',
        apply: () => ({
            name: 'OpenAI · GPT-4o (Responses)',
            endpoint: 'https://api.openai.com/v1/responses',
            authHeaderName: 'Authorization',
            apiShape: 'responses',
            jsonRequest: responsesBody,
        }),
    },
    {
        id: 'azure',
        label: 'Azure OpenAI',
        apply: () => ({
            name: 'Azure OpenAI · GPT-4o',
            endpoint: 'https://{resource}.openai.azure.com/openai/deployments/{deployment}/chat/completions?api-version=2024-10-21',
            authHeaderName: 'api-key',
            apiShape: 'chat_completions',
            jsonRequest: chatCompletionsBody,
        }),
    },
    {
        id: 'thaillm',
        label: 'ThaiLLM',
        apply: () => ({
            name: 'ThaiLLM · OpenThaiGPT',
            endpoint: 'http://thaillm.or.th/api/openthaigpt/v1/chat/completions',
            authHeaderName: 'apikey',
            apiShape: 'chat_completions',
            jsonRequest: chatCompletionsBody,
        }),
    },
]

function applyPreset(preset) {
    const filled = preset.apply()
    form.name = filled.name
    form.endpoint = filled.endpoint
    form.authHeaderName = filled.authHeaderName
    form.apiShape = filled.apiShape
    form.jsonRequest = filled.jsonRequest
    activePreset.value = preset.id
}

const deleteOpen = ref(false)
const deleting = ref(false)
const deleteTarget = ref(null)

const resultOpen = ref(false)
const resultStatus = ref('success')
const resultMessage = ref('')

const keyPlaceholder = computed(() => {
    if (editingRow.value && editingRow.value.hasApiKey) {
        // Show the masked value as the placeholder so the user *sees* there's an existing
        // key. Submitting with the field blank preserves it (the backend reads null as
        // "keep current"); typing replaces it.
        return `${editingRow.value.apiKeyMasked} — leave blank to keep`
    }
    return 'Paste your API key'
})

function showResult(status, message) {
    resultStatus.value = status
    resultMessage.value = message
    resultOpen.value = true
}

async function loadRows() {
    loading.value = true
    try {
        const response = activeTab.value === 'llm' ? await getAiConfigs() : await getEmbeddingConfigs()
        rows.value = (response?.data ?? []).map(normalizeRow)
    } catch (err) {
        rows.value = []
        showResult('error', err?.message || 'Failed to load endpoints.')
    } finally {
        loading.value = false
    }
}

function normalizeRow(r) {
    // Backend returns camelCase via System.Text.Json default policy.
    return {
        id: r.id,
        name: r.name,
        endpoint: r.endpoint,
        jsonRequest: r.jsonRequest,
        authHeaderName: r.authHeaderName,
        apiShape: r.apiShape || 'chat_completions',
        apiKeyMasked: r.apiKeyMasked,
        hasApiKey: r.hasApiKey,
        activeFlag: r.activeFlag,
    }
}

function setTab(tab) {
    if (activeTab.value === tab) return
    activeTab.value = tab
    loadRows()
}

function resetForm() {
    form.name = ''
    form.endpoint = ''
    form.authHeaderName = 'api-key'
    form.apiShape = 'chat_completions'
    form.apiKey = ''
    form.jsonRequest = ''
    showKey.value = false
    activePreset.value = null
}

function openEditor(row) {
    resetForm()
    if (row) {
        editingRow.value = row
        form.name = row.name || ''
        form.endpoint = row.endpoint || ''
        form.authHeaderName = row.authHeaderName || 'api-key'
        form.apiShape = row.apiShape || 'chat_completions'
        form.jsonRequest = row.jsonRequest || ''
        // Leave apiKey empty — user must explicitly retype to change it.
    } else {
        editingRow.value = null
    }
    editorOpen.value = true
}

async function save() {
    saving.value = true
    const payload = {
        name: form.name,
        endpoint: form.endpoint,
        authHeaderName: form.authHeaderName || 'api-key',
        // apiShape is meaningful only for LLM endpoints — embedding endpoints ignore it server-side.
        apiShape: activeTab.value === 'llm' ? form.apiShape : null,
        jsonRequest: form.jsonRequest || null,
        apiKey: form.apiKey || null,
    }
    try {
        if (editingRow.value) {
            const fn = activeTab.value === 'llm' ? updateAiConfig : updateEmbeddingConfig
            await fn(editingRow.value.id, payload)
            showResult('success', 'Endpoint updated.')
        } else {
            const fn = activeTab.value === 'llm' ? createAiConfig : createEmbeddingConfig
            await fn(payload)
            showResult('success', 'Endpoint created.')
        }
        editorOpen.value = false
        await loadRows()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to save.')
    } finally {
        saving.value = false
    }
}

async function toggleActive(row, value) {
    try {
        const fn = activeTab.value === 'llm' ? updateAiConfig : updateEmbeddingConfig
        await fn(row.id, {
            name: row.name,
            endpoint: row.endpoint,
            authHeaderName: row.authHeaderName,
            apiShape: activeTab.value === 'llm' ? row.apiShape : null,
            jsonRequest: row.jsonRequest,
            apiKey: null,
            activeFlag: value,
        })
        row.activeFlag = value
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to toggle endpoint.')
        await loadRows()
    }
}

function confirmDelete(row) {
    deleteTarget.value = row
    deleteOpen.value = true
}

async function doDelete() {
    if (!deleteTarget.value) return
    deleting.value = true
    try {
        const fn = activeTab.value === 'llm' ? deleteAiConfig : deleteEmbeddingConfig
        await fn(deleteTarget.value.id)
        deleteOpen.value = false
        showResult('success', 'Endpoint disabled.')
        await loadRows()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to delete.')
    } finally {
        deleting.value = false
    }
}

onMounted(loadRows)
</script>

<style scoped>
.panel {
    display: flex;
    flex-direction: column;
    gap: 16px;
    height: 100%;
    overflow-y: auto;
}

.panel-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
}

.tabs {
    display: flex;
    gap: 4px;
    background-color: #1f1f1f;
    border-radius: 10px;
    padding: 4px;
}

.tab-btn {
    background: transparent;
    border: none;
    color: #bdbdbd;
    font-size: 13px;
    padding: 8px 14px;
    border-radius: 8px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 6px;
    transition: background-color 0.15s ease, color 0.15s ease;
}

.tab-btn:hover {
    color: rgba(255, 255, 255, 0.87);
}

.tab-btn.active {
    background-color: #4a4a4a;
    color: #ffffff;
}

.add-btn {
    background-color: #646cff !important;
    color: white !important;
    text-transform: none !important;
    letter-spacing: normal !important;
    box-shadow: none !important;
    border: none !important;
    border-radius: 10px !important;
}

.add-btn:hover {
    background-color: #535bf2 !important;
}

.panel-tip {
    display: flex;
    align-items: center;
    gap: 6px;
    color: #9e9e9e;
    font-size: 12px;
    margin: 0;
}

.tip-icon {
    color: #646cff;
}

.loading-state,
.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 12px;
    padding: 60px 20px;
    color: #9e9e9e;
}

.row-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.row-card {
    display: flex;
    align-items: center;
    gap: 12px;
    background-color: #1f1f1f;
    border-radius: 12px;
    padding: 14px 16px;
    border: 1px solid transparent;
    transition: border-color 0.2s ease;
}

.row-card:hover {
    border-color: #3a3a3a;
}

.row-card.inactive {
    opacity: 0.55;
}

.row-main {
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 4px;
}

.row-title-line {
    display: flex;
    align-items: center;
    gap: 8px;
}

.row-name {
    font-size: 15px;
    font-weight: 500;
    color: rgba(255, 255, 255, 0.87);
}

.badge {
    font-size: 11px;
    padding: 2px 8px;
    border-radius: 999px;
}

.badge-muted {
    background-color: #3a3a3a;
    color: #bdbdbd;
}

.row-endpoint {
    font-size: 12px;
    color: #9e9e9e;
    font-family: 'Cascadia Mono', 'Consolas', monospace;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.row-meta {
    display: flex;
    gap: 8px;
    margin-top: 4px;
}

.meta-pill {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background-color: #2a2a2a;
    color: #bdbdbd;
    font-size: 11px;
    padding: 3px 8px;
    border-radius: 6px;
}

.meta-icon {
    color: #9e9e9e;
}

.row-actions {
    display: flex;
    align-items: center;
    gap: 4px;
}

.icon-btn {
    color: #bdbdbd !important;
    width: 32px !important;
    height: 32px !important;
    border-radius: 8px !important;
    box-shadow: none !important;
}

.icon-btn:hover {
    background-color: #3a3a3a !important;
    color: #ffffff !important;
}

.icon-btn-danger:hover {
    color: #ff8a80 !important;
}

.icon-btn :deep(.v-btn__overlay),
.icon-btn :deep(.v-btn__underlay) {
    display: none;
}

/* Dialog */
.action-dialog {
    background-color: #2a2a2a;
    color: #bdbdbd;
}

.dialog-title {
    color: rgba(255, 255, 255, 0.87);
    font-size: 18px;
}

.dialog-body {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding-bottom: 8px;
}

.preset-row {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 12px;
    background-color: #1a1a1a;
    border: 1px solid #3a3a3a;
    border-radius: 10px;
    padding: 8px 12px;
}

.preset-label {
    font-size: 12px;
    color: #9e9e9e;
    font-weight: 500;
    flex-shrink: 0;
}

.preset-chips {
    display: flex;
    gap: 6px;
    flex-wrap: wrap;
}

.preset-chip {
    background-color: #2a2a2a;
    color: #bdbdbd;
    border: 1px solid #3a3a3a;
    border-radius: 999px;
    padding: 4px 12px;
    font-size: 12px;
    cursor: pointer;
    transition: background-color 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.preset-chip:hover:not(:disabled) {
    background-color: #3a3a3a;
    color: #ffffff;
    border-color: #4a4a4a;
}

.preset-chip.active {
    background-color: rgba(100, 108, 255, 0.18);
    color: #c2c6ff;
    border-color: rgba(100, 108, 255, 0.4);
}

.preset-chip:disabled {
    opacity: 0.5;
    cursor: not-allowed;
}

.hint-icon {
    color: #646cff;
    vertical-align: -2px;
    margin-right: 2px;
}

.label-with-hint {
    display: flex !important;
    align-items: center;
    gap: 8px;
}

.current-key-pill {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    font-size: 11px;
    font-weight: 400;
    color: #c2c6ff;
    background-color: rgba(100, 108, 255, 0.18);
    padding: 2px 8px;
    border-radius: 999px;
}

.current-key-pill code {
    background: transparent;
    padding: 0;
    color: #ffffff;
    font-family: 'Cascadia Mono', 'Consolas', monospace;
}

.pill-icon {
    color: #c2c6ff;
}

.label-required {
    font-size: 11px;
    font-weight: 400;
    color: #ff8a80;
    margin-left: 6px;
    text-transform: uppercase;
    letter-spacing: 0.05em;
}

.field {
    display: flex;
    flex-direction: column;
    gap: 4px;
    margin-bottom: 4px;
}

.field label {
    font-size: 12px;
    color: #9e9e9e;
    font-weight: 500;
}

.field-row {
    display: flex;
    gap: 12px;
}

.field-note {
    font-size: 12px;
    color: #9e9e9e;
    margin: -4px 0 4px;
}

.field-note code {
    background-color: #1f1f1f;
    padding: 1px 5px;
    border-radius: 4px;
    color: #c7c7c7;
    font-family: 'Cascadia Mono', 'Consolas', monospace;
}

.json-textarea {
    width: 100%;
    background-color: #1a1a1a;
    color: #e0e0e0;
    border: 1px solid #3a3a3a;
    border-radius: 8px;
    padding: 10px 12px;
    font-family: 'Cascadia Mono', 'Consolas', monospace;
    font-size: 12px;
    line-height: 1.45;
    resize: vertical;
}

.json-textarea:focus {
    outline: none;
    border-color: #646cff;
}

.key-toggle {
    color: #9e9e9e;
    cursor: pointer;
}

.key-toggle:hover {
    color: rgba(255, 255, 255, 0.87);
}

.confirm-btn,
.cancel-btn {
    outline: none !important;
    box-shadow: none !important;
    border: none !important;
    text-transform: none !important;
    letter-spacing: normal !important;
}

.confirm-btn {
    background-color: #646cff !important;
    color: white !important;
}

.confirm-btn:hover {
    background-color: #535bf2 !important;
}

.cancel-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.confirm-btn :deep(.v-btn__overlay),
.confirm-btn :deep(.v-btn__underlay),
.cancel-btn :deep(.v-btn__overlay),
.cancel-btn :deep(.v-btn__underlay) {
    display: none;
}

.result-card {
    padding: 20px;
}
.result-body {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 8px 8px 16px 8px;
}
.result-text {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.result-title {
    font-size: 18px;
    font-weight: 500;
    color: rgba(255, 255, 255, 0.87);
    margin: 0;
}
.result-message {
    font-size: 14px;
    color: #bdbdbd;
    margin: 0;
}
</style>
