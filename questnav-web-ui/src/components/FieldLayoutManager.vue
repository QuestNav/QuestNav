<template>
  <!--
    Modal for creating, editing, renaming, and deleting custom AprilTag field-layout
    JSONs. Bundled layouts are read-only and shown for reference; custom layouts get
    Edit / Rename / Delete actions.

    The "Save" button writes via POST /api/apriltag-field-layouts (same endpoint for
    create and overwrite). The textarea is the user's only entry path for custom JSON;
    file upload is intentionally not supported.
  -->
  <div class="modal-overlay" @click="closeIfBackdrop">
    <div class="modal-content card" @click.stop>
      <div class="modal-header">
        <h2>Custom Field Layouts</h2>
        <button @click="emitClose" class="close-button" type="button" aria-label="Close">×</button>
      </div>

      <p class="modal-subtitle">
        Paste an AprilTag layout JSON below to add it to the dropdown. Bundled layouts
        are read-only; create a custom layout if you need a different field configuration.
      </p>

      <div v-if="loading" class="loading-row">
        <div class="spinner-small"></div>
        <span>Loading layouts...</span>
      </div>

      <div v-else class="layout-grid">
        <!-- Existing layouts -->
        <div v-if="layouts.length === 0" class="empty-row">No layouts available.</div>
        <div v-for="entry in layouts" :key="entry.fileName" class="layout-row">
          <div class="layout-info">
            <span class="layout-name">{{ entry.displayName }}</span>
            <span class="layout-meta">
              <span :class="['layout-source', entry.source]">{{ entry.source }}</span>
              <span class="layout-tag-count">{{ entry.tagCount }} tags</span>
              <span class="layout-filename">{{ entry.fileName }}</span>
            </span>
          </div>
          <div class="layout-actions" v-if="entry.source === 'custom'">
            <button @click="beginEdit(entry)" type="button" class="action-btn">Edit</button>
            <button @click="beginRename(entry)" type="button" class="action-btn">Rename</button>
            <button @click="confirmDelete(entry)" type="button" class="action-btn danger-btn">Delete</button>
          </div>
        </div>
      </div>

      <hr class="divider" />

      <!-- Create / edit form -->
      <h3>{{ editingLayout ? `Edit "${editingLayout.fileName}"` : 'New Custom Layout' }}</h3>

      <div class="form-row">
        <label for="layout-name">Name:</label>
        <input id="layout-name" type="text" v-model="formName" :disabled="!!editingLayout"
               placeholder="e.g., team-1234-practice"
               maxlength="64" />
      </div>
      <p class="form-hint">
        Letters, numbers, '.', '_', '-'. The server appends ".json" automatically.
      </p>

      <div class="form-row">
        <label for="layout-content">JSON content:</label>
        <textarea id="layout-content" v-model="formContent" rows="14" spellcheck="false"
                  placeholder='{"tags": [...], "field": {...}}'></textarea>
      </div>

      <div v-if="formError" class="form-error">{{ formError }}</div>
      <div v-if="formSuccess" class="form-success">{{ formSuccess }}</div>

      <div class="modal-footer">
        <button v-if="editingLayout" @click="cancelEdit" type="button" class="action-btn">
          Cancel
        </button>
        <button @click="saveLayout" type="button" class="primary-btn" :disabled="saving">
          {{ saving ? 'Saving...' : (editingLayout ? 'Save Changes' : 'Create Layout') }}
        </button>
      </div>
    </div>

    <!-- Rename modal-in-modal. Kept inline so the user does not lose the form state
         in the parent modal while renaming. -->
    <div v-if="renamingLayout" class="rename-overlay" @click.stop>
      <div class="rename-card card">
        <h3>Rename "{{ renamingLayout.fileName }}"</h3>
        <input type="text" v-model="renameTo" placeholder="new-name" maxlength="64" />
        <div v-if="renameError" class="form-error">{{ renameError }}</div>
        <div class="rename-actions">
          <button @click="renamingLayout = null" type="button" class="action-btn">Cancel</button>
          <button @click="performRename" type="button" class="primary-btn" :disabled="renaming">
            {{ renaming ? 'Renaming...' : 'Rename' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { configApi } from '../api/config'
import type { AprilTagFieldLayoutEntry } from '../types'

const emit = defineEmits<{
  (e: 'close'): void
  (e: 'changed'): void
}>()

const loading = ref(true)
const layouts = ref<AprilTagFieldLayoutEntry[]>([])

const formName = ref('')
const formContent = ref('')
const formError = ref<string | null>(null)
const formSuccess = ref<string | null>(null)
const saving = ref(false)
const editingLayout = ref<AprilTagFieldLayoutEntry | null>(null)

const renamingLayout = ref<AprilTagFieldLayoutEntry | null>(null)
const renameTo = ref('')
const renameError = ref<string | null>(null)
const renaming = ref(false)

async function refresh() {
  loading.value = true
  try {
    layouts.value = await configApi.getAprilTagFieldLayouts()
  } catch (err) {
    formError.value = err instanceof Error ? err.message : String(err)
  } finally {
    loading.value = false
  }
}

function emitClose() {
  emit('close')
}

function closeIfBackdrop(event: MouseEvent) {
  if (event.target === event.currentTarget) {
    emitClose()
  }
}

async function beginEdit(entry: AprilTagFieldLayoutEntry) {
  formError.value = null
  formSuccess.value = null
  editingLayout.value = entry
  formName.value = entry.fileName.endsWith('.json')
    ? entry.fileName.substring(0, entry.fileName.length - 5)
    : entry.fileName
  formContent.value = ''
  try {
    formContent.value = await configApi.getAprilTagFieldLayoutContent(entry.fileName)
  } catch (err) {
    formError.value = err instanceof Error ? err.message : String(err)
    editingLayout.value = null
    formName.value = ''
  }
}

function cancelEdit() {
  editingLayout.value = null
  formName.value = ''
  formContent.value = ''
  formError.value = null
  formSuccess.value = null
}

async function saveLayout() {
  formError.value = null
  formSuccess.value = null
  if (!formName.value.trim()) {
    formError.value = 'Name is required.'
    return
  }
  if (!formContent.value.trim()) {
    formError.value = 'JSON content is required.'
    return
  }
  saving.value = true
  try {
    const result = await configApi.saveCustomAprilTagFieldLayout(formName.value, formContent.value)
    formSuccess.value = `Saved ${result.fileName} (${result.tagCount} tags).`
    if (!editingLayout.value) {
      // After a "new" save, clear the form so the user can keep adding more.
      formName.value = ''
      formContent.value = ''
    } else {
      // After an "edit" save, leave the form populated so the user can keep iterating.
    }
    await refresh()
    emit('changed')
  } catch (err) {
    formError.value = err instanceof Error ? err.message : String(err)
  } finally {
    saving.value = false
  }
}

async function confirmDelete(entry: AprilTagFieldLayoutEntry) {
  if (!confirm(`Delete custom layout "${entry.fileName}"? This cannot be undone.`)) {
    return
  }
  try {
    const result = await configApi.deleteCustomAprilTagFieldLayout(entry.fileName)
    if (result.fellBackTo) {
      formSuccess.value = `Deleted ${result.deletedFileName}. Falling back to ${result.fellBackTo}.`
    } else {
      formSuccess.value = `Deleted ${result.deletedFileName}.`
    }
    if (editingLayout.value?.fileName === entry.fileName) {
      cancelEdit()
    }
    await refresh()
    emit('changed')
  } catch (err) {
    formError.value = err instanceof Error ? err.message : String(err)
  }
}

function beginRename(entry: AprilTagFieldLayoutEntry) {
  renamingLayout.value = entry
  renameTo.value = entry.fileName.endsWith('.json')
    ? entry.fileName.substring(0, entry.fileName.length - 5)
    : entry.fileName
  renameError.value = null
}

async function performRename() {
  if (!renamingLayout.value) return
  if (!renameTo.value.trim()) {
    renameError.value = 'New name is required.'
    return
  }
  renaming.value = true
  try {
    const result = await configApi.renameCustomAprilTagFieldLayout(
      renamingLayout.value.fileName,
      renameTo.value
    )
    formSuccess.value = `Renamed ${result.oldFileName} -> ${result.newFileName}.`
    renamingLayout.value = null
    await refresh()
    emit('changed')
  } catch (err) {
    renameError.value = err instanceof Error ? err.message : String(err)
  } finally {
    renaming.value = false
  }
}

onMounted(refresh)
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1.5rem;
  z-index: 1000;
  backdrop-filter: blur(4px);
}

.modal-content {
  background: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 1.75rem;
  width: 100%;
  max-width: 760px;
  max-height: 92vh;
  overflow-y: auto;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.modal-subtitle {
  color: var(--text-secondary);
  font-size: 0.9rem;
  margin-bottom: 1.25rem;
}

.close-button {
  background: transparent;
  border: none;
  color: var(--text-secondary);
  font-size: 1.75rem;
  line-height: 1;
  cursor: pointer;
  padding: 0 0.4rem;
}

.close-button:hover {
  color: var(--text-primary);
}

.layout-grid {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.layout-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.6rem 0.85rem;
  background: var(--bg-tertiary);
  border-left: 3px solid var(--primary-color);
  border-radius: 6px;
}

.layout-info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1;
  min-width: 0;
}

.layout-name {
  font-weight: 600;
  color: var(--text-primary);
}

.layout-meta {
  display: flex;
  gap: 0.6rem;
  align-items: center;
  font-size: 0.8rem;
  color: var(--text-secondary);
  flex-wrap: wrap;
}

.layout-source {
  display: inline-block;
  padding: 0.1rem 0.45rem;
  border-radius: 4px;
  font-weight: 700;
  text-transform: uppercase;
  font-size: 0.7rem;
}

.layout-source.bundled {
  background: rgba(76, 175, 80, 0.18);
  color: var(--success-color);
}

.layout-source.custom {
  background: rgba(51, 161, 253, 0.2);
  color: var(--primary-color);
}

.layout-filename {
  font-family: monospace;
  font-size: 0.75rem;
  opacity: 0.85;
}

.layout-actions {
  display: flex;
  gap: 0.4rem;
  flex-shrink: 0;
}

.action-btn {
  padding: 0.35rem 0.75rem;
  border-radius: 4px;
  border: 1px solid var(--border-color);
  background: var(--card-bg);
  color: var(--text-primary);
  font-weight: 500;
  cursor: pointer;
  font-size: 0.85rem;
}

.action-btn:hover {
  border-color: var(--primary-color);
  color: var(--primary-color);
}

.action-btn.danger-btn {
  color: var(--danger-color);
}

.action-btn.danger-btn:hover {
  border-color: var(--danger-color);
  background: rgba(220, 53, 69, 0.1);
}

.divider {
  border: none;
  border-top: 1px solid var(--border-color);
  margin: 1.25rem 0 1rem;
}

.form-row {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  margin-bottom: 0.75rem;
}

.form-row label {
  font-weight: 500;
  font-size: 0.9rem;
}

.form-row input,
.form-row textarea {
  padding: 0.55rem 0.65rem;
  border: 1px solid var(--border-color);
  border-radius: 4px;
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-family: monospace;
  font-size: 0.9rem;
}

.form-row textarea {
  resize: vertical;
  min-height: 220px;
  white-space: pre;
  overflow-x: auto;
}

.form-hint {
  font-size: 0.78rem;
  color: var(--text-secondary);
  margin-top: -0.5rem;
  margin-bottom: 1rem;
}

.form-error {
  background: rgba(220, 53, 69, 0.12);
  border: 1px solid var(--danger-color);
  color: var(--danger-color);
  padding: 0.6rem 0.85rem;
  border-radius: 4px;
  font-size: 0.85rem;
  margin-bottom: 0.75rem;
}

.form-success {
  background: rgba(76, 175, 80, 0.12);
  border: 1px solid var(--success-color);
  color: var(--success-color);
  padding: 0.6rem 0.85rem;
  border-radius: 4px;
  font-size: 0.85rem;
  margin-bottom: 0.75rem;
}

.modal-footer {
  display: flex;
  justify-content: flex-end;
  gap: 0.6rem;
  margin-top: 0.5rem;
}

.primary-btn {
  padding: 0.55rem 1.2rem;
  border-radius: 4px;
  border: 1px solid var(--primary-color);
  background: var(--primary-color);
  color: white;
  font-weight: 600;
  cursor: pointer;
}

.primary-btn:hover:not(:disabled) {
  background: var(--primary-dark, var(--primary-color));
}

.primary-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.loading-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 1rem;
  color: var(--text-secondary);
}

.spinner-small {
  width: 14px;
  height: 14px;
  border: 2px solid var(--border-color);
  border-top-color: var(--primary-color);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.empty-row {
  padding: 0.85rem;
  color: var(--text-secondary);
  font-style: italic;
  background: var(--bg-tertiary);
  border-radius: 6px;
}

.rename-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1100;
}

.rename-card {
  background: var(--card-bg);
  border: 1px solid var(--border-color);
  border-radius: 10px;
  padding: 1.5rem;
  width: 100%;
  max-width: 420px;
}

.rename-card input {
  width: 100%;
  padding: 0.55rem 0.65rem;
  border: 1px solid var(--border-color);
  border-radius: 4px;
  background: var(--bg-tertiary);
  color: var(--text-primary);
  font-family: monospace;
  margin: 0.85rem 0 0.5rem;
}

.rename-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.6rem;
  margin-top: 0.85rem;
}
</style>
