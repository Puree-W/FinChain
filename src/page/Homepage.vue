<template>
    <div class="layout">
        <Navbar :historyList="historyList" />
        <div class="page">
            <div class="scroll-area">
                <div class="content-column">
                    <HistoryConversation/>
                </div>
            </div>
            <div class="chat-area">
                <div class="content-column">
                    <Chatbox />
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import Chatbox from '../components/chat/Chatbox.vue'
import HistoryConversation from '../components/chat/HistoryConversation.vue'
import Navbar from '../components/Navbar.vue'
import { getAllHistory } from '../api/call.js'

const historyList = ref([])

onMounted(() => {
    getAllHistory().then(data => {
        historyList.value = data;
    }).catch(err => {
        console.error("Failed to get history:", err);
    });
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
    position: relative;
    flex: 1;
    min-width: 0;
    height: 100vh;
}

.scroll-area {
    position: relative;
    height: 100%;
    overflow-y: auto;
}

.chat-area {
    position: absolute;
    z-index: 2;
    bottom: 0;
    left: 0;
    right: 0;
    display: flex;
    justify-content: center;
    pointer-events: none;
}

.chat-area .content-column {
    width: 61%;
    position: relative;
    pointer-events: auto;
    padding-top: 10px;
    padding-bottom: 40px;
    background-color: #222222;
}

.chat-area .content-column::before {
    content: '';
    position: absolute;
    top: -20px;
    left: 0;
    right: 0;
    height: 20px;
    background: linear-gradient(transparent, #222222);
    pointer-events: none;
}

.content-column {
    width: 60%;
}

.scroll-area .content-column {
    margin: 20px auto 0;
    padding-bottom: 160px;
}
</style>
