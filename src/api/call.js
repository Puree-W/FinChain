import axios from "axios";


export async function apiPostStream(endpoint, body = {}, { params = {}, onChunk, signal } = {}) {
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
export async function callThaiLLM(input, model, topicId, { onChunk, signal } = {}) {
    await apiPostStream("/api/Chat/ChatPost", {
        stream: true,
        messages: [{ role: 'user', content: input }],
        maxTokens: 2048,
        temperature: 0.3,
        topicId,
    }, {
        params: { model },
        onChunk,
        signal,
    });
}

export async function renameTopicHistory(topicId, newName) {
    return await apiPut("/api/Chat/UpdateTopicName", { id: topicId, topicName: newName });
}
export async function DeleteTopicHistory(topicId) {
 return await apiDelete("/api/Chat/DeleteTopic", { topicId });
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
        const response = await axios.delete(endpoint, body, { params });
        return response.data;
    } catch (err) {
        console.error(`POST ${endpoint} failed:`, err);
        throw err;
    }
}
