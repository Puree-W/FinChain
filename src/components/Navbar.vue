<template>
    <v-dialog :model-value="isApiLoading" persistent width="auto">
        <v-card class="progress-card" color="#333333" rounded="lg" elevation="0">
            <v-progress-circular indeterminate color="white" size="64" />
        </v-card>
    </v-dialog>
        
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
                    <div v-for="item in historyList" :key="item.id" class="chat-history-item" :class="{ active: selectedTopicId === item.id }" @click="selectTopic(item.id)">
                        <span class="history-item-name">{{ truncatedHistoryName(item.topicName) }}</span>
                        <Transition name="fade">
                            <div v-if="selectedTopicId === item.id" class="history-item-menu-wrapper">
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
                    
                </div>
            </div>
        </Transition>

        <!-- Box -->
        <v-dialog max-width="500" v-model="renameBox">
            <v-card title="Rename topic" class="rename-dialog" theme="dark">
                <v-card-text style="margin-bottom: -2rem;">
                    <v-text-field
                        class="rename-input"
                        variant="outlined"
                        base-color="#bdbdbd"
                        color="#ffffff"
                        :rules="rules"
                        density="comfortable"
                        placeholder="Enter new name"
                        v-model="newName"
                    ></v-text-field>
                </v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        class="confirm-btn"
                        @click="renameBox = false; renameTopicAction(renameTopicId, newName);"
                        text="Confirm"
                        :ripple="false"
                    ></v-btn>
                    <v-btn
                        @click="renameBox = false;"
                        class="cancel-btn"
                        text="Cancel"
                        :ripple="false"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>
        
        <v-dialog max-width="500" v-model="deleteBox">
            <v-card title="Delete history confirmation">
                <v-card-text>Are you sure you want to delete this history? This action cannot be undone.</v-card-text>
                <v-card-actions>
                    <v-spacer></v-spacer>
                    <v-btn
                        @click="deleteBox = false"
                        class="confirm-btn"
                        text="Confirm"
                        :ripple="false"
                    ></v-btn>
                    <v-btn
                        @click="deleteBox = false;"
                        class="cancel-btn"
                        text="Cancel"
                        :ripple="false"
                    ></v-btn>
                </v-card-actions>
            </v-card>
        </v-dialog>

    </div>
</template>

<script setup>
import { ref } from 'vue'
import { getHistory, renameTopicHistory, DeleteTopicHistory } from '../api/call.js'
import { useChat } from '../composables/useChat.js'

const { loadHistory } = useChat()
const renameBox = ref(null)
const deleteBox = ref(null)
const isApiLoading = ref(false)
const newName = ref(null)
const renameTopicId = ref(null)

const rules = [
    value => !!value || 'Required.',
]

function renameTopic(topicId) {
    renameTopicId.value = topicId;
    renameBox.value = true;
}

function renameTopicAction(topicId, newName) {
    isApiLoading.value = true;
    renameTopicHistory(topicId, newName).then(response => {
        if (response.success) {
            console.log("rename topic success:", response.message);
        } else {
            console.error("Failed to rename topic:", response.message);
        }
    }).catch(err => {
        console.error("Failed to rename topic:", err);
    }).finally(() => {
        isApiLoading.value = false;
        newName.value = null;
    });
}

const deleteTopic = (topicId) => {
    deleteBox.value = true;
    selectedTopicId.value = topicId;
    // isApiLoading.value = true;
    // DeleteTopicHistory.then(response => {
    //     if (response.success) {
    //         console.log("Topic deleted successfully");
    //     } else {
    //         console.error("Failed to delete topic:", response.message);
    //     }
    // }).catch(err => {
    //     console.error("Failed to delete topic:", err);
    // }).finally(() => {
    //     isApiLoading.value = false;
    // });
}


const items = [
    { title: 'Rename', action: renameTopic },
    { title: 'Delete', action: deleteTopic },
]
const props = defineProps({
    historyList: {
        type: Array,
        default: () => []
    }
})

const emit = defineEmits(['newChat'])

function truncatedHistoryName(topicName) {
    // Logic to create a new chat
    return topicName.length > 25 ? topicName.slice(0, 25) + '...' : topicName;
}

function createNewChat() {
    selectedTopicId.value = null;
    emit('newChat');
    console.log("Creating new chat...");
}

function selectTopic(topicId) {
    selectedTopicId.value = topicId;
    getHistory(topicId).then(response => {
        loadHistory(response.data.messages || [], topicId);
    }).catch(err => {
        console.error("Failed to get history:", err);
    });
}

const isExpanded = ref(false)
const selectedTopicId = ref(null)
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

.rename-dialog {
    background-color: #333333;
    color: #bdbdbd;
    padding: 8px;
}
.rename-dialog :deep(.v-messages__message) {
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


.fade-enter-active,
.fade-leave-active {
    transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
    opacity: 0;
}
.progress-card {
    padding: 24px;  

}
</style>
