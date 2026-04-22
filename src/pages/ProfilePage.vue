<template>
  <div class="page-container">
    <div class="ambient-glow top-left"></div>
    <div class="ambient-glow bottom-right"></div>

    <header class="page-header">
      <div class="header-content">
        <div>
          <p class="eyebrow">Account</p>
          <h1>My Profile</h1>
          <p class="subtitle">Manage your personal notification channels.</p>
        </div>
      </div>
    </header>

    <main class="page-content">
      <div class="grid">
        <section class="settings-card">
          <div class="card-header">
            <div>
              <h3>Notification Channels</h3>
              <p class="card-subtitle">Set where you want to receive alerts personally.</p>
            </div>
          </div>
          <div class="card-body">

            <div class="form-group">
              <label for="telegramChatId">
                <span class="label-text">Your Telegram Chat ID</span>
              </label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 2L11 13"/><path d="M22 2L15 22l-4-9-9-4 20-7z"/></svg>
                </span>
                <input id="telegramChatId" v-model="form.telegramChatId" type="text"
                  class="form-input" placeholder="e.g. 1753235625" autocomplete="off" />
              </div>
              <div class="hint-panel">
                <div class="hint-title">How to get your Telegram Chat ID</div>
                <div class="hint-body">
                  <div class="hint-row"><span class="hint-badge">1</span><span>Message <strong>@SPKMonitorbot</strong> on Telegram</span></div>
                  <div class="hint-row"><span class="hint-badge">2</span><span>Open: <code>api.telegram.org/bot&lt;TOKEN&gt;/getUpdates</code></span></div>
                  <div class="hint-row subtle"><span class="hint-badge">!</span><span>Find <strong>"id"</strong> inside <strong>"chat"</strong></span></div>
                </div>
              </div>
            </div>

            <div class="form-group">
              <label for="lineToken">
                <span class="label-text">Your LINE User ID</span>
              </label>
              <div class="input-wrapper">
                <span class="input-icon">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
                </span>
                <input id="lineToken" v-model="form.lineToken" type="text"
                  class="form-input" placeholder="e.g. U1234567890abcdef..." autocomplete="off" />
              </div>
              <div class="hint-panel">
                <div class="hint-title">How to get your LINE User ID</div>
                <div class="hint-body">
                  <div class="hint-row"><span class="hint-badge">1</span><span>Add friend <strong>@977cpffa</strong> on LINE</span></div>
                  <div class="hint-row"><span class="hint-badge">2</span><span>Type anything to receive your ID</span></div>
                  <div class="hint-row subtle"><span class="hint-badge">!</span><span>Starts with <strong>U</strong></span></div>
                </div>
              </div>
            </div>

            <div class="form-actions">
              <button class="btn-save" @click="save" :disabled="loading">
                <span v-if="loading" class="loading-spinner"></span>
                <span v-else>Save</span>
              </button>
              <transition name="fade">
                <p v-if="message" :class="['feedback-message', msgClass]">{{ message }}</p>
              </transition>
            </div>
          </div>
        </section>

        <aside class="info-card">
          <div class="info-header">Signed in as</div>
          <div class="info-body">
            <div class="user-avatar">{{ initials }}</div>
            <div class="info-item">
              <div class="info-title">{{ currentUser?.username }}</div>
              <div class="info-desc">{{ roleLabel }}</div>
            </div>
          </div>
        </aside>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';

const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (window.location.port === '8080' || window.location.hostname !== 'localhost') return '/api';
  return 'http://localhost:5050';
};

const currentUser = computed(() => {
  try { return JSON.parse(localStorage.getItem('user') || '{}'); } catch { return {}; }
});

const initials = computed(() => {
  const name = currentUser.value?.username || '';
  return name.slice(0, 2).toUpperCase();
});

const roleLabel = computed(() => {
  const r = currentUser.value?.roleId;
  return r === 1 ? 'Administrator' : r === 2 ? 'Operator' : 'Viewer';
});

const form = ref({ telegramChatId: '', lineToken: '' });
const loading = ref(false);
const message = ref('');
const msgClass = ref('');

const save = async () => {
  loading.value = true;
  message.value = '';
  try {
    const userId = currentUser.value?.userId;
    await fetch(`${getApiBaseUrl()}/users/${userId}/notifications`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ telegramChatId: form.value.telegramChatId, lineToken: form.value.lineToken }),
    });
    message.value = '✅ Saved!';
    msgClass.value = 'success';
  } catch {
    message.value = '❌ Failed to save.';
    msgClass.value = 'error';
  } finally {
    loading.value = false;
    setTimeout(() => { message.value = ''; }, 3000);
  }
};

onMounted(async () => {
  try {
    const userId = currentUser.value?.userId;
    const res = await fetch(`${getApiBaseUrl()}/users`);
    const users = await res.json();
    const me = users.find(u => u.userId === userId);
    if (me) {
      form.value.telegramChatId = me.telegramChatId || '';
      form.value.lineToken = me.lineToken || '';
    }
  } catch {}
});
</script>

<style scoped>
.page-container { min-height: 100vh; background: #050608; color: #c9d1d9; position: relative; overflow: hidden; }
.ambient-glow { position: fixed; width: 600px; height: 600px; border-radius: 50%; filter: blur(120px); opacity: 0.15; z-index: 0; pointer-events: none; }
.top-left { top: -200px; left: -200px; background: radial-gradient(circle, rgba(77,165,221,1) 0%, rgba(0,0,0,0) 70%); }
.bottom-right { bottom: -200px; right: -200px; background: radial-gradient(circle, rgba(124,58,237,1) 0%, rgba(0,0,0,0) 70%); }
.page-header { padding: 40px 48px 24px; position: relative; z-index: 1; border-bottom: 1px solid rgba(255,255,255,0.06); }
.header-content { display: flex; justify-content: space-between; align-items: flex-start; }
.eyebrow { text-transform: uppercase; letter-spacing: 0.18em; font-size: 11px; color: #8b949e; margin-bottom: 6px; }
h1 { font-size: 28px; font-weight: 700; color: white; margin: 0 0 6px; }
.subtitle { font-size: 14px; color: #8b949e; margin: 0; }
.page-content { padding: 32px 48px; position: relative; z-index: 1; }
.grid { display: grid; grid-template-columns: 1fr 300px; gap: 24px; }
.settings-card, .info-card { background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.08); border-radius: 12px; padding: 28px; }
.card-header { margin-bottom: 24px; }
h3 { font-size: 16px; font-weight: 600; color: white; margin: 0 0 4px; }
.card-subtitle { font-size: 13px; color: #8b949e; margin: 0; }
.form-group { margin-bottom: 24px; }
label { display: block; margin-bottom: 8px; }
.label-text { font-size: 13px; font-weight: 600; color: #c9d1d9; text-transform: uppercase; letter-spacing: 0.08em; }
.input-wrapper { display: flex; align-items: center; background: rgba(10,13,23,0.9); border: 1px solid rgba(255,255,255,0.1); border-radius: 8px; padding: 0 14px; transition: border-color 0.2s; }
.input-wrapper:focus-within { border-color: #4da5dd; }
.input-icon { color: #8b949e; display: flex; margin-right: 10px; flex-shrink: 0; }
.form-input { flex: 1; background: transparent; border: none; outline: none; padding: 14px 0; font-size: 14px; color: #c9d1d9; }
.hint-panel { margin-top: 12px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.06); border-radius: 8px; padding: 12px 14px; }
.hint-title { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.1em; color: #8b949e; margin-bottom: 8px; }
.hint-body { display: flex; flex-direction: column; gap: 6px; }
.hint-row { display: flex; align-items: flex-start; gap: 10px; font-size: 13px; color: #c9d1d9; }
.hint-row.subtle { color: #8b949e; }
.hint-badge { min-width: 20px; height: 20px; border-radius: 50%; background: rgba(77,165,221,0.2); color: #4da5dd; font-size: 11px; font-weight: 700; display: flex; align-items: center; justify-content: center; }
.form-actions { display: flex; align-items: center; gap: 16px; margin-top: 8px; }
.btn-save { background: #4da5dd; color: white; border: none; border-radius: 8px; padding: 12px 28px; font-size: 14px; font-weight: 600; cursor: pointer; transition: all 0.2s; }
.btn-save:hover:not(:disabled) { background: #3d95cd; }
.btn-save:disabled { opacity: 0.5; cursor: not-allowed; }
.feedback-message { font-size: 13px; }
.feedback-message.success { color: #3fb950; }
.feedback-message.error { color: #f85149; }
.info-header { font-size: 11px; text-transform: uppercase; letter-spacing: 0.15em; color: #8b949e; margin-bottom: 16px; }
.user-avatar { width: 56px; height: 56px; border-radius: 50%; background: linear-gradient(135deg, #4da5dd, #7c3aed); display: flex; align-items: center; justify-content: center; font-size: 20px; font-weight: 700; color: white; margin-bottom: 16px; }
.info-item .info-title { font-size: 15px; font-weight: 600; color: white; margin-bottom: 4px; }
.info-item .info-desc { font-size: 13px; color: #8b949e; }
code { background: rgba(255,255,255,0.08); padding: 2px 5px; border-radius: 4px; font-size: 11px; }
.fade-enter-active, .fade-leave-active { transition: opacity 0.3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
@media (max-width: 900px) { .grid { grid-template-columns: 1fr; } .page-header, .page-content { padding: 24px 20px; } }
</style>
