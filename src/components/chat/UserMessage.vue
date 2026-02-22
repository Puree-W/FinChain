<template>
    <div class="user-message-wrapper">
        <div class="user-message">
            <p class="user-message-text">{{ message }}</p>
        </div>
        
    </div>
</template>

<script setup>
defineProps({
  message: {
    type: String,
    required: true,
    default: "This is a user message."
  },
 time: {
    type: String,
    required: false,
    default: "10:00 AM"
  }
})
</script>

<style scoped>

.user-message-wrapper {
    display: flex;
    justify-content: flex-end;
    padding: 4px 0;
}

.user-message {
    background-color: #444444;
    border-radius: 8px;
    max-width: 75%;
    padding: 8px 14px;
}

.user-message-text {
    margin: 0;
    color: white;
}


</style>
