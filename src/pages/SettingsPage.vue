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
        </div>
      </header>
  
      <main class="page-content">
          <div class="settings-card-container">
              <div class="settings-card">
                  <div class="card-header">
                      <h3>Alert Receiver</h3>
                      <p class="card-subtitle">Specify who should receive the alerts.</p>
                  </div>
  
                  <div class="card-body">
                      <div class="form-group">
                          <label for="lineToken">
                              <span class="label-text">Your LINE User ID</span>
                          </label>
                          <div class="input-wrapper">
                              <span class="input-icon">👤</span>
                              <input 
                              id="lineToken"
                              v-model="settings.lineToken" 
                              type="text" 
                              class="form-input" 
                              placeholder="e.g. U1234567890abcdef..."
                              />
                          </div>
                           <small class="hint">
                              ⚠️ <strong>Not your ID/Phone Number.</strong> It starts with 'U'.<br>
                              👉 To get this ID: Add friend <strong>@977cpffa</strong> and type anything to it.
                          </small>
                      </div>
  
                       <div class="form-group-checkbox">
                           <label class="custom-checkbox">
                               <input type="checkbox" id="enableAlerts" v-model="settings.isEnable">
                               <span class="checkmark"></span>
                               <span class="label-text">Enable Notification System</span>
                           </label>
                      </div>
  
                      <div class="form-actions">
                          <button class="btn-save" @click="saveSettings" :disabled="loading">
                              <span v-if="loading" class="loading-spinner"></span>
                              <span v-else>Save Configuration</span>
                          </button>
                      </div>
  
                       <transition name="fade">
                          <p v-if="message" :class="['feedback-message', msgClass]">
                              {{ message }}
                          </p>
                       </transition>
                  </div>
              </div>
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
  /* ... CSS เดิม ... */
  /* เพิ่ม CSS ให้ Hint ดูเด่นขึ้นนิดนึง */
  .hint { display: block; margin-top: 8px; color: #8b949e; font-size: 13px; line-height: 1.5; background: rgba(255,255,255,0.05); padding: 8px; border-radius: 4px; border-left: 3px solid #4da5dd; }
  /* ... CSS เดิม ... */
  /* ใส่ CSS เดิมทั้งหมดต่อท้ายได้เลยครับ */
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
  
  /* Card Styling */
  .page-content {
      padding: 0 clamp(24px, 6vw, 72px);
      display: flex;
      position: relative;
      z-index: 1;
  }
  
  .settings-card-container {
      width: 100%;
      max-width: 500px; /* ปรับให้แคบลงนิดหน่อยเพราะเหลือ input เดียว */
  }
  
  .settings-card {
    background: #161b22;
    border: 1px solid #30363d;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);
  }
  
  .card-header {
      padding: 24px;
      border-bottom: 1px solid #30363d;
      background: rgba(255,255,255,0.02);
  }
  
  .card-header h3 { margin: 0 0 4px 0; color: white; font-size: 16px; }
  .card-subtitle { margin: 0; font-size: 13px; color: #8b949e; }
  
  .card-body { padding: 24px; }
  
  /* Inputs */
  .form-group { margin-bottom: 24px; }
  .label-text { display: block; margin-bottom: 8px; font-weight: 600; font-size: 13px; color: #e6edf3; }
  
  .input-wrapper { position: relative; display: flex; align-items: center; }
  .input-icon { position: absolute; left: 12px; font-size: 16px; opacity: 0.7; }
  
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
      border-color: #06c755; /* สีเขียว LINE */
      box-shadow: 0 0 0 3px rgba(6, 199, 85, 0.15);
  }
  
  /* Checkbox & Button */
  .form-group-checkbox { margin-bottom: 32px; padding-top: 16px; border-top: 1px solid #30363d; }
  .custom-checkbox { display: inline-flex; align-items: center; cursor: pointer; position: relative; }
  .custom-checkbox input { position: absolute; opacity: 0; }
  .checkmark {
      height: 18px; width: 18px;
      background-color: #0d1117;
      border: 1px solid #30363d;
      border-radius: 4px;
      margin-right: 10px;
      transition: all 0.2s;
  }
  .custom-checkbox:hover .checkmark { border-color: #8b949e; }
  .custom-checkbox input:checked ~ .checkmark { background-color: #238636; border-color: #238636; }
  .checkmark:after { content: ""; position: absolute; display: none; }
  .custom-checkbox input:checked ~ .checkmark:after { display: block; }
  .custom-checkbox .checkmark:after {
      left: 6px; top: 2px; width: 4px; height: 9px;
      border: solid white; border-width: 0 2px 2px 0; transform: rotate(45deg);
  }
  .checkbox-hint { margin: 4px 0 0 28px; font-size: 12px; color: #8b949e; }
  
  .btn-save {
    background: #238636;
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
  </style>