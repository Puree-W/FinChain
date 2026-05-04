import { ref, reactive } from 'vue'
import { callThaiLLM } from '../api/call.js'

const messages = ref([])
const isLoading = ref(false)
const isStreamDone = ref(false)
const isAtBottom = ref(true)
const scrollAreaRef = ref(null)
const topicId = ref(null)

export function useChat() {
  async function sendMessage(text, model = "openthaigpt", topicId) {
    if (!text.trim() || isLoading.value) return

    // 1) Add user message
    messages.value.push({
      role: 'user',
      content: text,
    })

    // 2) Add empty bot message (will stream into it)
    const botMsg = reactive({
      role: 'assistant',
      content: '',
    })
    messages.value.push(botMsg)

    // 3) Call the API with streaming
    isLoading.value = true
    isStreamDone.value = false
    try {
      await callThaiLLM(text, model, topicId, {
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

  return { messages, isLoading, isStreamDone, isAtBottom, scrollAreaRef, topicId, sendMessage, loadHistory, scrollToBottom }

}
