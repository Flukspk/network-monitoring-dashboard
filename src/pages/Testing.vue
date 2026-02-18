<template>
  <div class="testing-page">
    <header class="page-header">
      <div class="header-content">
        <div class="header-text">
          <p class="eyebrow">Network Diagnostics</p>
          <h1>Live Agent Monitor</h1>
          <p class="subtitle">
            Real-time latency monitoring and network diagnostics
          </p>
        </div>
        <div class="header-actions">
          <button
            v-if="!isMonitoring"
            class="btn-primary btn-icon"
            @click="openModal"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M8 3v10M3 8h10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
            New Test
          </button>
          <button v-else class="btn-danger btn-icon" @click="stopMonitoring">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <rect x="4" y="4" width="8" height="8" rx="1" fill="currentColor" />
            </svg>
            Stop Monitoring
          </button>
        </div>
      </div>
    </header>

    <section v-if="!isMonitoring" class="dashboard-section">
        <div class="cards-grid">
            <div class="card activity-card">
               <div v-if="latestMetric" class="card-body">
                    <div class="metric-item">
                    <span class="metric-label">Target</span>
                    <span class="metric-value highlight">{{ latestMetric.target }}</span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label">Type</span>
                        <span class="metric-badge">{{ latestMetric.metricType }}</span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label">Status</span>
                        <span class="status-badge" :class="getStatusClass(latestMetric.status)">
                            <span class="status-dot"></span>
                            {{ latestMetric.status }}
                        </span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label">Latency</span>
                        <span class="metric-value large">{{ latestMetric.value }}<span class="unit">ms</span></span>
                    </div>
                    <div class="metric-item">
                        <span class="metric-label">Timestamp</span>
                        <span class="metric-value dimmed">{{ formatTime(latestMetric.timestamp) }}</span>
                    </div>
               </div>
               <div v-else class="empty-state">
                    <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                        <circle cx="12" cy="12" r="10" />
                        <path d="M12 8v4M12 16h.01" />
                    </svg>
                    <p>No recent activity</p>
                    <p class="empty-hint">Start a new test to see results here</p>
               </div>
            </div>

            <div class="card info-card">
                <div class="card-header">
                    <h3>Quick Start</h3>
                </div>
                <div class="card-body">
                    <ol class="steps-list">
                    <li>
                        <span class="step-number">1</span>
                        <div>
                        <strong>Click "New Test"</strong>
                        <p>Open the test configuration dialog</p>
                        </div>
                    </li>
                    <li>
                        <span class="step-number">2</span>
                        <div>
                        <strong>Select Agent & Targets</strong>
                        <p>Add one or multiple targets to monitor</p>
                        </div>
                    </li>
                    <li>
                        <span class="step-number">3</span>
                        <div>
                        <strong>Start Monitoring</strong>
                        <p>View real-time graph and logs</p>
                        </div>
                    </li>
                    </ol>
                </div>
            </div>
        </div>
    </section>

    <section v-else class="monitoring-section">
      <div class="stats-bar">
        <div class="stat-item">
          <span class="stat-label">Targets</span>
          <span class="stat-value" v-if="activeTargets.length > 1">{{ activeTargets.length }} Targets Active</span>
          <span class="stat-value" v-else>{{ activeTargets[0] }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Agent</span>
          <span class="stat-value">{{ selectedAgentMode }}</span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Status</span>
          <span class="live-badge">
            <span class="pulse-dot"></span>
            Live
          </span>
        </div>
        <div class="stat-item">
          <span class="stat-label">Updates</span>
          <span class="stat-value">{{ logs.length }} tests</span>
        </div>
      </div>

      <div class="card graph-card">
        <div class="card-header">
          <div class="card-title-group">
            <h3>Latency Graph</h3>
            <div class="graph-controls">
              <span class="graph-range">Min: {{ minVal }}ms</span>
              <span class="graph-range">Max: {{ maxVal }}ms</span>
            </div>
          </div>
        </div>
        <div class="graph-container">
          <canvas ref="chartCanvas" width="800" height="300"></canvas>
        </div>
      </div>

      <div class="card logs-card">
        <div class="card-header">
          <h3>Recent Logs</h3>
          <span class="logs-count">{{ logs.length }} entries</span>
        </div>
        <div class="table-container">
          <table class="logs-table">
            <thead>
              <tr>
                <th>Time</th>
                <th>Target</th>
                <th>Latency</th>
                <th>Status</th>
                <th>Info</th> </tr>
            </thead>
            <tbody>
              <tr v-if="logs.length === 0">
                <td colspan="5" class="empty-table">
                  <span>Waiting for test results...</span>
                </td>
              </tr>
              <tr v-for="(log, i) in logs" :key="i" class="log-row">
                <td class="log-time">{{ log.time }}</td>
                <td class="log-target">{{ log.target }}</td>
                <td class="log-value">
                  <span class="value-number">{{ log.value }}</span>
                  <span class="value-unit">ms</span>
                </td>
                <td>
                  <span class="status-badge" :class="getStatusClass(log.status)">
                    <span class="status-dot"></span>
                    {{ log.status }}
                  </span>
                </td>
                <td>
                  <button 
                    v-if="log.extraData" 
                    class="btn-icon-sm" 
                    @click="showLogDetails(log)"
                    title="View Details"
                  >
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                      <path d="M8 3.5a6.5 6.5 0 1 0 0 13 6.5 6.5 0 0 0 0-13zM8 5a3 3 0 1 1 0 6 3 3 0 0 1 0-6z" stroke="currentColor" stroke-width="1.5"/>
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </section>

    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-dialog">
        <div class="modal-header">
          <h2>Configure Test</h2>
          <button class="close-btn" @click="closeModal" aria-label="Close">
            <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
              <path d="M5 5l10 10M15 5l-10 10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label for="agent-select">
              <span>Agent Type</span>
              <span class="label-hint">Select the type of network test</span>
            </label>
            <select id="agent-select" v-model="selectedAgentMode" class="form-select">
              <option value="PING">Agent 1 - Ping (ICMP)</option>
              <option value="HTTP">Agent 2 - HTTP Request</option>
              <option value="TRACEROUTE">Agent 3 - Traceroute</option>
            </select>
          </div>

          <div class="form-group">
            <label>
                <span>Targets</span>
                <span class="label-hint">Add one or more targets to monitor</span>
            </label>
            <div class="target-rows-container">
                
                <div 
                v-for="(item, index) in targetRows" 
                :key="index" 
                class="target-row"
                >
                <div class="input-wrapper">
                    <span class="row-number">#{{ index + 1 }}</span>
                    <input
                    v-model="item.value"
                    type="text"
                    class="form-input target-input"
                    placeholder="e.g. 8.8.8.8"
                    @keydown.enter.prevent="addTargetRow" 
                    ref="targetInputs"
                    />
                </div>
                
                <button 
                    v-if="targetRows.length > 1 || item.value"
                    class="btn-icon-danger" 
                    @click="removeTargetRow(index)"
                    title="Remove"
                >
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                    <path d="M4 8h8" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                    </svg>
                </button>
                </div>

                <button class="btn-add-row" @click="addTargetRow">
                <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
                    <path d="M8 3v10M3 8h10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                </svg>
                Add Another Target
                </button>

            </div>
            
            <div class="examples-section">
                <p class="examples-label">Quick add:</p>
                <div class="examples-grid">
                    <template v-if="selectedAgentMode === 'PING'">
                        <button v-for="ex in pingExamples" :key="ex" class="example-chip" @click="fillExample(ex)">{{ ex }}</button>
                    </template>
                    <template v-else-if="selectedAgentMode === 'HTTP'">
                        <button v-for="ex in httpExamples" :key="ex" class="example-chip" @click="fillExample(ex)">{{ ex.replace('https://', '') }}</button>
                    </template>
                     <template v-else>
                        <button v-for="ex in traceExamples" :key="ex" class="example-chip" @click="fillExample(ex)">{{ ex }}</button>
                    </template>
                </div>
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-secondary" @click="closeModal">Cancel</button>
          <button
            class="btn-primary"
            @click="startTestFromModal"
            :disabled="!hasValidTargets"
          >
            <span v-if="isRunningTest">Starting...</span>
            <span v-else>Start Monitoring ({{ validTargetCount }})</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick } from "vue";
import axios from "axios";

// State
const isMonitoring = ref(false);
const showModal = ref(false);
const latestMetric = ref(null);
const selectedAgentMode = ref("PING");

// Multi-target State
const targetRows = ref([{ value: '' }]); // เริ่มต้น 1 ช่องเสมอ
const activeTargets = ref([]); // เก็บ Targets ที่กำลัง Monitor อยู่จริง

const logs = ref([]);
const dataPoints = ref(Array.from({ length: 20 }, () => 0));
const timer = ref(null);
const chartCanvas = ref(null);
const maxVal = ref(100);
const minVal = ref(0);
const isRunningTest = ref(false);

// Example targets
const pingExamples = ["1.1.1.1", "8.8.8.8", "8.8.4.4", "208.67.222.222", "9.9.9.9"];
const httpExamples = ["https://www.google.com", "https://www.github.com", "https://www.cloudflare.com", "https://httpbin.org/get"];
const traceExamples = ["8.8.8.8", "1.1.1.1", "google.com"];

// API Configuration
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api";
  return "http://localhost:5050";
};
const API_BASE_URL = getApiBaseUrl();

// Computed Properties for Validation
const validTargetCount = computed(() => {
    return targetRows.value.filter(r => r.value.trim() !== '').length;
});
const hasValidTargets = computed(() => validTargetCount.value > 0);


// Helper Functions
const formatTime = (timestamp) => new Date(timestamp).toLocaleString();
const getStatusClass = (status) => {
  if (status === "Success") return "status-success";
  if (status === "Investigate" || status === "Failed" || status === "Error") return "status-danger";
  return "status-warning";
};

// --- Target Row Functions ---
const addTargetRow = async () => {
  targetRows.value.push({ value: '' });
  await nextTick();
  // Focus ช่องใหม่ล่าสุด
  const inputs = document.querySelectorAll('.target-input');
  if(inputs.length > 0) inputs[inputs.length - 1].focus();
};

const removeTargetRow = (index) => {
    // ถ้าเหลือช่องเดียว ให้แค่เคลียร์ค่า ไม่ลบ row
    if (targetRows.value.length === 1) {
        targetRows.value[0].value = '';
    } else {
        targetRows.value.splice(index, 1);
    }
};

const fillExample = (val) => {
    // หาช่องว่างช่องแรก
    const emptyRow = targetRows.value.find(r => r.value.trim() === '');
    if (emptyRow) {
        emptyRow.value = val;
    } else {
        // ถ้าเต็มหมด เพิ่มช่องใหม่
        targetRows.value.push({ value: val });
    }
};

// Modal Actions
const openModal = () => {
  targetRows.value = [{ value: '' }]; // Reset
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

// Start Monitoring
const startTestFromModal = async () => {
  // ดึงค่าจาก Rows ออกมาเป็น Array String
  const finalTargets = targetRows.value
    .map(r => r.value.trim())
    .filter(v => v !== '');

  if (finalTargets.length === 0) {
    alert("Please enter at least one target");
    return;
  }

  // Set Active Targets
  activeTargets.value = [...finalTargets];

  showModal.value = false;
  isMonitoring.value = true;
  isRunningTest.value = true;

  // Reset data
  logs.value = [];
  dataPoints.value = Array.from({ length: 20 }, () => 0);

  // Run test immediately, then loop
  runBatchTest();
  timer.value = setInterval(runBatchTest, 5000); // 5 sec interval
  isRunningTest.value = false;
};

// Run Batch Test
const runBatchTest = async () => {
  if (activeTargets.value.length === 0) return;

  try {
    const apiEndpoint = API_BASE_URL.startsWith("/")
      ? `${API_BASE_URL}/agent/run-batch`
      : `${API_BASE_URL}/api/agent/run-batch`;

    await axios.post(apiEndpoint, {
      AgentId: selectedAgentMode.value,
      Targets: activeTargets.value, // ส่ง List ไป
      MetricType: selectedAgentMode.value,
    });

    // Wait and fetch
    setTimeout(() => {
      fetchData();
    }, 800);
  } catch (err) {
    console.error("Test error:", err);
  }
};

// Stop Monitoring
const stopMonitoring = async () => {
  isMonitoring.value = false;
  activeTargets.value = [];
  if (timer.value) {
    clearInterval(timer.value);
    timer.value = null;
  }
  fetchLatest();
};

const fetchLatest = async () => {
  try {
    const apiEndpoint = API_BASE_URL.startsWith("/") ? `${API_BASE_URL}/metrics/filter` : `${API_BASE_URL}/api/metrics/filter`;
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
    const apiEndpoint = API_BASE_URL.startsWith("/") ? `${API_BASE_URL}/metrics/filter` : `${API_BASE_URL}/api/metrics/filter`;
    const res = await axios.get(apiEndpoint);

    if (res.data && res.data.length > 0) {
      // Filter เฉพาะ Targets ที่เราสนใจ
      const relevantLogs = res.data
        .filter(d => activeTargets.value.includes(d.target))
        .slice(0, activeTargets.value.length * 2); 
      
      for (const data of relevantLogs) {
           const timeStr = new Date(data.timestamp).toLocaleTimeString();
           const exists = logs.value.some(l => l.timestamp === data.timestamp && l.target === data.target);
           
           if (!exists) {
                logs.value.unshift({
                    time: timeStr,
                    target: data.target,
                    value: data.value,
                    status: data.status,
                    timestamp: data.timestamp,
                    extraData: data.extraData // ✅ เก็บข้อมูล Hops ไว้
                });
                
                dataPoints.value.shift();
                dataPoints.value.push(data.value);
           }
      }

      if (logs.value.length > 50) logs.value = logs.value.slice(0, 50);

      await nextTick();
      drawGraph();
    }
  } catch (err) {
    console.error("Fetch error:", err);
  }
};

// ✅ ฟังก์ชันแสดงรายละเอียด (Traceroute Hops)
const showLogDetails = (log) => {
  if (!log.extraData) return;

  try {
    const data = typeof log.extraData === 'string' 
      ? JSON.parse(log.extraData) 
      : log.extraData;

    if (data.Hops || data.Detail?.Hops) {
      // รองรับทั้งโครงสร้างตรงๆ และแบบมี Detail
      const hops = data.Hops || data.Detail.Hops;
      let msg = `Trace Result for ${log.target}:\n\n`;
      
      hops.forEach(hop => {
        msg += `#${hop.hop} - ${hop.ip} (${hop.time}ms) [${hop.status}]\n`;
      });
      alert(msg);
    } else {
      alert(`Details:\n${data.Message || JSON.stringify(data, null, 2)}`);
    }
  } catch (e) {
    console.error("Parse error", e);
    alert("Raw Data: " + log.extraData);
  }
};

// Draw Graph (Same logic)
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
  const min = Math.min(...data.filter((d) => d > 0), 0);
  minVal.value = Math.round(min);

  // Grid
  ctx.strokeStyle = "rgba(255, 255, 255, 0.05)";
  ctx.lineWidth = 1;
  for (let i = 0; i <= 5; i++) {
    const y = (height / 5) * i;
    ctx.beginPath();
    ctx.moveTo(0, y);
    ctx.lineTo(width, y);
    ctx.stroke();
  }

  // Line
  ctx.beginPath();
  ctx.strokeStyle = "#4da5dd";
  ctx.lineWidth = 3;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";

  const stepX = width / (data.length - 1);
  data.forEach((val, index) => {
    const x = index * stepX;
    const y = height - (val / max) * height;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();

  // Fill
  ctx.lineTo(width, height);
  ctx.lineTo(0, height);
  ctx.closePath();
  const gradient = ctx.createLinearGradient(0, 0, 0, height);
  gradient.addColorStop(0, "rgba(77, 165, 221, 0.2)");
  gradient.addColorStop(1, "rgba(77, 165, 221, 0.01)");
  ctx.fillStyle = gradient;
  ctx.fill();

  // Dots
  ctx.fillStyle = "#4da5dd";
  data.forEach((val, index) => {
    if (val > 0) {
      const x = index * stepX;
      const y = height - (val / max) * height;
      ctx.beginPath();
      ctx.arc(x, y, 3, 0, Math.PI * 2);
      ctx.fill();
    }
  });
};

onMounted(() => {
  fetchLatest();
});

onUnmounted(() => {
  if (timer.value) clearInterval(timer.value);
});
</script>

<style scoped>
/* --- Styles ใหม่สำหรับ Target Rows --- */
.target-rows-container {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.target-row {
  display: flex;
  gap: 10px;
  align-items: center;
  animation: slideDown 0.2s ease;
}

.input-wrapper {
  position: relative;
  flex: 1;
  display: flex;
  align-items: center;
}

.row-number {
  position: absolute;
  left: 12px;
  color: #4da5dd;
  font-size: 12px;
  font-family: monospace;
  opacity: 0.7;
  pointer-events: none;
  z-index: 2;
}

.target-input {
  padding-left: 40px !important; /* เว้นที่ให้ตัวเลขลำดับ */
}

.btn-icon-danger {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 42px; /* ให้ปุ่มใหญ่กดง่าย */
  height: 42px;
  border: 1px solid #da3633;
  background: rgba(218, 54, 51, 0.1);
  color: #f85149;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  flex-shrink: 0;
}

.btn-icon-danger:hover {
  background: #da3633;
  color: white;
}

.btn-add-row {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  width: 100%;
  padding: 12px;
  border: 1px dashed #30363d;
  background: transparent;
  color: #8b949e;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 14px;
  margin-top: 4px;
}

.btn-add-row:hover {
  border-color: #4da5dd;
  color: #4da5dd;
  background: rgba(77, 165, 221, 0.05);
}

@keyframes slideDown {
  from { opacity: 0; transform: translateY(-10px); }
  to { opacity: 1; transform: translateY(0); }
}

/* เพิ่ม Style ให้ Column Target ใน Table */
.log-target {
    font-size: 13px;
    color: #e6edf3;
    font-weight: 500;
}

/* ... CSS เดิม ... */
.testing-page { min-height: 100vh; padding: 40px clamp(24px, 4vw, 60px); background: #050608; color: white; }
.page-header { margin-bottom: 32px; }
.header-content { display: flex; justify-content: space-between; align-items: flex-start; gap: 24px; }
.header-text { flex: 1; }
.eyebrow { text-transform: uppercase; letter-spacing: 0.2em; font-size: 12px; color: #8b949e; margin-bottom: 8px; }
.page-header h1 { font-size: clamp(28px, 4vw, 36px); font-weight: 700; margin: 0 0 8px 0; color: white; }
.subtitle { font-size: 15px; color: #8b949e; margin: 0; }
.btn-primary { display: inline-flex; align-items: center; gap: 8px; padding: 12px 24px; background: #238636; color: white; border: none; border-radius: 6px; font-weight: 600; font-size: 15px; cursor: pointer; transition: all 0.2s ease; }
.btn-primary:hover:not(:disabled) { background: #2ea043; transform: translateY(-1px); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; transform: none; }
.btn-danger { display: inline-flex; align-items: center; gap: 8px; padding: 12px 24px; background: #da3633; color: white; border: none; border-radius: 6px; font-weight: 600; font-size: 15px; cursor: pointer; transition: all 0.2s ease; }
.btn-danger:hover { background: #f85149; transform: translateY(-1px); }
.btn-secondary { padding: 10px 20px; background: transparent; border: 1px solid #30363d; color: white; border-radius: 6px; font-weight: 500; cursor: pointer; transition: all 0.2s ease; }
.btn-secondary:hover { border-color: #8b949e; background: rgba(255, 255, 255, 0.05); }
.btn-icon-sm { background: transparent; border: none; color: #8b949e; cursor: pointer; padding: 4px; border-radius: 4px; transition: all 0.2s ease; }
.btn-icon-sm:hover { color: white; background: rgba(255, 255, 255, 0.05); }
.cards-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 24px; }
.card { background: linear-gradient(135deg, #161b22 0%, #1c2128 100%); border: 1px solid #30363d; border-radius: 8px; padding: 24px; transition: transform 0.2s ease, box-shadow 0.2s ease; }
.card:hover { transform: translateY(-2px); box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3); }
.card-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 16px; border-bottom: 1px solid #30363d; }
.card-title-group { display: flex; justify-content: space-between; align-items: center; width: 100%; }
.card h3 { font-size: 18px; font-weight: 600; margin: 0; color: white; }
.metric-item { display: flex; justify-content: space-between; align-items: center; padding: 12px 0; border-bottom: 1px solid #30363d; }
.metric-item:last-child { border-bottom: none; }
.metric-label { font-size: 13px; color: #8b949e; font-weight: 500; }
.metric-value { font-size: 15px; color: white; font-weight: 600; }
.metric-value.highlight { color: #4da5dd; font-size: 16px; }
.metric-value.large { font-size: 20px; font-weight: 700; color: white; }
.metric-value.large .unit { font-size: 14px; font-weight: 500; margin-left: 4px; color: #8b949e; }
.metric-value.dimmed { color: #8b949e; font-size: 13px; font-weight: 400; }
.metric-badge { padding: 4px 12px; background: rgba(77, 165, 221, 0.15); color: #4da5dd; border-radius: 12px; font-size: 12px; font-weight: 600; }
.status-badge { display: inline-flex; align-items: center; gap: 6px; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; }
.status-badge.status-success { background: rgba(63, 185, 80, 0.15); color: #3fb950; }
.status-badge.status-danger { background: rgba(248, 81, 73, 0.15); color: #f85149; }
.status-badge.status-warning { background: rgba(210, 153, 34, 0.15); color: #d29922; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.empty-state { text-align: center; padding: 48px 24px; color: #8b949e; }
.empty-state svg { margin-bottom: 16px; opacity: 0.5; }
.empty-state p { margin: 8px 0; font-size: 15px; }
.empty-hint { font-size: 13px; opacity: 0.7; }
.steps-list { list-style: none; padding: 0; margin: 0; }
.steps-list li { display: flex; gap: 16px; margin-bottom: 24px; align-items: flex-start; }
.steps-list li:last-child { margin-bottom: 0; }
.step-number { display: flex; align-items: center; justify-content: center; width: 32px; height: 32px; background: #238636; color: white; border-radius: 50%; font-weight: 700; font-size: 14px; flex-shrink: 0; }
.steps-list li div { flex: 1; padding-top: 4px; }
.steps-list strong { display: block; color: white; margin-bottom: 4px; font-size: 14px; }
.steps-list p { color: #8b949e; font-size: 13px; margin: 0; }
.monitoring-section { display: flex; flex-direction: column; gap: 24px; }
.stats-bar { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 16px; padding: 20px; background: linear-gradient(135deg, #161b22 0%, #1c2128 100%); border: 1px solid #30363d; border-radius: 8px; }
.stat-item { display: flex; flex-direction: column; gap: 6px; }
.stat-label { font-size: 12px; color: #8b949e; text-transform: uppercase; letter-spacing: 0.05em; font-weight: 600; }
.stat-value { font-size: 16px; font-weight: 600; color: white; }
.live-badge { display: inline-flex; align-items: center; gap: 6px; padding: 4px 12px; background: rgba(63, 185, 80, 0.15); color: #3fb950; border-radius: 12px; font-size: 12px; font-weight: 600; }
.pulse-dot { width: 8px; height: 8px; background: #3fb950; border-radius: 50%; animation: pulse 2s infinite; }
@keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.7; transform: scale(1.2); } }
.graph-card { padding: 0; overflow: hidden; }
.graph-card .card-header { padding: 24px 24px 0 24px; margin-bottom: 0; }
.graph-controls { display: flex; gap: 16px; font-size: 12px; color: #8b949e; }
.graph-container { padding: 24px; background: rgba(13, 17, 23, 0.5); border-radius: 6px; margin: 20px; }
.graph-container canvas { width: 100%; height: auto; display: block; }
.logs-card .card-header { margin-bottom: 0; }
.logs-count { font-size: 12px; color: #8b949e; font-weight: 500; }
.table-container { max-height: 400px; overflow-y: auto; margin-top: 16px; }
.logs-table { width: 100%; border-collapse: collapse; }
.logs-table thead { position: sticky; top: 0; background: #161b22; z-index: 10; }
.logs-table th { padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: #8b949e; border-bottom: 2px solid #30363d; }
.log-row { transition: background 0.15s ease; }
.log-row:hover { background: rgba(255, 255, 255, 0.02); }
.log-row td { padding: 14px 16px; border-bottom: 1px solid #30363d; }
.log-time { font-size: 13px; color: #8b949e; font-family: "Monaco", "Menlo", monospace; }
.log-value { display: flex; align-items: baseline; gap: 4px; }
.value-number { font-size: 16px; font-weight: 600; color: white; }
.value-unit { font-size: 12px; color: #8b949e; }
.empty-table { text-align: center; padding: 48px; color: #8b949e; font-style: italic; }
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0, 0, 0, 0.75); backdrop-filter: blur(8px); display: flex; align-items: center; justify-content: center; z-index: 1000; animation: fadeIn 0.2s ease; }
.modal-dialog { background: linear-gradient(135deg, #161b22 0%, #1c2128 100%); border: 1px solid #30363d; border-radius: 8px; width: 90%; max-width: 520px; max-height: 90vh; overflow: hidden; box-shadow: 0 24px 64px rgba(0, 0, 0, 0.5); animation: slideUp 0.3s ease; display: flex; flex-direction: column; }
.modal-header { display: flex; justify-content: space-between; align-items: center; padding: 24px; border-bottom: 1px solid #30363d; }
.modal-header h2 { font-size: 20px; font-weight: 600; margin: 0; color: white; }
.close-btn { background: transparent; border: none; color: #8b949e; cursor: pointer; padding: 4px; border-radius: 4px; transition: all 0.2s ease; display: flex; align-items: center; justify-content: center; }
.close-btn:hover { color: white; background: rgba(255, 255, 255, 0.05); }
.modal-body { padding: 24px; overflow-y: auto; flex: 1; }
.modal-footer { display: flex; justify-content: flex-end; gap: 12px; padding: 20px 24px; border-top: 1px solid #30363d; background: rgba(13, 17, 23, 0.5); }
.form-group { margin-bottom: 24px; }
.form-group label { display: flex; flex-direction: column; gap: 6px; margin-bottom: 10px; font-size: 14px; font-weight: 600; color: white; }
.label-hint { font-size: 12px; font-weight: 400; color: #8b949e; }
.form-input, .form-select { width: 100%; padding: 12px 16px; background: rgba(13, 17, 23, 0.8); border: 1px solid #30363d; border-radius: 6px; color: white; font-size: 15px; transition: all 0.2s ease; font-family: inherit; }
.form-input:focus, .form-select:focus { outline: none; border-color: #4da5dd; box-shadow: 0 0 0 3px rgba(77, 165, 221, 0.1); background: rgba(13, 17, 23, 0.95); }
.form-select { cursor: pointer; }
.examples-section { margin-top: 16px; padding-top: 16px; border-top: 1px solid #30363d; }
.examples-label { font-size: 12px; color: #8b949e; margin-bottom: 10px; display: block; }
.examples-grid { display: flex; flex-wrap: wrap; gap: 8px; }
.example-chip { padding: 6px 12px; background: rgba(77, 165, 221, 0.1); border: 1px solid rgba(77, 165, 221, 0.2); border-radius: 16px; color: #4da5dd; font-size: 12px; font-weight: 500; cursor: pointer; transition: all 0.2s ease; }
.example-chip:hover { background: rgba(77, 165, 221, 0.2); border-color: #4da5dd; transform: translateY(-1px); }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
@keyframes slideUp { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
@media (max-width: 768px) { .testing-page { padding: 20px; } .header-content { flex-direction: column; align-items: stretch; } .cards-grid { grid-template-columns: 1fr; } .stats-bar { grid-template-columns: repeat(2, 1fr); } .modal-dialog { width: 95%; margin: 20px; } }
</style>