<template>
    <div class="panel">
        <div class="panel-header">
            <div class="header-text">
                <h2 class="header-title">Model templates</h2>
                <p class="header-subtitle">Each template bundles an LLM endpoint with its parameters. The default template is preselected on the chat page.</p>
            </div>
            <v-btn class="add-btn" :ripple="false" :disabled="!llmOptions.length" @click="openEditor()">
                <v-icon size="18" class="mr-1">mdi-plus</v-icon>
                New template
            </v-btn>
        </div>

        <div v-if="!llmOptions.length && !loading" class="warning-banner">
            <v-icon size="18" color="#ffb74d">mdi-alert-outline</v-icon>
            <span>You need at least one active LLM endpoint before creating a template. Open <router-link to="/configuration/llm" class="warning-link">LLM Configuration</router-link> first.</span>
        </div>

        <div v-if="loading" class="loading-state">
            <v-progress-circular indeterminate color="#646cff" size="28" width="3" />
            <span>Loading templates…</span>
        </div>

        <div v-else-if="!templates.length" class="empty-state">
            <v-icon size="40" color="#646cff">mdi-tune-vertical</v-icon>
            <p>No templates yet — your first one will become the chat default.</p>
            <v-btn class="confirm-btn" :ripple="false" :disabled="!llmOptions.length" @click="openEditor()">
                Create a template
            </v-btn>
        </div>

        <div v-else class="row-list">
            <div v-for="t in templates" :key="t.id" class="row-card" :class="{ inactive: !t.activeFlag }">
                <div class="row-main">
                    <div class="row-title-line">
                        <span class="row-name">{{ t.name }}</span>
                        <span v-if="t.isDefault" class="badge badge-default">
                            <v-icon size="13" class="badge-icon">mdi-star</v-icon>
                            Default
                        </span>
                        <span v-if="!t.activeFlag" class="badge badge-muted">Disabled</span>
                    </div>
                    <div class="row-description" v-if="t.description">{{ t.description }}</div>
                    <div class="row-meta">
                        <span class="meta-pill">
                            <v-icon size="13" class="meta-icon">mdi-server-network</v-icon>
                            {{ t.aiConfigName || '—' }}
                        </span>
                        <span class="meta-pill">temp {{ t.temperature.toFixed(2) }}</span>
                        <span class="meta-pill">max {{ t.maxTokens }}</span>
                        <span class="meta-pill">top_p {{ t.topP.toFixed(2) }}</span>
                    </div>
                </div>
                <div class="row-actions">
                    <v-btn
                        v-if="!t.isDefault && t.activeFlag"
                        variant="text"
                        :ripple="false"
                        class="text-btn"
                        @click="makeDefault(t)"
                    >Make default</v-btn>
                    <v-switch
                        :model-value="t.activeFlag"
                        density="compact"
                        color="#646cff"
                        hide-details
                        @update:model-value="toggleActive(t, $event)"
                    ></v-switch>
                    <v-btn icon variant="text" :ripple="false" class="icon-btn" @click="openEditor(t)">
                        <v-icon size="18">mdi-pencil-outline</v-icon>
                    </v-btn>
                    <v-btn icon variant="text" :ripple="false" class="icon-btn icon-btn-danger" @click="confirmDelete(t)">
                        <v-icon size="18">mdi-trash-can-outline</v-icon>
                    </v-btn>
                </div>
            </div>
        </div>

        <!-- Editor dialog -->
        <v-dialog max-width="720" v-model="editorOpen" :persistent="saving">
            <v-card class="action-dialog" theme="dark">
                <v-card-title class="dialog-title">
                    {{ editingRow ? 'Edit template' : 'New template' }}
                </v-card-title>
                <v-card-text class="dialog-body">
                    <div class="field-row">
                        <div class="field" style="flex: 1.2;">
                            <label>Name</label>
                            <v-text-field
                                v-model="form.name"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                placeholder="e.g. Precise Analysis"
                                :disabled="saving"
                            ></v-text-field>
                        </div>
                        <div class="field" style="flex: 1.4;">
                            <label>LLM endpoint</label>
                            <v-select
                                v-model="form.aiConfigId"
                                :items="llmOptions"
                                item-title="name"
                                item-value="id"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                :menu-props="{ class: 'finchain-select-menu' }"
                                :disabled="saving"
                                placeholder="Select an endpoint"
                            ></v-select>
                        </div>
                    </div>
                    <div class="field">
                        <label>Description (optional)</label>
                        <v-text-field
                            v-model="form.description"
                            variant="outlined"
                            base-color="#bdbdbd"
                            color="#ffffff"
                            density="comfortable"
                            placeholder="Shown beneath the name in the chat dropdown"
                            :disabled="saving"
                        ></v-text-field>
                    </div>

                    <div class="slider-grid">
                        <div class="slider-row">
                            <div class="slider-head">
                                <label>Temperature</label>
                                <span class="slider-value">{{ form.temperature.toFixed(2) }}</span>
                            </div>
                            <v-slider
                                v-model="form.temperature"
                                :min="0" :max="2" :step="0.05"
                                color="#646cff" track-color="#3a3a3a" thumb-color="#646cff"
                                hide-details :disabled="saving"
                            ></v-slider>
                        </div>
                        <div class="slider-row">
                            <div class="slider-head">
                                <label>Top P</label>
                                <span class="slider-value">{{ form.topP.toFixed(2) }}</span>
                            </div>
                            <v-slider
                                v-model="form.topP"
                                :min="0" :max="1" :step="0.05"
                                color="#646cff" track-color="#3a3a3a" thumb-color="#646cff"
                                hide-details :disabled="saving"
                            ></v-slider>
                        </div>
                        <div class="slider-row">
                            <div class="slider-head">
                                <label>Frequency penalty</label>
                                <span class="slider-value">{{ form.frequencyPenalty.toFixed(2) }}</span>
                            </div>
                            <v-slider
                                v-model="form.frequencyPenalty"
                                :min="-2" :max="2" :step="0.05"
                                color="#646cff" track-color="#3a3a3a" thumb-color="#646cff"
                                hide-details :disabled="saving"
                            ></v-slider>
                        </div>
                        <div class="slider-row">
                            <div class="slider-head">
                                <label>Presence penalty</label>
                                <span class="slider-value">{{ form.presencePenalty.toFixed(2) }}</span>
                            </div>
                            <v-slider
                                v-model="form.presencePenalty"
                                :min="-2" :max="2" :step="0.05"
                                color="#646cff" track-color="#3a3a3a" thumb-color="#646cff"
                                hide-details :disabled="saving"
                            ></v-slider>
                        </div>
                    </div>

                    <div class="field-row">
                        <div class="field" style="flex: 1;">
                            <label>Max tokens</label>
                            <v-text-field
                                v-model.number="form.maxTokens"
                                type="number"
                                variant="outlined"
                                base-color="#bdbdbd"
                                color="#ffffff"
                                density="comfortable"
                                :min="1"
                                :max="32768"
                                :disabled="saving"
                            ></v-text-field>
                        </div>
                        <div class="field default-toggle-field" style="flex: 1;">
                            <label>Default</label>
                            <div class="default-toggle">
                                <v-switch
                                    v-model="form.isDefault"
                                    density="compact"
                                    color="#646cff"
                                    hide-details
                                    :disabled="saving"
                                ></v-switch>
                                <span class="default-toggle-hint">Preselected on the chat page</span>
                            </div>
                        </div>
                    </div>

                    <div class="field">
                        <label>System prompt (optional)</label>
                        <textarea
                            v-model="form.systemPrompt"
                            class="prompt-textarea"
                            rows="4"
                            :disabled="saving"
                            placeholder="You are a financial analyst assistant…"
                        ></textarea>
                    </div>
                </v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        :loading="saving"
                        :disabled="!form.name || !form.aiConfigId || saving"
                        :ripple="false"
                        @click="save"
                    >
                        <template v-slot:loader>
                            <v-progress-circular indeterminate color="white" size="20" width="2" />
                        </template>
                        Save
                    </v-btn>
                    <v-btn class="cancel-btn" :disabled="saving" :ripple="false" @click="editorOpen = false" text="Cancel"></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <!-- Delete confirm -->
        <v-dialog max-width="460" v-model="deleteOpen" :persistent="deleting">
            <v-card class="action-dialog" theme="dark">
                <v-card-title class="dialog-title">Disable this template?</v-card-title>
                <v-card-text>
                    "{{ deleteTarget?.name }}" will be soft-deleted and removed from the chat dropdown.
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
import { ref, reactive, onMounted } from 'vue'
import {
    getModelTemplates, createModelTemplate, updateModelTemplate,
    deleteModelTemplate, setDefaultModelTemplate, getAiConfigs,
} from '../../api/call.js'
import { useChat } from '../../composables/useChat.js'

const { loadTemplates: refreshChatTemplates } = useChat()

const templates = ref([])
const llmOptions = ref([])
const loading = ref(false)

const editorOpen = ref(false)
const saving = ref(false)
const editingRow = ref(null)

const defaultForm = () => ({
    name: '',
    description: '',
    aiConfigId: null,
    temperature: 0.3,
    maxTokens: 2048,
    topP: 1.0,
    frequencyPenalty: 0,
    presencePenalty: 0,
    systemPrompt: '',
    isDefault: false,
})
const form = reactive(defaultForm())

const deleteOpen = ref(false)
const deleting = ref(false)
const deleteTarget = ref(null)

const resultOpen = ref(false)
const resultStatus = ref('success')
const resultMessage = ref('')

function showResult(status, message) {
    resultStatus.value = status
    resultMessage.value = message
    resultOpen.value = true
}

async function loadAll() {
    loading.value = true
    try {
        const [templatesRes, aiRes] = await Promise.all([getModelTemplates(), getAiConfigs()])
        templates.value = (templatesRes?.data ?? []).map(normalizeTemplate)
        llmOptions.value = (aiRes?.data ?? [])
            .filter(c => c.activeFlag)
            .map(c => ({ id: c.id, name: c.name }))
    } catch (err) {
        templates.value = []
        llmOptions.value = []
        showResult('error', err?.message || 'Failed to load templates.')
    } finally {
        loading.value = false
    }
}

function normalizeTemplate(t) {
    return {
        id: t.id,
        name: t.name,
        description: t.description,
        aiConfigId: t.aiConfigId,
        aiConfigName: t.aiConfigName,
        temperature: t.temperature ?? 0.3,
        maxTokens: t.maxTokens ?? 2048,
        topP: t.topP ?? 1.0,
        frequencyPenalty: t.frequencyPenalty ?? 0,
        presencePenalty: t.presencePenalty ?? 0,
        systemPrompt: t.systemPrompt,
        isDefault: t.isDefault,
        activeFlag: t.activeFlag,
    }
}

function openEditor(row) {
    Object.assign(form, defaultForm())
    if (row) {
        editingRow.value = row
        form.name = row.name || ''
        form.description = row.description || ''
        form.aiConfigId = row.aiConfigId
        form.temperature = row.temperature
        form.maxTokens = row.maxTokens
        form.topP = row.topP
        form.frequencyPenalty = row.frequencyPenalty
        form.presencePenalty = row.presencePenalty
        form.systemPrompt = row.systemPrompt || ''
        form.isDefault = row.isDefault
    } else {
        editingRow.value = null
        // First template the user creates becomes the default automatically.
        if (!templates.value.length) form.isDefault = true
    }
    editorOpen.value = true
}

async function save() {
    saving.value = true
    const payload = {
        name: form.name,
        description: form.description || null,
        aiConfigId: form.aiConfigId,
        temperature: Number(form.temperature),
        maxTokens: Number(form.maxTokens),
        topP: Number(form.topP),
        frequencyPenalty: Number(form.frequencyPenalty),
        presencePenalty: Number(form.presencePenalty),
        systemPrompt: form.systemPrompt || null,
        isDefault: !!form.isDefault,
    }
    try {
        if (editingRow.value) {
            await updateModelTemplate(editingRow.value.id, payload)
            showResult('success', 'Template updated.')
        } else {
            await createModelTemplate(payload)
            showResult('success', 'Template created.')
        }
        editorOpen.value = false
        await loadAll()
        await refreshChatTemplates()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to save.')
    } finally {
        saving.value = false
    }
}

async function makeDefault(t) {
    try {
        await setDefaultModelTemplate(t.id)
        showResult('success', `"${t.name}" is now the default template.`)
        await loadAll()
        await refreshChatTemplates()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to set default.')
    }
}

async function toggleActive(t, value) {
    try {
        await updateModelTemplate(t.id, {
            name: t.name,
            description: t.description,
            aiConfigId: t.aiConfigId,
            temperature: t.temperature,
            maxTokens: t.maxTokens,
            topP: t.topP,
            frequencyPenalty: t.frequencyPenalty,
            presencePenalty: t.presencePenalty,
            systemPrompt: t.systemPrompt,
            isDefault: t.isDefault && value,
            activeFlag: value,
        })
        await loadAll()
        await refreshChatTemplates()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to toggle template.')
        await loadAll()
    }
}

function confirmDelete(t) {
    deleteTarget.value = t
    deleteOpen.value = true
}

async function doDelete() {
    if (!deleteTarget.value) return
    deleting.value = true
    try {
        await deleteModelTemplate(deleteTarget.value.id)
        deleteOpen.value = false
        showResult('success', 'Template disabled.')
        await loadAll()
        await refreshChatTemplates()
    } catch (err) {
        showResult('error', err?.response?.data?.error || err?.message || 'Failed to delete.')
    } finally {
        deleting.value = false
    }
}

onMounted(loadAll)
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
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
}

.header-text {
    display: flex;
    flex-direction: column;
}

.header-title {
    color: rgba(255, 255, 255, 0.87);
    font-size: 18px;
    font-weight: 600;
    margin: 0;
}

.header-subtitle {
    color: #9e9e9e;
    font-size: 12px;
    margin: 4px 0 0;
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

.warning-banner {
    display: flex;
    align-items: center;
    gap: 8px;
    background-color: rgba(255, 183, 77, 0.12);
    border: 1px solid rgba(255, 183, 77, 0.3);
    color: #ffd9a3;
    padding: 10px 14px;
    border-radius: 10px;
    font-size: 13px;
}

.warning-link {
    color: #ffd9a3;
    text-decoration: underline;
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

.row-description {
    font-size: 13px;
    color: #9e9e9e;
}

.row-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
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

.badge {
    font-size: 11px;
    padding: 2px 8px;
    border-radius: 999px;
    display: inline-flex;
    align-items: center;
    gap: 4px;
}

.badge-default {
    background-color: rgba(100, 108, 255, 0.18);
    color: #c2c6ff;
}

.badge-icon {
    color: #c2c6ff;
}

.badge-muted {
    background-color: #3a3a3a;
    color: #bdbdbd;
}

.row-actions {
    display: flex;
    align-items: center;
    gap: 4px;
}

.text-btn {
    color: #c2c6ff !important;
    font-size: 12px !important;
    text-transform: none !important;
    letter-spacing: normal !important;
    box-shadow: none !important;
}

.text-btn:hover {
    background-color: rgba(100, 108, 255, 0.18) !important;
    color: #ffffff !important;
}

.text-btn :deep(.v-btn__overlay),
.text-btn :deep(.v-btn__underlay) {
    display: none;
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

.slider-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px 24px;
    margin: 4px 0 8px;
}

.slider-row {
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.slider-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.slider-head label {
    font-size: 12px;
    color: #9e9e9e;
    font-weight: 500;
}

.slider-value {
    font-size: 12px;
    color: rgba(255, 255, 255, 0.87);
    font-family: 'Cascadia Mono', 'Consolas', monospace;
}

.default-toggle-field {
    align-items: flex-start;
}

.default-toggle {
    display: flex;
    align-items: center;
    gap: 8px;
    background-color: #1a1a1a;
    border: 1px solid #3a3a3a;
    border-radius: 8px;
    padding: 4px 12px;
    height: 40px;
}

.default-toggle-hint {
    font-size: 12px;
    color: #9e9e9e;
}

.prompt-textarea {
    width: 100%;
    background-color: #1a1a1a;
    color: #e0e0e0;
    border: 1px solid #3a3a3a;
    border-radius: 8px;
    padding: 10px 12px;
    font-family: inherit;
    font-size: 13px;
    line-height: 1.5;
    resize: vertical;
}

.prompt-textarea:focus {
    outline: none;
    border-color: #646cff;
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
