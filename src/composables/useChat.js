import { ref, reactive } from 'vue'
import { callChat, getModelTemplates } from '../api/call.js'

const messages = ref([])
const isLoading = ref(false)
const isStreamDone = ref(false)
const isAtBottom = ref(true)
const scrollAreaRef = ref(null)
const topicId = ref(null)
// Incremented each time the backend hands us a brand-new topic id — Homepage
// watches this to refresh the navbar history list.
const newTopicCreated = ref(0)

// Phase 2 — templates are now the user-managed equivalent of a "model".
const templates = ref([])
const selectedTemplate = ref(null)
const templatesLoading = ref(false)

export function useChat() {
  async function sendMessage(text) {
    if (!text.trim() || isLoading.value) return

    // 1) Add user message
    messages.value.push({
      role: 'user',
      content: text,
    })

    // 2) Snapshot full conversation history so backend has multi-turn context
    const conversation = messages.value.map(m => ({
      role: m.role,
      content: m.content,
    }))

    // 3) Add empty bot message (will stream into it)
    const botMsg = reactive({
      role: 'assistant',
      content: '',
    })
    messages.value.push(botMsg)

    // 4) Call the API with streaming
    const wasNewTopic = !topicId.value
    isLoading.value = true
    isStreamDone.value = false
    try {
      const templateId = selectedTemplate.value?.id ?? null
      await callChat(conversation, templateId, topicId.value, {
        onTopic(id) {
          if (!topicId.value) {
            topicId.value = id
          }
        },
        onChunk(chunk) {
          if (chunk.includes('[DONE]')) {
            isStreamDone.value = true
            const cleaned = chunk.replace('[DONE]', '')
            if (cleaned) botMsg.content += cleaned
            return
          }
          botMsg.content += chunk
        },
      })
      if (wasNewTopic && topicId.value) {
        newTopicCreated.value++
      }
    } catch (err) {
      botMsg.content = 'Error: ' + err.message
    } finally {
      isLoading.value = false
    }
  }

  function loadHistory(historyMessages, newTopicId) {
    messages.value = historyMessages
    topicId.value = newTopicId
  }

  function scrollToBottom() {
    const el = scrollAreaRef.value
    if (el) el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' })
  }

  async function loadTemplates() {
    templatesLoading.value = true
    try {
      const response = await getModelTemplates()
      const list = response?.data ?? []
      templates.value = list
      // Preserve the user's prior selection if it still exists; otherwise fall back
      // to the row marked default; otherwise the first template.
      const stillExists = selectedTemplate.value
        ? list.find(t => t.id === selectedTemplate.value.id)
        : null
      if (stillExists) {
        selectedTemplate.value = stillExists
      } else {
        selectedTemplate.value = list.find(t => t.isDefault) ?? list[0] ?? null
      }
    } catch (err) {
      console.error('Failed to load model templates:', err)
      templates.value = []
      selectedTemplate.value = null
    } finally {
      templatesLoading.value = false
    }
  }

  function selectTemplate(template) {
    selectedTemplate.value = template
  }

  return {
    messages,
    isLoading,
    isStreamDone,
    isAtBottom,
    scrollAreaRef,
    topicId,
    newTopicCreated,
    templates,
    selectedTemplate,
    templatesLoading,
    sendMessage,
    loadHistory,
    scrollToBottom,
    loadTemplates,
    selectTemplate,
  }
}
