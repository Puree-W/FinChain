<template>
    <div class="conversation-messages">
        <div v-if="messages.length === 0">
            <div class="welcome-title">
                <h1 style="font-size: 3rem;">Welcome!</h1>
                <p>LLM knowledge manage framework. feel free to ask!</p>
            </div>
        </div>
        <div v-else>
            <v-list-item style="padding: 0;" class="message-item"
                v-for="(msg, index) in messages" :key="index">
                <userMessage v-if="msg.role === 'U' || msg.role === 'user'" :message="msg.content" />
                <botMessage v-else :message="msg.content" :loading="isLoading && index === messages.length - 1" />
            </v-list-item>
        </div>
        <div ref="bottomAnchor"></div>
    </div>
</template>

<script setup>
import { ref, watch, nextTick } from 'vue'
import userMessage from './UserMessage.vue'
import botMessage from './BotMessage.vue'
import { useChat } from '../../composables/useChat.js'
const { messages, topicId, isLoading } = useChat()

const bottomAnchor = ref(null)

function scrollToBottom() {
    nextTick(() => {
        bottomAnchor.value?.scrollIntoView({ behavior: isLoading.value ? 'instant' : 'smooth' })
    })
}

watch(messages, scrollToBottom, { deep: true })
</script>

<style scoped>
.conversation-messages {
    width: 100%;
    box-sizing: border-box;
}
.welcome-title {
    text-align: center;
    margin-top: 30%;
}
.message-item {
    margin-bottom: 8px;
}
</style>