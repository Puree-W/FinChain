<template>
  <Transition name="fade">
    <v-btn v-show="!isAtBottom" class="circle" :ripple="false" @click="scrollToBottom">
        <v-icon size="20" class="circle-icon">mdi-arrow-down</v-icon>
    </v-btn>
  </Transition>
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
            <span class="tool-label">{{ buttonLabel }}</span>
            <v-icon size="18" class="ml-1">
              {{ showModelMenu ? 'mdi-chevron-up' : 'mdi-chevron-down' }}
            </v-icon>
          </v-btn>
          <Transition name="dropdown">
            <div v-if="showModelMenu" class="model-dropdown" v-click-outside="() => showModelMenu = false">
              <template v-if="templates.length">
                <div
                  v-for="t in templates.filter(t => t.activeFlag)"
                  :key="t.id"
                  class="model-option"
                  :class="{ active: t.id === selectedTemplate?.id }"
                  @click="pickTemplate(t)"
                >
                  <div class="model-option-info">
                    <span class="model-name">
                      {{ t.name }}
                      <span v-if="t.isDefault" class="default-pill">default</span>
                    </span>
                    <span class="model-desc">
                      {{ t.aiConfigName || '—' }} · temp {{ Number(t.temperature).toFixed(2) }} · max {{ t.maxTokens }}
                    </span>
                  </div>
                  <v-icon v-if="t.id === selectedTemplate?.id" size="18" color="#64B5F6">
                    mdi-check-circle
                  </v-icon>
                </div>
              </template>
              <div v-else class="model-empty">
                <p class="model-empty-text">No templates yet.</p>
                <router-link to="/configuration/model" class="model-empty-link" @click="showModelMenu = false">
                  Create one in Configuration →
                </router-link>
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
import { ref, computed, nextTick } from 'vue'
import { useChat } from '../../composables/useChat.js'

const { sendMessage: send, isAtBottom, scrollToBottom, templates, selectedTemplate, selectTemplate } = useChat()
const message = ref('')
const textareaRef = ref(null)
const showModelMenu = ref(false)

const buttonLabel = computed(() => selectedTemplate.value?.name ?? 'Select template')

function pickTemplate(t) {
  selectTemplate(t)
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
  send(message.value)
  message.value = ''
  nextTick(() => autoResize())
}
</script>

<style scoped>

.circle{
  width: 26px !important;
  height: 26px !important;
  min-width: 26px !important;
  padding: 0 !important;
  background-color: white !important;
  border-radius: 50% !important;
  z-index: 3;
  display: flex;
  position: absolute;
  top: -30px;
  left: 50%;
  transform: translateX(-50%);
  outline: none !important;
}

.circle-icon {
  color: #000000;
}

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

.circle:hover {
  color: white !important;
}

.circle :deep(.v-btn__overlay),
.circle :deep(.v-btn__underlay) {
  display: none;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
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
  min-width: 260px;
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
  min-width: 0;
}

.model-name {
  color: #e0e0e0;
  font-size: 14px;
  font-weight: 500;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.default-pill {
  font-size: 10px;
  background-color: rgba(100, 108, 255, 0.18);
  color: #c2c6ff;
  padding: 1px 6px;
  border-radius: 999px;
  font-weight: 500;
}

.model-desc {
  color: #9e9e9e;
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.model-empty {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 4px;
  align-items: flex-start;
}

.model-empty-text {
  color: #bdbdbd;
  font-size: 13px;
  margin: 0;
}

.model-empty-link {
  color: #c2c6ff;
  font-size: 13px;
  text-decoration: none;
}

.model-empty-link:hover {
  color: #ffffff;
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
