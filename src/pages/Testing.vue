<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Lab testing</p>
        <h1>Scenario validation</h1>
        <p class="text-muted">
          Run synthetic flows and capture baselines before promoting config changes to the live
          dashboard.
        </p>
      </div>
      <button class="btn-primary" @click="showPopup = true">New test run</button>
    </header>

    <div v-if="showPopup" class="modal-backdrop">
      <div class="modal">
        <h3>Run Test</h3>
        
        <div class="form-group">
          <label>Choose Agent:</label>
          <select v-model="selectedAgent">
            <option v-for="agent in agents" :key="agent.id" :value="agent.id">{{ agent.name }}</option>
          </select>
        </div>

        <div class="form-group">
          <label>Target:</label>
          <input v-model="newTarget" placeholder="e.g. https://google.com" @keyup.enter="confirmRun" />
        </div>

        <div class="modal-actions">
          <button class="btn-ghost" @click="showPopup = false">Cancel</button>
          <button class="btn-primary" @click="confirmRun" :disabled="loading">
            {{ loading ? 'Running...' : 'Run Test' }}
          </button>
        </div>
      </div>
    </div>

    <section class="grid-auto-fit">
      <article class="panel test-card" v-for="(suite, index) in testSuites" :key="index">
        <div class="card-head">
          <h3>{{ suite.name }}</h3>
          <span class="pill soft-pill">
            <span class="status-dot" :class="suite.statusClass"></span>
            {{ suite.status }}
          </span>
        </div>
        <p class="text-muted">{{ suite.description }}</p>
        <p class="text-muted" style="font-size: 0.8em; margin-top: 5px;">Time: {{ suite.latency }}</p>
        <div class="card-footer">
          <span>Last run · {{ suite.lastRun }}</span>
          <button class="btn-ghost compact" @click="viewLog(suite)">View log</button>
        </div>
      </article>

      <div v-if="testSuites.length === 0" class="empty-state">
        <p class="text-muted">No test runs yet. Click "New test run" to start.</p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

// เปลี่ยน Port เป็น 5050 (หนี AirPlay)
const API_URL = 'http://localhost:5050/api/agent/run'

const testSuites = ref([])
const showPopup = ref(false)
const loading = ref(false)
const newTarget = ref('')
const agents = ref([
  { id: 'agent-core', name: 'Agent1' },
  { id: 'agent-backup', name: 'Agent2' }
])
const selectedAgent = ref(agents.value[0].id)

function viewLog(suite) {
  alert(`Detailed Log:\nTarget: ${suite.name}\nResult: ${suite.status}\nLatency: ${suite.latency}\nTime: ${suite.lastRun}`)
}

async function confirmRun() {
  if (!newTarget.value) return alert('Please enter a target')
  
  loading.value = true; // เริ่มหมุนติ้วๆ

  try {
    // ยิงไป Backend
    const res = await axios.post(API_URL, {
      agentId: selectedAgent.value,
      target: newTarget.value
    })

    const data = res.data;

    // สร้าง Object ผลลัพธ์ใหม่
    const newResult = {
      name: data.target,
      description: data.message,
      status: data.status,
      statusClass: data.status === 'Success' ? 'status-success' : 'status-warning', // สีเขียว/เหลือง
      latency: data.latency,
      lastRun: new Date(data.timestamp).toLocaleTimeString()
    }

    // .unshift คือเอาไปแทรกไว้บนสุดของ Array (อันใหม่สุดอยู่บน)
    testSuites.value.unshift(newResult)
    
    // ปิด Popup และเคลียร์ค่า
    showPopup.value = false
    newTarget.value = ''

  } catch (err) {
    console.error(err)
    alert('Failed to run test. Check Backend connection.')
  } finally {
    loading.value = false; // หยุดหมุน
  }
}
</script>

<style scoped>
.page-shell {
  padding: 40px;
}
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 40px;
}
.eyebrow {
  text-transform: uppercase;
  letter-spacing: 0.1em;
  font-size: 12px;
  color: #8b949e;
  margin-bottom: 8px;
}
h1 {
  font-size: 32px;
  margin: 0 0 8px 0;
}
.grid-auto-fit {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}

/* Modal Styles (Dark Mode Theme) */
.modal-backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.7); /* พื้นหลังมืดลง */
  backdrop-filter: blur(2px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 999;
}

.modal {
  background: #161b22; /* สีเทาเข้มเหมือน GitHub Dark */
  color: white;
  padding: 24px;
  border-radius: 12px;
  width: 350px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  border: 1px solid #30363d;
  box-shadow: 0 8px 24px rgba(0,0,0,0.5);
}

.modal h3 {
  margin: 0;
  font-size: 18px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

label {
  font-size: 13px;
  color: #8b949e;
}

/* Input & Select สไตล์ Dark Mode */
input, select {
  background: #0d1117;
  border: 1px solid #30363d;
  color: white;
  padding: 8px 12px;
  border-radius: 6px;
  outline: none;
}

input:focus, select:focus {
  border-color: #58a6ff;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 8px;
}

/* Card Styles */
.panel {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 8px;
  padding: 20px;
  display: flex;
  flex-direction: column;
}
.card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}
.card-head h3 {
  margin: 0;
  font-size: 16px;
}
.card-footer {
  margin-top: auto;
  padding-top: 16px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  color: #8b949e;
}

/* Utilities */
.text-muted { color: #8b949e; }
.btn-primary {
  background: #238636;
  color: white;
  border: none;
  padding: 6px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
}
.btn-primary:hover { background: #2ea043; }
.btn-primary:disabled { background: #238636; opacity: 0.5; cursor: not-allowed; }

.btn-ghost {
  background: transparent;
  color: #58a6ff;
  border: none;
  cursor: pointer;
}
.btn-ghost:hover { text-decoration: underline; }

.pill {
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
  border: 1px solid;
  display: flex;
  align-items: center;
  gap: 6px;
}
.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}
.status-success { background: #238636; border-color: #238636; color: #3fb950; }
.status-warning { background: #9e6a03; border-color: #9e6a03; color: #d29922; }
.status-dot.status-success { background: #3fb950; }
.status-dot.status-warning { background: #d29922; }

.empty-state {
  grid-column: 1 / -1;
  text-align: center;
  padding: 40px;
  border: 1px dashed #30363d;
  border-radius: 8px;
}
</style>