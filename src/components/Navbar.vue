<template>
    <div class="navbar-container" :class="{ expanded: isExpanded }">
        <v-btn icon variant="text" :ripple="false" class="menu-btn" size="small" @click="isExpanded = !isExpanded">
            <v-icon size="24">mdi-menu</v-icon>
        </v-btn>

        <Transition name="fade">
            <div v-if="isExpanded" class="expanded-content">
                <v-btn variant="text" :ripple="false" class="navbar-btn" size="small" @click="createNewChat">
                    <v-icon size="24">mdi-chat-plus</v-icon>
                    <span class="navbar-text">New Chat</span>
                </v-btn>

                <div class="chat-history">
                    <p class="chat-history-label">History</p>
                    <div class="chat-history-scroll">
                    <Transition name="fade" mode="out-in">
                        <div v-if="loadingHistory" key="loading" class="history-skeleton-list">
                            <div v-for="i in 5" :key="i" class="history-skeleton-item">
                                <div class="history-skeleton-bar" :style="{ width: skeletonWidth(i) }"></div>
                            </div>
                        </div>

                        <TransitionGroup v-else key="loaded" name="history-fade" tag="div" class="history-list">
                            <div v-for="item in historyList" :key="item.id" class="chat-history-item" :class="{ active: activeTopicId === item.id }" @click="selectTopic(item.id)">
                                <span class="history-item-name">{{ truncatedHistoryName(item.topicName) }}</span>
                                <Transition name="fade">
                                    <div v-if="activeTopicId === item.id" class="history-item-menu-wrapper">
                                        <v-menu class="history-item-menu" offset-y>
                                            <template v-slot:activator="{ props }">
                                                <v-btn icon="mdi-dots-horizontal" v-bind="props" :ripple="false"
                                                class="history-item-menu-icon"></v-btn>
                                            </template>
                                            <v-list class="history-item-menu-list">
                                                <v-list-item v-for="(menuItem, i) in items" :key="i" class="history-item-menu-sublist" :ripple="false" @click="menuItem.action(item.id)">
                                                <v-list-item-title>{{ menuItem.title }}</v-list-item-title>
                                                </v-list-item>
                                            </v-list>
                                        </v-menu>
                                    </div>
                                </Transition>
                            </div>
                        </TransitionGroup>
                    </Transition>
                    </div>
                </div>
            </div>
        </Transition>

        <div class="navbar-footer">
            <v-btn
                variant="text"
                :ripple="false"
                class="navbar-footer-btn"
                :class="{ active: isConfigActive }"
                size="small"
                @click="goConfiguration"
            >
                <v-icon size="22">mdi-cog-outline</v-icon>
                <span v-if="isExpanded" class="navbar-text">Configuration</span>
            </v-btn>
        </div>

        <!-- Rename dialog -->
        <v-dialog max-width="500" v-model="renameBox" :persistent="renameLoading">
            <v-card title="Rename topic" class="action-dialog" theme="dark">
                <v-card-text style="margin-bottom: -2rem;">
                    <v-text-field
                        class="rename-input"
                        variant="outlined"
                        base-color="#bdbdbd"
                        color="#ffffff"
                        :rules="rules"
                        density="comfortable"
                        placeholder="Enter new name"
                        :disabled="renameLoading"
                        v-model="newName"
                    ></v-text-field>
                </v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        :loading="renameLoading"
                        :disabled="!newName || renameLoading"
                        @click="renameTopicAction(renameTopicId, newName)"
                        :ripple="false"
                    >
                        <template v-slot:loader>
                            <v-progress-circular indeterminate color="white" size="20" width="2" />
                        </template>
                        Confirm
                    </v-btn>
                    <v-btn
                        :disabled="renameLoading"
                        @click="renameBox = false"
                        class="cancel-btn"
                        text="Cancel"
                        :ripple="false"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <!-- Delete dialog -->
        <v-dialog max-width="500" v-model="deleteBox" :persistent="deleteLoading">
            <v-card title="Delete history confirmation" class="action-dialog" theme="dark">
                <v-card-text>Are you sure you want to delete this history? This action cannot be undone.</v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        :loading="deleteLoading"
                        :disabled="deleteLoading"
                        @click="deleteTopicAction(deleteTopicId)"
                        :ripple="false"
                    >
                        <template v-slot:loader>
                            <v-progress-circular indeterminate color="white" size="20" width="2" />
                        </template>
                        Confirm
                    </v-btn>
                    <v-btn
                        :disabled="deleteLoading"
                        @click="deleteBox = false"
                        class="cancel-btn"
                        text="Cancel"
                        :ripple="false"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

        <!-- Result dialog (success / error) -->
        <v-dialog max-width="420" v-model="resultBox">
            <v-card class="action-dialog result-card" theme="dark">
                <div class="result-body">
                    <v-icon
                        :color="resultStatus === 'success' ? '#646cff' : '#ff5252'"
                        size="48"
                    >
                        {{ resultStatus === 'success' ? 'mdi-check-circle-outline' : 'mdi-alert-circle-outline' }}
                    </v-icon>
                    <div class="result-text">
                        <p class="result-title">{{ resultStatus === 'success' ? 'Complete' : 'Error' }}</p>
                        <p class="result-message">{{ resultMessage }}</p>
                    </div>
                </div>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        text="OK"
                        :ripple="false"
                        @click="resultBox = false"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

    </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getHistory, renameTopicHistory, DeleteTopicHistory } from '../api/call.js'
import { useChat } from '../composables/useChat.js'

const { loadHistory, topicId: activeTopicId } = useChat()
const route = useRoute()
const router = useRouter()

const isConfigActive = computed(() => route.path.startsWith('/configuration'))

function goConfiguration() {
    if (isConfigActive.value) {
        router.push('/')
    } else {
        router.push('/configuration')
    }
}

const renameBox = ref(false)
const deleteBox = ref(false)
const resultBox = ref(false)

const renameLoading = ref(false)
const deleteLoading = ref(false)

const newName = ref(null)
const renameTopicId = ref(null)
const deleteTopicId = ref(null)

const resultStatus = ref('success') // 'success' | 'error'
const resultMessage = ref('')

const rules = [
    value => !!value || 'Required.',
]

function showResult(status, message) {
    resultStatus.value = status
    resultMessage.value = message
    resultBox.value = true
}

function renameTopic(topicId) {
    renameTopicId.value = topicId
    newName.value = null
    renameBox.value = true
}

function renameTopicAction(topicId, name) {
    if (!name) return
    renameLoading.value = true
    renameTopicHistory(topicId, name).then(response => {
        if (response.success) {
            renameBox.value = false
            emit('refresh-history')
            showResult('success', 'Topic renamed successfully.')
        } else {
            showResult('error', response.message || 'Failed to rename topic.')
        }
    }).catch(err => {
        showResult('error', err?.message || 'Failed to rename topic.')
    }).finally(() => {
        renameLoading.value = false
        newName.value = null
    })
}

function deleteTopic(topicId) {
    deleteTopicId.value = topicId
    deleteBox.value = true
}

function deleteTopicAction(topicId) {
    deleteLoading.value = true
    DeleteTopicHistory(topicId).then(response => {
        if (response.success) {
            deleteBox.value = false
            if (activeTopicId.value === topicId) {
                loadHistory([], null)
            }
            emit('refresh-history')
            showResult('success', 'Topic deleted successfully.')
        } else {
            showResult('error', response.message || 'Failed to delete topic.')
        }
    }).catch(err => {
        showResult('error', err?.message || 'Failed to delete topic.')
    }).finally(() => {
        deleteLoading.value = false
    })
}


const items = [
    { title: 'Rename', action: renameTopic },
    { title: 'Delete', action: deleteTopic },
]
const props = defineProps({
    historyList: {
        type: Array,
        default: () => []
    },
    loadingHistory: {
        type: Boolean,
        default: false,
    },
})

// Varied widths give skeleton rows the feel of real titles rather than a uniform block.
const SKELETON_WIDTHS = ['85%', '60%', '75%', '90%', '55%']
function skeletonWidth(i) {
    return SKELETON_WIDTHS[(i - 1) % SKELETON_WIDTHS.length]
}

const emit = defineEmits(['newChat', 'refresh-history'])

function truncatedHistoryName(topicName) {
    return topicName.length > 25 ? topicName.slice(0, 25) + '...' : topicName
}

function createNewChat() {
    emit('newChat')
}

function selectTopic(topicId) {
    getHistory(topicId).then(response => {
        loadHistory(response.data.messages || [], topicId)
    }).catch(err => {
        console.error("Failed to get history:", err)
    })
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

/* Expanded content fills the space between the menu button and the footer.
   New Chat stays pinned at the top; only the history list scrolls. */
.expanded-content {
    display: flex;
    flex-direction: column;
    gap: 8px;
    width: 100%;
    margin-top: 8px;
    flex: 1 1 auto;
    min-height: 0;
    overflow: hidden;
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
    flex-shrink: 0;
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
    flex: 1 1 auto;
    min-height: 0;
}

.chat-history-label {
    font-size: 16px;
    color: #bdbdbd;
    margin: 0;
    padding-bottom: 4px;
    flex-shrink: 0;
}

/* Only this region scrolls — the History label and the New Chat button above stay put. */
.chat-history-scroll {
    flex: 1 1 auto;
    min-height: 0;
    overflow-y: auto;
    overflow-x: hidden;
    /* Slim, theme-aware scrollbar so the rail stays clean. */
    scrollbar-width: thin;
    scrollbar-color: #4a4a4a transparent;
}

.chat-history-scroll::-webkit-scrollbar {
    width: 6px;
}

.chat-history-scroll::-webkit-scrollbar-track {
    background: transparent;
}

.chat-history-scroll::-webkit-scrollbar-thumb {
    background-color: #4a4a4a;
    border-radius: 3px;
}

.chat-history-scroll::-webkit-scrollbar-thumb:hover {
    background-color: #5a5a5a;
}

.chat-history-item {
    color: #bdbdbd;
    font-size: 16px;
    padding: 8px 0;
    border-radius: 8px;
    cursor: pointer;
    transition: background-color 0.2s ease;
    white-space: nowrap;
    display: flex;
    flex-direction: row;
    align-items: center;
}

.chat-history-item:hover,
.chat-history-item.active {
    color: white;
    background-color: #4a4a4a;
}
.history-item-name {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
}
.history-item-menu-icon{
    margin-left: auto;
    display: flex;
    justify-content: end;
    align-items: end;
    color: #bdbdbd !important;
    background-color: #4a4a4a;
}
.history-item-menu-icon{
    width: 26px !important;
    height: 26px !important;
    min-width: 26px !important;
    border-radius: 50% !important;
    border: none !important;
    outline: none !important;
    box-shadow: none !important;
}
.history-item-menu-icon:hover {
    color: white !important;
}
.history-item-menu-icon :deep(.v-btn__overlay),
.history-item-menu-icon :deep(.v-btn__underlay) {
  display: none;
}

.history-item-menu-list{
    background-color: #4a4a4a !important;
    color: #bdbdbd !important;
}
.history-item-menu-sublist:hover {
  background-color: #3a3a3a !important;
}
.history-item-menu-sublist:active {
  background-color: #3a3a3a !important;
}
.history-item-menu-sublist :deep(.v-list-item__overlay) {
  display: none;
}

/* Footer with the Configuration gear icon — pinned to the bottom. margin-top:auto
   keeps it down when the navbar is collapsed (no expanded-content). flex-shrink:0
   keeps it from being squeezed by the scrolling history above. */
.navbar-footer {
    margin-top: auto;
    width: 100%;
    padding-top: 8px;
    border-top: 1px solid #404040;
    flex-shrink: 0;
}

.navbar-footer-btn {
    color: #bdbdbd !important;
    text-transform: none !important;
    letter-spacing: normal !important;
    outline: none !important;
    box-shadow: none !important;
    border-radius: 8px !important;
    width: 100% !important;
    min-width: 0 !important;
    justify-content: flex-start !important;
    padding: 0 8px !important;
}

.navbar-footer-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.navbar-footer-btn.active {
    color: #ffffff !important;
    background-color: rgba(100, 108, 255, 0.18) !important;
}

.navbar-footer-btn.active :deep(.v-icon) {
    color: #646cff !important;
}

.navbar-footer-btn :deep(.v-btn__overlay),
.navbar-footer-btn :deep(.v-btn__underlay) {
    display: none;
}

/* Shared dialog skin (rename / delete / result) */
.action-dialog {
    background-color: #333333;
    color: #bdbdbd;
    padding: 8px;
}
.action-dialog :deep(.v-messages__message) {
    color: #ff8a80;
}

:deep(.rename-input .v-field__input) {
    font-size: 16px !important;
    line-height: 1.3 !important;
    color: #e6e6e6 !important;
}

:deep(.rename-input input::placeholder) {
    font-size: 16px !important;
    color: #9a9a9a !important;
    opacity: 1 !important;
}

.confirm-btn,
.cancel-btn {
    outline: none !important;
    box-shadow: none !important;
    border: none !important;
}

.confirm-btn:focus,
.confirm-btn:focus-visible,
.cancel-btn:focus,
.cancel-btn:focus-visible {
    outline: none !important;
    box-shadow: none !important;
    border: none !important;
}

.confirm-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.confirm-btn :deep(.v-btn__overlay),
.confirm-btn :deep(.v-btn__underlay) {
    display: none;
}

.cancel-btn:hover {
    color: white !important;
    background-color: #4a4a4a !important;
}

.cancel-btn :deep(.v-btn__overlay),
.cancel-btn :deep(.v-btn__underlay) {
    display: none;
}

/* Result dialog */
.result-card {
    padding: 20px;
}
.result-body {
    display: flex;
    align-items: center;
    gap: 16px;
    padding: 8px 8px 16px 8px;
}
.result-text {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.result-title {
    font-size: 18px;
    font-weight: 500;
    color: #e6e6e6;
    margin: 0;
}
.result-message {
    font-size: 14px;
    color: #bdbdbd;
    margin: 0;
}

.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}

/* History list reveal: items fade in one-by-one and fade out on remove */
.history-list {
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.history-fade-enter-active {
    transition: opacity 0.35s ease, transform 0.35s ease;
}
.history-fade-leave-active {
    transition: opacity 0.2s ease, transform 0.2s ease;
    position: absolute;
}
.history-fade-enter-from {
    opacity: 0;
    transform: translateY(-4px);
}
.history-fade-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}

/* Skeleton placeholders shown while history is loading */
.history-skeleton-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px 0;
}
.history-skeleton-item {
    height: 20px;
    display: flex;
    align-items: center;
}
.history-skeleton-bar {
    height: 12px;
    border-radius: 6px;
    background: linear-gradient(
        90deg,
        rgba(255, 255, 255, 0.05) 0%,
        rgba(255, 255, 255, 0.14) 50%,
        rgba(255, 255, 255, 0.05) 100%
    );
    background-size: 200% 100%;
    animation: history-skeleton-shimmer 1.2s ease-in-out infinite;
}
@keyframes history-skeleton-shimmer {
    0% {
        background-position: 200% 0;
    }
    100% {
        background-position: -200% 0;
    }
}
</style>
