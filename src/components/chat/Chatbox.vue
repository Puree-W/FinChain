<template>
  <div class="chatbox">
    <div class="input-area">
      <textarea
        ref="textareaRef"
        v-model="message"
        placeholder="What's on your mind?"
        rows="1"
        @input="autoResize"
        @keydown.enter.exact="sendMessage"
      ></textarea>
    </div>
    <div class="toolbar">
      <div class="toolbar-left">
        <v-btn icon variant="text" :ripple="false" class="tool-btn" size="small">
          <v-icon size="20">mdi-plus</v-icon>
        </v-btn>
        <v-btn variant="text" :ripple="false" class="tool-btn" size="small">
          <v-icon size="18" class="mr-1">mdi-tune-variant</v-icon>
          <span class="tool-label">Tools</span>
        </v-btn>
      </div>
      <div class="toolbar-right">
        <div class="model-selector">
          <v-btn variant="text" :ripple="false" class="tool-btn" size="small"
            @click="showModelMenu = !showModelMenu">
            <span class="tool-label">{{ selectedModel.name }}</span>
            <v-icon size="18" class="ml-1">
              {{ showModelMenu ? 'mdi-chevron-up' : 'mdi-chevron-down' }}
            </v-icon>
          </v-btn>
          <Transition name="dropdown">
            <div v-if="showModelMenu" class="model-dropdown" v-click-outside="() => showModelMenu = false">
              <div
                v-for="model in models"
                :key="model.id"
                class="model-option"
                :class="{ active: model.id === selectedModel.id }"
                @click="selectModel(model)"
              >
                <div class="model-option-info">
                  <span class="model-name">{{ model.name }}</span>
                  <span class="model-desc">{{ model.description }}</span>
                </div>
                <v-icon v-if="model.id === selectedModel.id" size="18" color="#64B5F6">
                  mdi-check-circle
                </v-icon>
              </div>
            </div>
          </Transition>
        </div>
        <v-btn icon variant="text" :ripple="false" class="tool-btn" size="small" 
          @click="sendMessage" @keyup.enter="sendMessage">
          <v-icon size="20">mdi-send</v-icon>
        </v-btn>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue'
import { useChat } from '../../composables/useChat.js'

const { sendMessage: send, topicId} = useChat()
const message = ref('')
const textareaRef = ref(null)
const showModelMenu = ref(false)

const models = ref([
  { id: '1', name: 'OpenThaiGPT', description: 'By AIEAT', value: 'openthaigpt' },
  { id: '2', name: 'Pathumma', description: 'Qwen3 8b base model By NECTEC', value: 'pathumma' },
  { id: '3', name: 'Typhoons-s', description: 'By SCB10X', value: 'typhoon' },
  { id: '4', name: 'THaLLE', description: 'By KBTG', value: 'kbtg' },
])
const selectedModel = ref(models.value[0])

function selectModel(model) {
  selectedModel.value = model
  showModelMenu.value = false
}

const vClickOutside = {
  mounted(el, binding) {
    el.__clickOutside = (e) => {
      if (!el.contains(e.target)) binding.value()
    }
    setTimeout(() => document.addEventListener('click', el.__clickOutside), 0)
  },
  unmounted(el) {
    document.removeEventListener('click', el.__clickOutside)
  },
}

function autoResize() {
  const el = textareaRef.value
  if (!el) return
  // Temporarily remove height to measure true scrollHeight
  const prevHeight = el.style.height
  el.style.transition = 'none'
  el.style.height = 'auto'
  const targetHeight = Math.min(el.scrollHeight, 200)
  // Restore previous height, then animate to new height
  el.style.height = prevHeight || 'auto'
  // Force reflow so the browser registers the old height
  el.offsetHeight
  el.style.transition = 'height 0.3s ease'
  el.style.height = targetHeight + 'px'
}

function sendMessage(e) {
  e.preventDefault()
  if (!message.value.trim()) return
  send(message.value, selectedModel.value.value, topicId.value)
  message.value = ''
  nextTick(() => autoResize())
}
</script>

<style scoped>
.chatbox {
  background-color: #303030;
  border-radius: 20px;
  width: 100%;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  box-sizing: border-box;
}

.input-area {
  width: 100%;
}

.chatbox textarea {
  font-size: 16px;
  border: none;
  outline: none;
  width: 100%;
  background-color: transparent;
  color: #e0e0e0;
  resize: none;
  overflow-y: auto;
  max-height: 200px;
  line-height: 1.5;
  transition: height 0.2s ease;
  font-family: inherit;
}

.chatbox textarea::placeholder {
  color: #9e9e9e;
}

.toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.toolbar-left,
.toolbar-right {
  display: flex;
  align-items: center;
  gap: 4px;
  border: none; 
  outline: none;
}

.tool-btn {
  color: #bdbdbd !important;
  text-transform: none !important;
  letter-spacing: normal !important;
  outline: none !important;
  box-shadow: none !important;
}

.tool-btn:hover {
  color: white !important;
  background-color: #4a4a4a !important;
}

.tool-btn :deep(.v-btn__overlay),
.tool-btn :deep(.v-btn__underlay) {
  display: none;
}


.tool-label {
  font-size: 13px;
}

/* Model selector dropdown */
.model-selector {
  position: relative;
}

.model-dropdown {
  position: absolute;
  bottom: 100%;
  right: 0;
  margin-bottom: 8px;
  background-color: #2a2a2a;
  border-radius: 12px;
  min-width: 240px;
  padding: 8px 0;
  box-shadow: 0 -4px 20px rgba(0, 0, 0, 0.4);
  z-index: 10;
  transform-origin: bottom right;
}

.model-option {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  cursor: pointer;
  transition: background-color 0.15s;
}

.model-option:hover {
  background-color: #3a3a3a;
}

.model-option.active {
  background-color: #333;
}

.model-option-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.model-name {
  color: #e0e0e0;
  font-size: 14px;
  font-weight: 500;
}

.model-desc {
  color: #9e9e9e;
  font-size: 12px;
}

/* Dropdown transition */
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s ease, transform 0.15s ease;
}

.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(8px) scale(0.95);
}
</style>
