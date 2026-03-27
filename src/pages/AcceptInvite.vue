<template>
    <div class="auth-wrapper">
      <div class="auth-card">
        <div class="brand">
          <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#238636" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
            <polyline points="22 4 12 14.01 9 11.01"></polyline>
          </svg>
          <h2>Accept Invitation</h2>
        </div>
        
        <p class="subtitle">Set your password to activate your account and access the monitoring workspace.</p>
  
        <form @submit.prevent="submitPassword" v-if="!success">
          <div class="form-group">
            <label>New Password</label>
            <div class="input-wrapper">
              <input 
                v-model="password" 
                :type="showPassword ? 'text' : 'password'" 
                class="form-control" 
                placeholder="Minimum 6 characters" 
                required 
                minlength="6"
              />
              
              <button type="button" class="btn-toggle-password" @click="showPassword = !showPassword" tabindex="-1">
                <svg v-if="!showPassword" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                  <circle cx="12" cy="12" r="3"></circle>
                </svg>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"></path>
                  <line x1="1" y1="1" x2="23" y2="23"></line>
                </svg>
              </button>
            </div>
          </div>
          
          <button type="submit" class="btn-primary" :disabled="loading || !token">
            {{ loading ? 'Activating...' : 'Activate Account' }}
          </button>
        </form>
  
        <div v-if="success" class="success-msg">
          🎉 Account activated successfully! <br/> Redirecting to login...
        </div>
        <div v-if="errorMsg" class="error-msg">
          {{ errorMsg }}
        </div>
      </div>
    </div>
  </template>
  
  <script setup>
  import { ref, onMounted } from 'vue'
  import axios from 'axios'
  import { useRoute, useRouter } from 'vue-router'
  
  const route = useRoute()
  const router = useRouter()
  
  const password = ref('')
  const showPassword = ref(false) // 🔴 ตัวแปรเก็บสถานะเปิด/ปิดตา
  const loading = ref(false)
  const success = ref(false)
  const errorMsg = ref('')
  const token = ref('')
  
  // --- Config API ---
  const getApiBaseUrl = () => {
    if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
    if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
    return "http://localhost:5050"
  }
  const API_URL = getApiBaseUrl()
  
  onMounted(() => {
    // ดึง token ออกมาจาก URL (เช่น ?token=INVITE_1234...)
    token.value = route.query.token
    if (!token.value) {
      errorMsg.value = "⚠️ Invalid or missing invitation token. Please check your email link."
    }
  })
  
  const submitPassword = async () => {
    if (!token.value) return
    
    loading.value = true
    errorMsg.value = ''
    
    try {
      const endpoint = API_URL.startsWith("/") ? `${API_URL}/users/accept` : `${API_URL}/api/users/accept`
      
      // ส่ง Token และ รหัสผ่านใหม่ ไปให้ Backend C#
      await axios.post(endpoint, {
        Token: token.value,
        NewPassword: password.value
      })
  
      success.value = true
      password.value = ''
      
      // สำเร็จแล้วให้หน่วงเวลา 2 วินาที แล้วเด้งกลับไปหน้า Login อัตโนมัติ
      setTimeout(() => {
        router.push('/login')
      }, 2000)
      
    } catch (err) {
      console.error("Activation error:", err)
      errorMsg.value = err.response?.data?.message || "Failed to activate account. The link might be expired."
    } finally {
      loading.value = false
    }
  }
  </script>
  
  <style scoped>
  .auth-wrapper {
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #0d1117; 
    color: #c9d1d9;
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
    padding: 20px;
  }
  
  .auth-card {
    background: #161b22;
    border: 1px solid #30363d;
    padding: 40px;
    border-radius: 12px;
    width: 100%;
    max-width: 420px;
    box-shadow: 0 8px 24px rgba(0,0,0,0.4);
  }
  
  .brand {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
    margin-bottom: 12px;
  }
  
  .brand h2 {
    margin: 0;
    color: #ffffff;
    font-size: 24px;
  }
  
  .subtitle {
    text-align: center;
    color: #8b949e;
    font-size: 14px;
    margin-bottom: 24px;
    line-height: 1.5;
  }
  
  .form-group {
    margin-bottom: 20px;
  }
  
  .form-group label {
    display: block;
    margin-bottom: 8px;
    font-size: 14px;
    font-weight: 500;
    color: #e6edf3;
  }
  
  /* 🔴 สไตล์สำหรับช่องใส่รหัสผ่านและปุ่มดวงตา */
  .input-wrapper {
    position: relative;
    display: flex;
    align-items: center;
  }
  
  .form-control {
    width: 100%;
    padding: 12px;
    padding-right: 40px; /* เว้นที่ให้ปุ่มดวงตา */
    background: #010409;
    border: 1px solid #30363d;
    border-radius: 6px;
    color: #c9d1d9;
    font-size: 14px;
    outline: none;
    transition: border 0.2s;
    box-sizing: border-box;
  }
  
  .form-control:focus {
    border-color: #58a6ff;
    box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.3);
  }
  
  .btn-toggle-password {
    position: absolute;
    right: 10px;
    background: transparent;
    border: none;
    color: #8b949e;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 4px;
    transition: color 0.2s;
  }
  
  .btn-toggle-password:hover {
    color: #c9d1d9;
  }
  
  .btn-primary {
    width: 100%;
    padding: 12px;
    background: #238636;
    color: white;
    border: none;
    border-radius: 6px;
    font-weight: 600;
    font-size: 14px;
    cursor: pointer;
    transition: background 0.2s;
  }
  
  .btn-primary:hover:not(:disabled) {
    background: #2ea043;
  }
  
  .btn-primary:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
  
  .success-msg {
    margin-top: 20px;
    padding: 16px;
    background: rgba(63, 185, 80, 0.1);
    border: 1px solid rgba(63, 185, 80, 0.2);
    color: #3fb950;
    border-radius: 6px;
    font-size: 14px;
    text-align: center;
    line-height: 1.5;
  }
  
  .error-msg {
    margin-top: 20px;
    padding: 12px;
    background: rgba(248, 81, 73, 0.1);
    border: 1px solid rgba(248, 81, 73, 0.2);
    color: #f85149;
    border-radius: 6px;
    font-size: 14px;
    text-align: center;
  }
  </style>