<template>
  <div class="auth-gate">
    <div class="auth-card card">
      <h1 class="mb-3">🎮 QuestNav Configuration</h1>
      
      <p class="text-muted mb-3">
        Enter the authentication token from your Quest device to connect.
      </p>
      
      <form @submit.prevent="handleSubmit">
        <div class="form-group mb-2">
          <label for="token">Authentication Token</label>
          <input
            id="token"
            v-model="token"
            type="text"
            placeholder="Enter token from Unity console"
            autocomplete="off"
            :disabled="isAuthenticating"
          />
        </div>
        
        <div v-if="error" class="error-message mb-2">
          {{ error }}
        </div>
        
        <button type="submit" :disabled="!token || isAuthenticating">
          {{ isAuthenticating ? 'Connecting...' : 'Connect' }}
        </button>
      </form>
      
      <div class="help-text mt-3">
        <p class="text-muted">
          <strong>How to find your token:</strong>
        </p>
        <ol class="text-muted">
          <li>Launch the app on your Quest device</li>
          <li>Check the Unity console logs</li>
          <li>Look for "Auth Token:" in the server startup message</li>
          <li>Copy and paste the token here</li>
        </ol>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useConfigStore } from '../stores/config'

const configStore = useConfigStore()

const token = ref('')
const isAuthenticating = ref(false)
const error = ref<string | null>(null)

async function handleSubmit() {
  if (!token.value) return
  
  isAuthenticating.value = true
  error.value = null
  
  try {
    const success = await configStore.authenticate(token.value)
    
    if (!success) {
      error.value = 'Invalid token or unable to connect to server'
    }
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Authentication failed'
  } finally {
    isAuthenticating.value = false
  }
}
</script>

<style scoped>
.auth-gate {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
  background: linear-gradient(135deg, var(--bg-color) 0%, var(--bg-secondary) 100%);
}

.auth-card {
  max-width: 500px;
  width: 100%;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-weight: 500;
  color: var(--text-secondary);
}

.error-message {
  padding: 0.75rem;
  background-color: rgba(220, 53, 69, 0.1);
  border: 1px solid var(--danger-color);
  border-radius: 6px;
  color: var(--danger-color);
}

.help-text ol {
  margin-left: 1.5rem;
  margin-top: 0.5rem;
}

.help-text li {
  margin-bottom: 0.25rem;
}
</style>

