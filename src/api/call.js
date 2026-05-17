import axios from "axios";


export async function apiPostStream(endpoint, body = {}, { params = {}, onChunk, onResponse, signal } = {}) {
    const query = new URLSearchParams(params).toString();
    const url = query ? `${endpoint}?${query}` : endpoint;

    const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
        signal,
    });

    if (!response.ok) {
        throw new Error(`POST ${endpoint} error: ${response.status}`);
    }

    if (onResponse) onResponse(response);

    const reader = response.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const text = decoder.decode(value, { stream: true });
        if (text && onChunk) {
            onChunk(text);
        }
    }
}

export async function getHistory(topicId) {
    return await apiGet("/api/Chat/GetHistory", { topicId });
}
export async function getAllHistory() {
    return await apiGet("/api/Chat/GetAllHistory");
}
// Phase 2: chat call resolves provider/params/stream-mode from the selected template
// on the backend. templateId is optional — when omitted the backend uses the row
// flagged `is_default`. The response is always SSE.
export async function callChat(messages, templateId, topicId, { onChunk, onTopic, signal } = {}) {
    await apiPostStream("/api/Chat/ChatPost", {
        messages,
        templateId,
        topicId,
    }, {
        onChunk,
        onResponse(response) {
            const id = response.headers.get('X-Topic-Id');
            if (id && onTopic) onTopic(id);
        },
        signal,
    });
}

export async function renameTopicHistory(topicId, newName) {
    return await apiPut("/api/Chat/UpdateTopicName", { id: topicId, topicName: newName });
}
export async function DeleteTopicHistory(topicId) {
    return await apiDelete("/api/Chat/DeleteTopic", {}, { topicId });
}

// ─── Configuration API ───

// AI config (LLM endpoints)
export async function getAiConfigs() {
    return await apiGet("/api/Configuration/AiConfig");
}
export async function getAiConfig(id) {
    return await apiGet(`/api/Configuration/AiConfig/${id}`);
}
export async function createAiConfig(payload) {
    return await apiPost("/api/Configuration/AiConfig", payload);
}
export async function updateAiConfig(id, payload) {
    return await apiPut(`/api/Configuration/AiConfig/${id}`, payload);
}
export async function deleteAiConfig(id) {
    return await apiDelete(`/api/Configuration/AiConfig/${id}`);
}

// Embedding config
export async function getEmbeddingConfigs() {
    return await apiGet("/api/Configuration/EmbeddingConfig");
}
export async function getEmbeddingConfig(id) {
    return await apiGet(`/api/Configuration/EmbeddingConfig/${id}`);
}
export async function createEmbeddingConfig(payload) {
    return await apiPost("/api/Configuration/EmbeddingConfig", payload);
}
export async function updateEmbeddingConfig(id, payload) {
    return await apiPut(`/api/Configuration/EmbeddingConfig/${id}`, payload);
}
export async function deleteEmbeddingConfig(id) {
    return await apiDelete(`/api/Configuration/EmbeddingConfig/${id}`);
}

// Model templates
export async function getModelTemplates() {
    return await apiGet("/api/Configuration/ModelTemplate");
}
export async function getDefaultModelTemplate() {
    return await apiGet("/api/Configuration/ModelTemplate/Default");
}
export async function getModelTemplate(id) {
    return await apiGet(`/api/Configuration/ModelTemplate/${id}`);
}
export async function createModelTemplate(payload) {
    return await apiPost("/api/Configuration/ModelTemplate", payload);
}
export async function updateModelTemplate(id, payload) {
    return await apiPut(`/api/Configuration/ModelTemplate/${id}`, payload);
}
export async function setDefaultModelTemplate(id) {
    return await apiPut(`/api/Configuration/ModelTemplate/${id}/Default`, {});
}
export async function deleteModelTemplate(id) {
    return await apiDelete(`/api/Configuration/ModelTemplate/${id}`);
}

// ─── Base API Templates ───

export async function apiGet(endpoint, params = {}) {
    try {
        const response = await axios.get(endpoint, { params });
        return response.data;
    } catch (err) {
        console.error(`GET ${endpoint} failed:`, err);
        throw err;
    }
}

export async function apiPost(endpoint, body = {}, params = {}) {
    try {
        const response = await axios.post(endpoint, body, { params });
        return response.data;
    } catch (err) {
        console.error(`POST ${endpoint} failed:`, err);
        throw err;
    }
}

export async function apiPut(endpoint, body = {}, params = {}) {
    try {
        const response = await axios.put(endpoint, body, { params });
        return response.data;
    } catch (err) {
        console.error(`PUT ${endpoint} failed:`, err);
        throw err;
    }
}

export async function apiDelete(endpoint, body = {}, params = {}) {
    try {
        const response = await axios.delete(endpoint, { params, data: body });
        return response.data;
    } catch (err) {
        console.error(`DELETE ${endpoint} failed:`, err);
        throw err;
    }
}
