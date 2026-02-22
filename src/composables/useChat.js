import { ref, reactive } from 'vue'
import { callThaiLLM } from '../api/call.js'

const messages = ref([])
const isLoading = ref(false)
const topicId = ref(null)

export function useChat() {
  async function sendMessage(text, model = "openthaigpt", topicId) {
    if (!text.trim() || isLoading.value) return

    // 1) Add user message
    messages.value.push({
      role: 'U',
      content: text,
    })

    // 2) Add empty bot message (will stream into it)
    const botMsg = reactive({
      role: 'B',
      content: '',
    })
    messages.value.push(botMsg)

    // 3) Call the API with streaming
    isLoading.value = true
    try {
      await callThaiLLM(text, model, topicId, {
        onChunk(chunk) {
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

  return { messages, isLoading, topicId, sendMessage, loadHistory }

}
