<template>
    <div class="bot-message-wrapper">
        <div v-if="thinkContent" class="bot-think">
            <p class="bot-think-text">{{ thinkContent }}</p>
        </div>
        <div v-if="visibleMessage" class="bot-message">
            <div class="bot-message-text" v-html="renderedMessage"></div>
        </div>
    </div>
</template>

<script setup>
import { computed } from 'vue'
import { marked } from 'marked'

const props = defineProps({
  message: {
    type: String,
    required: true,
    default: "This is a bot message."
  },
  time: {
    type: String,
    required: false,
    default: "2025-01-01 10:00 AM"
  }
})

const thinkContent = computed(() => {
    const match = props.message.match(/<think>([\s\S]*?)<\/think>/)
    return match ? match[1].trim() : ''
})

const visibleMessage = computed(() => {
    return props.message.replace(/<think>[\s\S]*?<\/think>/g, '').trim()
})

const renderedMessage = computed(() => {
    return marked(visibleMessage.value)
})
</script>

<style scoped>
.bot-message-wrapper {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    padding: 4px 0;
    gap: 4px;
}

.bot-think {
    background-color: #333333;
    border-radius: 8px;
    border-left: 3px solid #888888;
    max-width: 75%;
    padding: 6px 12px;
    opacity: 0.7;
    margin-top: 16px;
}

.bot-think-text {
    margin: 0;
    color: #aaaaaa;
    font-size: 0.85em;
    font-style: italic;
}

.bot-message {
    width: 100%;
    padding: 8px 0;
    margin-bottom: 16px;
}

.bot-message-text {
    margin: 0;
    color: white;
    line-height: 1.7;
}

.bot-message-text :deep(p) {
    margin: 0.4em 0;
}

.bot-message-text :deep(p:first-child) {
    margin-top: 0;
}

.bot-message-text :deep(p:last-child) {
    margin-bottom: 0;
}

.bot-message-text :deep(table) {
    border-collapse: collapse;
    width: 100%;
    margin: 0.5em 0;
}

.bot-message-text :deep(th),
.bot-message-text :deep(td) {
    border: 1px solid #666666;
    padding: 4px 8px;
    text-align: left;
}

.bot-message-text :deep(th) {
    background-color: #555555;
}

.bot-message-text :deep(h1),
.bot-message-text :deep(h2),
.bot-message-text :deep(h3) {
    margin: 0.5em 0 0.3em;
}

.bot-message-text :deep(ul),
.bot-message-text :deep(ol) {
    margin: 0.3em 0;
    padding-left: 1.5em;
}

.bot-message-text :deep(code) {
    background-color: #333333;
    padding: 1px 4px;
    border-radius: 3px;
    font-size: 0.9em;
}

.bot-message-text :deep(pre) {
    background-color: #333333;
    padding: 8px;
    border-radius: 4px;
    overflow-x: auto;
}

.bot-message-text :deep(li) {
    margin: 0.3em 0;
    line-height: 1.7;
}

.bot-message-text :deep(pre code) {
    background: none;
    padding: 0;
}
</style>
