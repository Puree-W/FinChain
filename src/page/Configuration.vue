<template>
    <div class="layout">
        <Navbar :historyList="historyList" :loadingHistory="historyLoading" @new-chat="goNewChat" @refresh-history="refreshHistory" />
        <div class="page">
            <header class="config-header">
                <h1 class="config-title">
                    <v-icon size="22" class="header-icon">mdi-cog-outline</v-icon>
                    Configuration
                </h1>
                <p class="config-subtitle">Manage how FinChain talks to LLM providers and the templates you use in chat.</p>
            </header>

            <div class="config-body">
                <aside class="config-sidebar">
                    <router-link
                        v-for="item in items"
                        :key="item.to"
                        :to="item.to"
                        class="config-sidebar-item"
                        :class="{ active: isActive(item.to) }"
                    >
                        <v-icon size="20" class="sidebar-icon">{{ item.icon }}</v-icon>
                        <div class="sidebar-text">
                            <span class="sidebar-label">{{ item.label }}</span>
                            <span class="sidebar-hint">{{ item.hint }}</span>
                        </div>
                    </router-link>
                </aside>

                <section class="config-content">
                    <router-view />
                </section>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Navbar from '../components/Navbar.vue'
import { getAllHistory } from '../api/call.js'
import { useChat } from '../composables/useChat.js'

const route = useRoute()
const router = useRouter()
const { loadHistory, loadTemplates } = useChat()

const historyList = ref([])
const historyLoading = ref(true)

const items = [
    { to: '/configuration/llm', label: 'LLM Configuration', hint: 'Endpoints & API keys', icon: 'mdi-server-network' },
    { to: '/configuration/model', label: 'Model Setting', hint: 'Templates & base params', icon: 'mdi-tune-vertical' },
]

function isActive(to) {
    return route.path === to || route.path.startsWith(to + '/')
}

function refreshHistory() {
    historyLoading.value = true
    getAllHistory().then(response => {
        historyList.value = response.data
    }).catch(err => {
        console.error('Failed to get history:', err)
    }).finally(() => {
        historyLoading.value = false
    })
}

function goNewChat() {
    loadHistory([], null)
    router.push('/')
}

onMounted(() => {
    refreshHistory()
    // Keep templates fresh so chat picks up edits made on this page.
    loadTemplates()
})
</script>

<style scoped>
.layout {
    display: flex;
    flex-direction: row;
    height: 100vh;
    width: 100%;
}

.page {
    flex: 1;
    min-width: 0;
    height: 100vh;
    overflow-y: auto;
    background-color: #222222;
    padding: 32px 40px 80px;
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
}

.config-header {
    margin-bottom: 24px;
}

.config-title {
    display: flex;
    align-items: center;
    gap: 8px;
    color: rgba(255, 255, 255, 0.87);
    font-size: 24px;
    font-weight: 600;
    margin: 0 0 6px 0;
}

.header-icon {
    color: #646cff;
}

.config-subtitle {
    color: #9e9e9e;
    font-size: 14px;
    margin: 0;
}

.config-body {
    display: flex;
    gap: 24px;
    flex: 1;
    min-height: 0;
}

.config-sidebar {
    width: 240px;
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    gap: 4px;
    background-color: #2a2a2a;
    border-radius: 14px;
    padding: 12px;
    align-self: flex-start;
}

.config-sidebar-item {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 12px;
    padding: 10px 12px;
    border-radius: 10px;
    color: #bdbdbd;
    text-decoration: none;
    transition: background-color 0.2s ease, color 0.2s ease;
    cursor: pointer;
}

.config-sidebar-item:hover {
    background-color: #3a3a3a;
    color: rgba(255, 255, 255, 0.87);
}

.config-sidebar-item.active {
    background-color: rgba(100, 108, 255, 0.18);
    color: #ffffff;
}

.config-sidebar-item.active .sidebar-icon {
    color: #646cff;
}

.sidebar-icon {
    color: #9e9e9e;
    flex-shrink: 0;
}

.sidebar-text {
    display: flex;
    flex-direction: column;
    line-height: 1.2;
}

.sidebar-label {
    font-size: 14px;
    font-weight: 500;
}

.sidebar-hint {
    font-size: 12px;
    color: #9e9e9e;
    margin-top: 2px;
}

.config-content {
    flex: 1;
    min-width: 0;
    background-color: #2a2a2a;
    border-radius: 14px;
    padding: 24px;
    color: rgba(255, 255, 255, 0.87);
    overflow: hidden;
}

@media (max-width: 900px) {
    .config-body {
        flex-direction: column;
    }
    .config-sidebar {
        width: 100%;
        flex-direction: row;
    }
    .config-sidebar-item {
        flex: 1;
    }
}
</style>
