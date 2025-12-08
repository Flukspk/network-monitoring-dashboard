<template>
  <div class="page-shell">
    
    <header class="page-header">
      <div>
        <p class="eyebrow">On-Demand Test</p>
        <h1>Agent Live Monitor</h1>
        <p class="text-muted">Real-time latency check & diagnostic tool.</p>
      </div>
      
      <button v-if="!isMonitoring" class="btn-primary" @click="openModal">
        <span class="icon">+</span> New Test Run
      </button>
      
      <button v-else class="btn-danger" @click="stopMonitoring">
        Stop Monitoring
      </button>
    </header>

    <section v-if="!isMonitoring" class="dashboard-grid">
      <div class="card summary-card">
        <div class="card-header">
          <h3>🕒 Last Activity</h3>
          <button class="btn-link" @click="fetchLatest">Refresh</button>
        </div>
        
        <div v-if="latestMetric" class="card-body">
          <div class="stat-row">
            <span class="label">Target:</span>
            <span class="value highlight">{{ latestMetric.target }}</span>
          </div>
          <div class="stat-row">
            <span class="label">Type:</span>
            <span class="pill soft-pill">{{ latestMetric.metricType }}</span>
          </div>
          <div class="stat-row">
            <span class="label">Result:</span>
            <span class="pill" :class="latestMetric.status === 'Success' ? 'status-success' : 'status-danger'">
              {{ latestMetric.status }}
            </span>
          </div>
          <div class="stat-row">
            <span class="label">Latency:</span>
            <span class="value">{{ latestMetric.value }} ms</span>
          </div>
          <div class="stat-row">
            <span class="label">Time:</span>
            <span class="value dimmed">{{ new Date(latestMetric.timestamp).toLocaleString() }}</span>
          </div>
        </div>
        
        <div v-else class="card-body empty-state">
          No recent activity found.
        </div>
      </div>

      <div class="card info-card">
        <h3>🚀 How to start</h3>
        <ul>
          <li>Click <strong>New Test Run</strong> button.</li>
          <li>Select <strong>Agent</strong> and enter <strong>Target</strong>.</li>
          <li>System will run the test and show live graph.</li>
        </ul>
      </div>
    </section>

    <section v-else class="monitor-section">
      <div class="panel graph-panel">
        <div class="card-head">
          <div class="live-indicator">
            <span class="pulse-dot"></span>
            <h3>Live: {{ targetUrl }}</h3>
          </div>
          <span class="pill soft-pill">{{ selectedAgentMode }}</span>
        </div>
        
        <div class="canvas-wrapper">
          <canvas ref="chartCanvas" width="800" height="300"></canvas>
        </div>
        <div class="graph-legend">
          <span>Min: {{ minVal }}ms</span>
          <span>Max: {{ maxVal }}ms</span>
        </div>
      </div>

      <div class="panel table-panel">
        <h4>Recent Logs</h4>
        <table>
          <thead>
            <tr>
              <th>Time</th>
              <th>Value</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(log, i) in logs" :key="i">
              <td>{{ log.time }}</td>
              <td>{{ log.value }} ms</td>
              <td :class="log.status === 'Success' ? 'text-green' : 'text-red'">
                {{ log.status }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-content">
        <div class="modal-header">
          <h2>Start New Test</h2>
          <button class="close-btn" @click="showModal = false">×</button>
        </div>
        
        <div class="modal-body">
          <div class="form-group">
            <label>Select Agent:</label>
            <select v-model="selectedAgentMode">
              <option value="PING">Agent 1 (Ping)</option>
              <option value="HTTP">Agent 2 (HTTP)</option>
              <option value="TRACEROUTE">Agent 3 (Trace)</option>
            </select>
          </div>
          
          <div class="form-group">
            <label>Target:</label>
            <input
              v-model="targetUrl"
              placeholder="e.g. 8.8.8.8, google.com"
              @keyup.enter="startTestFromModal"
            />
            <div class="target-examples">
              <small>Examples:
                <span class="example-link" @click="targetUrl = '1.1.1.1'">1.1.1.1</span>,
                <span class="example-link" @click="targetUrl = 'google.com'">google.com</span>
              </small>
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-ghost" @click="showModal = false">Cancel</button>
          <button class="btn-primary full-width" @click="startTestFromModal" :disabled="isRunningTest">
            {{ isRunningTest ? 'Starting...' : 'Start Monitoring' }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, nextTick } from "vue";
import axios from "axios";

// --- State ---
const isMonitoring = ref(false);
const showModal = ref(false);
const latestMetric = ref(null);

const selectedAgentMode = ref("PING");
const targetUrl = ref("");
const logs = ref([]);
const dataPoints = ref(Array.from({ length: 20 }, () => 0));
const timer = ref(null);
const chartCanvas = ref(null);
const maxVal = ref(100);
const minVal = ref(0);
const isRunningTest = ref(false);

// --- Config Base URL ---
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api";
  return "http://localhost:5050";
};
const API_BASE_URL = getApiBaseUrl();

// --- Helper: Call Backend Start/Stop API ---
const toggleBackendMonitoring = async (action) => {
  try {
    const endpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/${action}`
      : `${API_BASE_URL}/api/metrics/${action}`;
      
    await axios.post(endpoint, {
      Target: targetUrl.value,
      Type: selectedAgentMode.value
    });
    console.log(`Backend ${action} command sent.`);
  } catch (err) {
    console.error(`Failed to ${action} backend:`, err);
  }
}

// --- Actions ---

const openModal = () => {
  targetUrl.value = ""; 
  showModal.value = true;
};

// 🔥 Start Monitoring
const startTestFromModal = async () => {
  if (!targetUrl.value.trim()) {
    alert("Please enter a target before starting monitoring");
    return;
  }
  
  showModal.value = false;
  isMonitoring.value = true;
  isRunningTest.value = true;

  // 1. บอก Backend ให้เริ่ม Monitor (Add to active list)
  await toggleBackendMonitoring('start');

  // 2. Reset กราฟ
  logs.value = [];
  dataPoints.value = Array.from({ length: 20 }, () => 0);
  
  // 3. เริ่มดึงข้อมูล
  fetchData();
  timer.value = setInterval(fetchData, 2000);
  isRunningTest.value = false;
};

// 🔥 Stop Monitoring
const stopMonitoring = async () => {
  // 1. บอก Backend ให้หยุด (Remove from active list)
  // Agent จะหยุดทำงานทันทีเมื่อไม่เจอชื่อใน list
  await toggleBackendMonitoring('stop');

  // 2. Reset UI
  isMonitoring.value = false;
  clearInterval(timer.value);
  timer.value = null;
  fetchLatest();
};

const fetchLatest = async () => {
  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/filter`
      : `${API_BASE_URL}/api/metrics/filter`;
      
    const res = await axios.get(apiEndpoint);
    if (res.data && res.data.length > 0) {
      latestMetric.value = res.data[0];
    }
  } catch (err) {
    console.error("Fetch latest error:", err);
  }
};

const fetchData = async () => {
  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/metrics/filter?target=${encodeURIComponent(targetUrl.value)}&type=${selectedAgentMode.value}`
      : `${API_BASE_URL}/api/metrics/filter?target=${encodeURIComponent(targetUrl.value)}&type=${selectedAgentMode.value}`;
    
    const res = await axios.get(apiEndpoint);
    
    if (res.data && res.data.length > 0) {
      const latestData = res.data[0];
      const timeStr = new Date(latestData.timestamp).toLocaleTimeString();
      
      if (logs.value.length === 0 || logs.value[0].timestamp !== latestData.timestamp) {
        logs.value.unshift({
          time: timeStr,
          value: latestData.value,
          status: latestData.status,
          timestamp: latestData.timestamp,
        });
        
        dataPoints.value.shift();
        dataPoints.value.push(latestData.value);
        
        await nextTick();
        drawGraph();
      }
    }
  } catch (err) {
    console.error("Fetch error:", err);
  }
};

const drawGraph = () => {
  const canvas = chartCanvas.value;
  if (!canvas) return;
  const ctx = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  const data = dataPoints.value;
  ctx.clearRect(0, 0, width, height);
  const max = Math.max(...data, 50) * 1.2;
  maxVal.value = Math.round(max);
  ctx.beginPath();
  ctx.strokeStyle = "#00f2ff";
  ctx.lineWidth = 2;
  const stepX = width / (data.length - 1);
  data.forEach((val, index) => {
    const x = index * stepX;
    const y = height - (val / max) * height;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();
  ctx.lineTo(width, height);
  ctx.lineTo(0, height);
  ctx.fillStyle = "rgba(0, 242, 255, 0.1)";
  ctx.fill();
};

onMounted(() => {
  fetchLatest(); 
});

onUnmounted(() => clearInterval(timer.value));
</script>

<style scoped>
/* Main Layout */
.page-shell { padding: 40px; color: white; min-height: 100vh; }
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 30px; }

/* Buttons */
.btn-primary { background: #238636; color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: bold; display: flex; align-items: center; gap: 8px; font-size: 1rem; transition: background 0.2s; }
.btn-primary:hover { background: #2ea043; }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-danger { background: #da3633; color: white; border: none; padding: 10px 20px; border-radius: 6px; cursor: pointer; font-weight: bold; }
.btn-link { background: none; border: none; color: #58a6ff; cursor: pointer; text-decoration: underline; font-size: 0.9em; }
.btn-ghost { background: transparent; border: 1px solid #30363d; color: #c9d1d9; padding: 10px 20px; border-radius: 6px; cursor: pointer; margin-right: 10px; }

/* Dashboard Cards */
.dashboard-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 20px; }
.card { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; }
.card-header { display: flex; justify-content: space-between; margin-bottom: 15px; border-bottom: 1px solid #30363d; padding-bottom: 10px; }
.stat-row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 0.95rem; }
.label { color: #8b949e; }
.value { color: #e6edf3; font-weight: 500; }
.value.highlight { color: #58a6ff; font-weight: bold; font-size: 1.1rem; }
.value.dimmed { color: #6e7681; font-size: 0.85rem; }
.empty-state { text-align: center; color: #8b949e; padding: 30px 0; font-style: italic; }
.info-card ul { padding-left: 20px; color: #8b949e; line-height: 1.6; }

/* Modal Popup */
.modal-overlay { position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.7); display: flex; justify-content: center; align-items: center; z-index: 1000; backdrop-filter: blur(5px); }
.modal-content { background: #161b22; width: 450px; border-radius: 12px; border: 1px solid #30363d; box-shadow: 0 20px 50px rgba(0,0,0,0.5); overflow: hidden; animation: slideUp 0.3s ease; }
.modal-header { padding: 20px; border-bottom: 1px solid #30363d; display: flex; justify-content: space-between; align-items: center; }
.modal-body { padding: 20px; }
.modal-footer { padding: 20px; background: #0d1117; text-align: right; border-top: 1px solid #30363d; }
.close-btn { background: none; border: none; color: #8b949e; font-size: 1.5rem; cursor: pointer; }

/* Inputs */
.form-group { margin-bottom: 20px; }
.form-group label { display: block; margin-bottom: 8px; color: #e6edf3; font-weight: 500; }
input, select { width: 100%; background: #0d1117; border: 1px solid #30363d; color: white; padding: 12px; border-radius: 6px; box-sizing: border-box; font-size: 1rem; }
input:focus, select:focus { border-color: #58a6ff; outline: none; }
.target-examples { margin-top: 8px; color: #9ba4c4; font-size: 12px; }
.example-link { color: #4da5dd; cursor: pointer; text-decoration: underline; margin: 0 4px; }

/* Graph Panel & Monitor */
.panel { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; margin-bottom: 20px; }
.card-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px; }
.live-indicator { display: flex; align-items: center; gap: 8px; }
.pulse-dot { width: 10px; height: 10px; background: #3fb950; border-radius: 50%; box-shadow: 0 0 0 0 rgba(63, 185, 80, 0.7); animation: pulse 1.5s infinite; }
.canvas-wrapper { background: #0d1117; border: 1px solid #30363d; border-radius: 6px; padding: 10px; }
canvas { width: 100%; height: auto; display: block; }
.graph-legend { display: flex; justify-content: space-between; font-size: 0.8em; color: #8b949e; margin-top: 5px; }

/* Table */
table { width: 100%; margin-top: 10px; border-collapse: collapse; }
th, td { padding: 8px; border-bottom: 1px solid #30363d; text-align: left; }
.text-green { color: #3fb950; }
.text-red { color: #f85149; }

/* Pills */
.pill { padding: 2px 10px; border-radius: 12px; font-size: 0.8rem; display: inline-block; }
.soft-pill { background: rgba(255,255,255,0.1); }
.status-success { background: rgba(63,185,80,0.2); color: #3fb950; }
.status-danger { background: rgba(248,81,73,0.2); color: #f85149; }

@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(63, 185, 80, 0.7); } 70% { box-shadow: 0 0 0 10px rgba(63, 185, 80, 0); } 100% { box-shadow: 0 0 0 0 rgba(63, 185, 80, 0); } }
@keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
</style>