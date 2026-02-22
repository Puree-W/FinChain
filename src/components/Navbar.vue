<template>
    <div class="navbar-container" :class="{ expanded: isExpanded }">
        <v-btn icon variant="text" :ripple="false" class="menu-btn" size="small" @click="isExpanded = !isExpanded">
            <v-icon size="24">mdi-menu</v-icon>
        </v-btn>

        <Transition name="fade">
            <div v-if="isExpanded" class="expanded-content">
                <v-btn variant="text" :ripple="false" class="navbar-btn" size="small">
                    <v-icon size="24">mdi-chat-plus</v-icon>
                    <span class="navbar-text">New Chat</span>
                </v-btn>

                <div class="chat-history">
                    <p class="chat-history-label">History</p>
                    <div v-for="item in historyList" :key="item.id" class="chat-history-item" @click="selectHistory(item.id)">
                        {{ truncatedHistoryName(item.topicName) }}
                    </div>
                </div>
            </div>
        </Transition>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import { getHistory } from '../api/call.js'
import { useChat } from '../composables/useChat.js'

const { loadHistory } = useChat()

const props = defineProps({
    historyList: {
        type: Array,
        default: () => []
    }
})

function truncatedHistoryName(topicName) {
    // Logic to create a new chat
    return topicName.length > 25 ? topicName.slice(0, 25) + '...' : topicName;
}


function createNewChat() {
    // Logic to create a new chat
    console.log("Creating new chat...");
}

function selectHistory(topicId) {
    getHistory(topicId).then(data => {
        loadHistory(data.messages || [], topicId);
    }).catch(err => {
        console.error("Failed to get history:", err);
    });
}
const isExpanded = ref(false)
</script>

<style scoped>
.navbar-container {
    width: 56px;
    height: 100%;
    background-color: #333333;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    flex-shrink: 0;
    transition: width 0.3s ease;
    padding: 8px;
    box-sizing: border-box;
    overflow: hidden;
}

.navbar-container.expanded {
    width: 240px;
}

/* Menu button - fixed size circle, never moves */
.menu-btn {
    color: #bdbdbd !important;
    width: 40px !important;
    height: 40px !important;
    min-width: 40px !important;
    border-radius: 50% !important;
    border: none !important;
    outline: none !important;
    box-shadow: none !important;
    flex-shrink: 0;
}

.menu-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.menu-btn :deep(.v-btn__overlay),
.menu-btn :deep(.v-btn__underlay) {
    display: none;
}

/* Expanded content aligned to same x as menu icon */
.expanded-content {
    display: flex;
    flex-direction: column;
    gap: 8px;
    width: 100%;
    margin-top: 8px;
}

.navbar-btn {
    color: #bdbdbd !important;
    text-transform: none !important;
    letter-spacing: normal !important;
    outline: none !important;
    box-shadow: none !important;
    width: 100% !important;
    justify-content: flex-start !important;
    padding: 0 8px !important;
    min-width: 0 !important;
    border-radius: 8px !important;
}

.navbar-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.navbar-btn :deep(.v-btn__overlay),
.navbar-btn :deep(.v-btn__underlay) {
    display: none;
}

.navbar-text {
    margin-left: 16px;
    font-size: 16px;
    white-space: nowrap;
}

.chat-history {
    display: flex;
    flex-direction: column;
    gap: 2px;
    width: 100%;
    padding: 0 8px;
    margin-top: 8px;
}

.chat-history-label {
    font-size: 1ุ6px;
    color: #bdbdbd;
    margin: 0;
    padding-bottom: 4px;
}

.chat-history-item {
    color: #bdbdbd;
    font-size: 16px;
    padding: 8px 0;
    border-radius: 8px;
    cursor: pointer;
    transition: background-color 0.2s ease;
    white-space: nowrap;
}

.chat-history-item:hover {
    color: white;
    background-color: #4a4a4a;
}

/* Transition */
.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}
</style>
