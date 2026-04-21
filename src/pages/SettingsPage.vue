<template>
  <div class="page-container">
    <div class="ambient-glow top-left"></div>
    <div class="ambient-glow bottom-right"></div>

    <header class="page-header">
      <div class="header-content">
        <div>
          <p class="eyebrow">Configuration</p>
          <h1>Notification Settings</h1>
          <p class="subtitle">Manage how you receive critical alerts via NAPMA BOT.</p>
        </div>
        <div class="status-chip" :class="settings.isEnable ? 'on' : 'off'">
          <span class="chip-dot"></span>
          {{ settings.isEnable ? 'Notifications Enabled' : 'Notifications Disabled' }}
        </div>
      </div>
    </header>

    <main class="page-content">
      <div class="grid">
        <section class="settings-card">
          <div class="card-header">
            <div>
              <h3>Alert Receiver</h3>
              <p class="card-subtitle">Specify who should receive the alerts.</p>
            </div>
          </div>

          <div class="card-body">
            <div class="form-group">
              <label for="lineToken">
                <span class="label-text">Your LINE User ID</span>
              </label>
              <div class="input-wrapper">
                <span class="input-icon" aria-hidden="true">
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                </span>
                <input
                  id="lineToken"
                  v-model="settings.lineToken"
                  type="text"
                  class="form-input"
                  placeholder="e.g. U1234567890abcdef..."
                  autocomplete="off"
                  spellcheck="false"
                />
              </div>
              <div class="hint-panel">
                <div class="hint-title">How to get your LINE User ID</div>
                <div class="hint-body">
                  <div class="hint-row">
                    <span class="hint-badge">1</span>
                    <span>Add friend <strong>@977cpffa</strong></span>
                  </div>
                  <div class="hint-row">
                    <span class="hint-badge">2</span>
                    <span>Type anything to the bot to receive your ID</span>
                  </div>
                  <div class="hint-row subtle">
                    <span class="hint-badge">!</span>
                    <span>It starts with <strong>U</strong> (not phone number)</span>
                  </div>
                </div>
              </div>
            </div>

            <div class="form-group-checkbox">
              <label class="toggle">
                <input type="checkbox" id="enableAlerts" v-model="settings.isEnable" />
                <span class="toggle-track" aria-hidden="true">
                  <span class="toggle-thumb"></span>
                </span>
                <span class="toggle-label">
                  <span class="label-text">Enable Notification System</span>
                  <span class="label-subtext">Send alerts automatically when metrics exceed thresholds.</span>
                </span>
              </label>
            </div>

            <div class="form-actions">
              <button class="btn-save" @click="saveSettings" :disabled="loading">
                <span v-if="loading" class="loading-spinner"></span>
                <span v-else>Save Configuration</span>
              </button>
              <transition name="fade">
                <p v-if="message" :class="['feedback-message', msgClass]">
                  {{ message }}
                </p>
              </transition>
            </div>
          </div>
        </section>

        <aside class="info-card">
          <div class="info-header">What gets notified?</div>
          <div class="info-body">
            <div class="info-item">
              <div class="info-title">Down / Error</div>
              <div class="info-desc">Target is unreachable or HTTP returns error.</div>
            </div>
            <div class="info-item">
              <div class="info-title">High Latency</div>
              <div class="info-desc">Latency exceeds per-monitor threshold configured in Testing.</div>
            </div>
            <div class="info-divider"></div>
            <div class="info-note">
              Tip: Add multiple agents on different VMs to simulate different locations.
            </div>
          </div>
        </aside>
      </div>
    </main>
  </div>
</template>
  
  <script setup>
  import { ref, onMounted } from 'vue';
  import axios from 'axios';
  
  const getApiBaseUrl = () => {
    if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
    if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
    return "http://localhost:5050"
  }
  const API_URL = `${getApiBaseUrl()}/settings/notifications`;
  
  const settings = ref({
  lineToken: '',
  telegramToken: 'disabled', // 👈 แก้ตรงนี้! ใส่ข้อความอะไรก็ได้ที่ไม่ใช่ค่าว่าง
  isEnable: true
});
  
  const loading = ref(false);
  const message = ref('');
  const msgClass = ref('');
  
  const loadSettings = async () => {
    try {
      const res = await axios.get(API_URL);
      if (res.data) settings.value = res.data;
    } catch (err) {
      console.error("Load settings failed", err);
    }
  };
  
  const saveSettings = async () => {
    loading.value = true;
    message.value = '';
    try {
      await axios.post(API_URL, settings.value);
      message.value = '✅ User ID saved! Try running a test.';
      msgClass.value = 'success';
      setTimeout(() => { message.value = ''; }, 3000);
    } catch (err) {
      message.value = '❌ Failed to save.';
      msgClass.value = 'error';
    } finally {
      loading.value = false;
    }
  };
  
  onMounted(loadSettings);
  </script>
  
  <style scoped>
  .page-container {
    min-height: 100vh;
    background: #050608; 
    color: #c9d1d9;
    position: relative;
    overflow: hidden; 
  }
  
  /* แสงฟุ้ง Ambient Glow (เหมือนหน้า User) */
  .ambient-glow {
      position: fixed;
      width: 600px;
      height: 600px;
      border-radius: 50%;
      filter: blur(120px); 
      opacity: 0.15; 
      z-index: 0; 
      pointer-events: none; 
  }
  
  .top-left {
      top: -200px;
      left: -200px;
      background: radial-gradient(circle, rgba(77, 165, 221, 1) 0%, rgba(0,0,0,0) 70%);
  }
  
  .bottom-right {
      bottom: -200px;
      right: -200px;
      background: radial-gradient(circle, rgba(124, 58, 237, 1) 0%, rgba(0,0,0,0) 70%);
  }
  
  /* Header */
  .page-header {
    padding: 48px clamp(24px, 6vw, 72px);
    position: relative;
    z-index: 1;
  }

  .header-content {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 24px;
  }
  
  .eyebrow {
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: 0.16em;
    color: #8b949e;
    margin-bottom: 8px;
    font-weight: 600;
  }
  
  h1 {
    font-size: clamp(28px, 4vw, 36px);
    font-weight: 700;
    color: white;
    margin: 6px 0;
  }
  
  .subtitle {
    font-size: 14px;
    color: #8b949e;
    margin: 0;
  }

  .status-chip {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    padding: 10px 14px;
    border-radius: 999px;
    border: 1px solid #30363d;
    background: rgba(255, 255, 255, 0.04);
    font-size: 12px;
    font-weight: 600;
    white-space: nowrap;
  }
  .status-chip.on { color: #73bf69; border-color: rgba(115, 191, 105, 0.35); background: rgba(115, 191, 105, 0.10); }
  .status-chip.off { color: #f2495c; border-color: rgba(242, 73, 92, 0.35); background: rgba(242, 73, 92, 0.10); }
  .chip-dot { width: 8px; height: 8px; border-radius: 999px; background: currentColor; }
  
  /* Card Styling */
  .page-content {
      padding: 0 clamp(24px, 6vw, 72px);
      display: flex;
      position: relative;
      z-index: 1;
  }

  .grid {
    width: 100%;
    display: grid;
    grid-template-columns: 1.6fr 1fr;
    gap: 18px;
    align-items: start;
    padding-bottom: 48px;
  }
  
  .settings-card {
    background: #161b22;
    border: 1px solid #30363d;
    border-radius: 14px;
    overflow: hidden;
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);
  }
  
  .card-header {
      padding: 24px;
      border-bottom: 1px solid #30363d;
      background: linear-gradient(135deg, rgba(88, 166, 255, 0.08) 0%, rgba(124, 58, 237, 0.06) 100%);
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
  }
  
  .card-header h3 { margin: 0 0 4px 0; color: white; font-size: 16px; }
  .card-subtitle { margin: 0; font-size: 13px; color: #8b949e; }
  
  .card-body { padding: 24px; }
  
  /* Inputs */
  .form-group { margin-bottom: 24px; }
  .label-text { display: block; margin-bottom: 8px; font-weight: 700; font-size: 13px; color: #e6edf3; }
  
  .input-wrapper { position: relative; display: flex; align-items: center; }
  .input-icon { position: absolute; left: 12px; opacity: 0.8; color: #8b949e; display: inline-flex; align-items: center; }
  
  .form-input {
    width: 100%;
    padding: 10px 12px 10px 40px;
    background: #0d1117;
    border: 1px solid #30363d;
    color: white;
    border-radius: 6px;
    font-size: 14px;
    transition: all 0.2s ease;
  }
  
  .form-input:focus {
      outline: none;
      border-color: #58a6ff;
      box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.15);
  }

  .hint-panel {
    margin-top: 12px;
    padding: 12px;
    border-radius: 12px;
    border: 1px solid rgba(88, 166, 255, 0.25);
    background: rgba(88, 166, 255, 0.06);
  }
  .hint-title {
    font-size: 12px;
    font-weight: 800;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: #9cc7ff;
    margin-bottom: 10px;
  }
  .hint-body { display: flex; flex-direction: column; gap: 8px; }
  .hint-row { display: flex; align-items: center; gap: 10px; font-size: 13px; color: #c9d1d9; }
  .hint-row.subtle { color: #8b949e; }
  .hint-badge {
    width: 22px;
    height: 22px;
    border-radius: 7px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-weight: 800;
    font-size: 12px;
    color: #c9d1d9;
    border: 1px solid rgba(255, 255, 255, 0.10);
    background: rgba(0, 0, 0, 0.25);
    flex: 0 0 auto;
  }
  
  /* Checkbox & Button */
  .form-group-checkbox { margin-bottom: 22px; padding-top: 18px; border-top: 1px solid #30363d; }
  .toggle { display: grid; grid-template-columns: auto 1fr; gap: 14px; align-items: start; cursor: pointer; }
  .toggle input { position: absolute; opacity: 0; }
  .toggle-track {
    width: 46px;
    height: 26px;
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid #30363d;
    border-radius: 999px;
    position: relative;
    transition: all 0.15s ease;
    flex: 0 0 auto;
  }
  .toggle-thumb {
    width: 20px;
    height: 20px;
    border-radius: 999px;
    background: #c9d1d9;
    position: absolute;
    top: 2px;
    left: 3px;
    transition: all 0.15s ease;
  }
  .toggle-label { display: flex; flex-direction: column; gap: 3px; padding-top: 1px; }
  .label-subtext { font-size: 12px; color: #8b949e; }
  .toggle:has(input:checked) .toggle-track { background: rgba(35, 134, 54, 0.20); border-color: rgba(35, 134, 54, 0.45); }
  .toggle:has(input:checked) .toggle-thumb { left: 22px; background: #3fb950; }
  
  .btn-save {
    background: linear-gradient(90deg, #238636 0%, #2ea043 100%);
    color: white;
    border: none;
    padding: 10px 24px;
    border-radius: 6px;
    cursor: pointer;
    font-weight: 600;
    font-size: 14px;
    width: 100%;
    transition: background 0.2s;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  .btn-save:hover:not(:disabled) { background: #2ea043; }
  .btn-save:disabled { opacity: 0.6; cursor: not-allowed; }
  
  .feedback-message { margin-top: 16px; padding: 10px; border-radius: 6px; text-align: center; font-size: 13px; }
  .success { background: rgba(46, 160, 67, 0.15); color: #3fb950; border: 1px solid rgba(46, 160, 67, 0.4); }
  .error { background: rgba(248, 81, 73, 0.15); color: #f85149; border: 1px solid rgba(248, 81, 73, 0.4); }
  
  /* Animation */
  .fade-enter-active, .fade-leave-active { transition: opacity 0.3s; }
  .fade-enter-from, .fade-leave-to { opacity: 0; }
  .loading-spinner {
      width: 16px; height: 16px;
      border: 2px solid #ffffff; border-top: 2px solid transparent;
      border-radius: 50%; animation: spin 0.8s linear infinite;
  }
  @keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }

  .info-card {
    background: rgba(22, 27, 34, 0.75);
    border: 1px solid #30363d;
    border-radius: 14px;
    overflow: hidden;
    backdrop-filter: blur(8px);
  }
  .info-header {
    padding: 18px 18px 14px;
    border-bottom: 1px solid #30363d;
    color: #e6edf3;
    font-weight: 700;
  }
  .info-body { padding: 16px 18px 18px; display: flex; flex-direction: column; gap: 14px; }
  .info-item { padding: 12px; border-radius: 12px; border: 1px solid rgba(255,255,255,0.08); background: rgba(0,0,0,0.20); }
  .info-title { font-size: 13px; font-weight: 800; color: #c9d1d9; margin-bottom: 4px; }
  .info-desc { font-size: 13px; color: #8b949e; }
  .info-divider { height: 1px; background: rgba(255,255,255,0.08); }
  .info-note { font-size: 13px; color: #8b949e; line-height: 1.45; }

  @media (max-width: 980px) {
    .grid { grid-template-columns: 1fr; }
    .status-chip { display: none; }
  }
</style>