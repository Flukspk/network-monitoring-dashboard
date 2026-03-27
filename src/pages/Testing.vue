<template>
  <div class="kuma-layout">
    <aside class="sidebar-monitors">
      <div class="sidebar-header">
        <button class="btn-add-monitor" @click="openModal">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
            <path d="M8 3v10M3 8h10" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          Add New Monitor
        </button>
      </div>
      
      <div class="monitor-list">
        <div 
          v-for="m in monitors" 
          :key="m.target"
          class="monitor-item"
          :class="{ active: selectedMonitor && selectedMonitor.target === m.target }"
          @click="selectMonitor(m)"
        >
          <span class="status-pill" :class="getStatusColor(m.status)"></span>
          <div class="monitor-info">
             <span class="monitor-name">{{ m.target }}</span>
             <span class="monitor-type">{{ m.type }}</span>
          </div>
          <span class="monitor-ping" v-if="m.latency > 0">{{ m.latency }}ms</span>
        </div>
        <div v-if="monitors.length === 0" class="empty-list">
          No monitors added yet.
        </div>
      </div>
    </aside>

    <main class="main-content" v-if="selectedMonitor">
      
      <div class="content-header">
        <div>
          <h1 class="target-title">{{ selectedMonitor.target }}</h1>
          <p class="target-subtitle">
            Monitor Uptime of {{ selectedMonitor.target }} ({{ selectedMonitor.type }})
            <span v-if="selectedMonitor.threshold" class="threshold-badge">
              • Alert Threshold: {{ selectedMonitor.threshold }} ms
            </span>
          </p>
        </div>
        <div class="header-actions">
          <button class="btn-delete" @click="deleteMonitor(selectedMonitor)">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
              <path d="M3 4h10M6 4V3a1 1 0 0 1 1-1h2a1 1 0 0 1 1 1v1m-7 2v7a2 2 0 0 0 2 2h4a2 2 0 0 0 2-2V6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
            </svg>
            Delete
          </button>
        </div>
      </div>

      <div class="card uptime-card">
        <div class="uptime-layout">
          <div class="uptime-bars-container">
            <div class="uptime-bars">
              <div 
                v-for="(log, idx) in getHistoryBars(selectedMonitor)" 
                :key="idx"
                class="bar-pill"
                :class="getStatusColor(log.status)"
                :title="log.time + ' | ' + log.latency + 'ms'"
              ></div>
            </div>
            <p class="check-interval">Check every 5 seconds</p>
          </div>
          <div class="big-badge" :class="getStatusColor(selectedMonitor.status)">
             {{ selectedMonitor.status === 'Success' ? 'Up' : (selectedMonitor.status === 'Pending' ? 'Wait' : 'Down') }}
          </div>
        </div>
      </div>

      <div class="stats-grid">
        <div class="stat-box">
          <p class="stat-label">Response<br>(Current)</p>
          <p class="stat-value" :class="{'text-red': selectedMonitor.threshold && selectedMonitor.latency > selectedMonitor.threshold}">
            {{ selectedMonitor.latency }} <span class="unit">ms</span>
          </p>
        </div>
        <div class="stat-box">
          <p class="stat-label">Avg. Response<br>(All time)</p>
          <p class="stat-value">{{ calculateAvgLatency(selectedMonitor) }} <span class="unit">ms</span></p>
        </div>
        <div class="stat-box">
          <p class="stat-label">Uptime<br>(Recorded)</p>
          <p class="stat-value">{{ calculateUptime(selectedMonitor) }} <span class="unit">%</span></p>
        </div>
        <div class="stat-box">
          <p class="stat-label">Status<br>(Latest)</p>
          <p class="stat-value" :class="getStatusTextClass(selectedMonitor.status)">{{ selectedMonitor.status }}</p>
        </div>
      </div>

      <div class="card graph-card" v-show="selectedMonitor.type !== 'TRACEROUTE'">
        <div class="graph-header">
           <span class="graph-title">Response Time (ms)</span>
        </div>
        <div class="canvas-wrapper">
          <canvas ref="chartCanvas" width="800" height="200"></canvas>
        </div>
      </div>

      <div class="card terminal-card" v-if="selectedMonitor.type === 'TRACEROUTE'">
         <div class="graph-header">
           <span class="graph-title">Traceroute Console (Latest Result)</span>
        </div>
        <div class="terminal-box">
          <div class="terminal-header">
            traceroute to {{ selectedMonitor.target }} ({{ selectedMonitor.target }}), 30 hops max, 32 byte packets
          </div>
          <div class="terminal-body">
            <div v-if="latestHops.length === 0" class="terminal-waiting">
               Waiting for traceroute data... (Takes up to 30 seconds)
            </div>
            <div v-for="hop in latestHops" :key="hop.hop" class="hop-line">
              <span class="hop-num">{{ hop.hop }}</span>
              <span class="hop-ip" v-if="hop.ip !== '*'">{{ hop.ip }} ({{ hop.ip }})</span>
              <span class="hop-ip timeout-ip" v-else>* * *</span>
              <span class="hop-time" v-if="hop.time > 0">{{ hop.time }} ms</span>
              <span class="hop-time timeout-text" v-else-if="hop.ip === '*'">Request timeout</span>
            </div>
          </div>
        </div>
      </div>

    </main>

    <main class="main-content empty-state" v-else>
      <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
          <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          <path d="M9 12l2 2 4-4" />
      </svg>
      <h2>Welcome to Live Monitor</h2>
      <p>Select a monitor from the left or add a new one.</p>
    </main>

    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-dialog">
        <div class="modal-header">
          <h2>Add New Monitor</h2>
          <button class="close-btn" @click="closeModal">✕</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label>Monitor Type</label>
            <select v-model="newType" class="form-control">
              <option value="PING">PING (IP or Domain)</option>
              <option value="HTTP">HTTP(s) (Website URL)</option>
              <option value="TRACEROUTE">TRACEROUTE (Path analysis)</option>
            </select>
          </div>
          
          <div class="form-group">
            <label>Target (IP / Hostname / URL)</label>
            <input 
              v-model="newTarget" 
              type="text" 
              class="form-control" 
              :placeholder="newType === 'HTTP' ? 'https://google.com' : '8.8.8.8'"
            />
            <small class="hint" v-if="newType === 'HTTP'">Must start with http:// or https://</small>
            <small class="hint" v-else>Do NOT include http://</small>
          </div>

          <div class="form-group" v-show="newType !== 'TRACEROUTE'">
            <label>Alert Threshold (ms)</label>
            <input 
              v-model="newThreshold" 
              type="number" 
              class="form-control" 
              placeholder="e.g. 500"
              min="1"
            />
            <small class="hint text-warning">Trigger line notification if latency exceeds this value</small>
          </div>

        </div>
        <div class="modal-footer">
          <button class="btn-cancel" @click="closeModal">Cancel</button>
          <button class="btn-save" @click="addMonitor">Save & Start</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from "vue";
import axios from "axios";

// --- State ---
const monitors = ref([]); 
const selectedMonitor = ref(null); 
const showModal = ref(false);

// Form State
const newTarget = ref("");
const newType = ref("PING");
const newThreshold = ref(500); // 🚨 ค่าเริ่มต้น Threshold 500ms

let loopInterval = null;
const chartCanvas = ref(null);

const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL;
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api";
  return "http://localhost:5050";
};
const API_BASE_URL = getApiBaseUrl();

// 💻 ดึงข้อมูล Hops ตัวล่าสุดของ Traceroute ออกมาแสดงผล
const latestHops = computed(() => {
  if (!selectedMonitor.value || selectedMonitor.value.type !== 'TRACEROUTE') return [];
  const history = selectedMonitor.value.history;
  if (!history || history.length === 0) return [];
  
  const latestLog = history[history.length - 1]; 
  if (!latestLog || !latestLog.extraData) return [];

  try {
    const parsed = JSON.parse(latestLog.extraData);
    return parsed.Detail?.Hops || parsed.Hops || [];
  } catch (e) {
    return [];
  }
});

onMounted(() => {
  const saved = localStorage.getItem("kuma_monitors");
  if (saved) {
    monitors.value = JSON.parse(saved);
    if (monitors.value.length > 0) {
      selectedMonitor.value = monitors.value[0];
    }
  }

  runLoop();
  loopInterval = setInterval(runLoop, 5000);
});

onUnmounted(() => {
  if (loopInterval) clearInterval(loopInterval);
});

watch(selectedMonitor, () => {
  if (selectedMonitor.value && selectedMonitor.value.type !== 'TRACEROUTE') {
    nextTick(() => { drawGraph(); });
  }
});

// --- Modal Functions ---
const openModal = () => {
  newTarget.value = "";
  newType.value = "PING";
  newThreshold.value = 500; // 🚨 รีเซ็ตเป็น 500 ตอนเปิด Modal ใหม่
  showModal.value = true;
};
const closeModal = () => { showModal.value = false; };

const addMonitor = () => {
  let target = newTarget.value.trim().toLowerCase();
  
  if (!target) return alert("Target cannot be empty!");
  if (newType.value === "HTTP" && !target.startsWith("http")) {
    return alert("HTTP monitor must start with http:// or https://");
  }
  if ((newType.value === "PING" || newType.value === "TRACEROUTE") && target.startsWith("http")) {
    return alert(`${newType.value} monitor cannot start with http://`);
  }
  // 🚨 Validation สำหรับ Threshold
  if (newType.value !== "TRACEROUTE" && (!newThreshold.value || newThreshold.value <= 0)) {
    return alert("Please enter a valid threshold (greater than 0 ms).");
  }

  if (monitors.value.some(m => m.target === target && m.type === newType.value)) {
    return alert("This target & type is already being monitored!");
  }

  const newMon = {
    target: target,
    type: newType.value,
    threshold: newType.value === 'TRACEROUTE' ? null : newThreshold.value, // 🚨 บันทึกค่าลงตัวแปร
    status: "Pending",
    latency: 0,
    history: []
  };

  monitors.value.push(newMon);
  saveToLocal();
  selectedMonitor.value = newMon;
  closeModal();
};

const deleteMonitor = (mon) => {
  if(!confirm(`Are you sure you want to delete ${mon.target}?`)) return;
  monitors.value = monitors.value.filter(m => !(m.target === mon.target && m.type === mon.type));
  selectedMonitor.value = monitors.value.length > 0 ? monitors.value[0] : null;
  saveToLocal();
};

const selectMonitor = (mon) => {
  selectedMonitor.value = mon;
};

const saveToLocal = () => {
  localStorage.setItem("kuma_monitors", JSON.stringify(monitors.value));
};

// --- Core Logic ---
const runLoop = async () => {
  if (monitors.value.length === 0) return;

  const types = ["PING", "HTTP", "TRACEROUTE"];
  for (const t of types) {
    const targetsOfType = monitors.value.filter(m => m.type === t).map(m => m.target);
    if (targetsOfType.length > 0) {
      try {
        const endpoint = API_BASE_URL.startsWith("/") ? `${API_BASE_URL}/agent/run-batch` : `${API_BASE_URL}/api/agent/run-batch`;
        await axios.post(endpoint, {
          AgentId: t,
          Targets: targetsOfType,
          MetricType: t
        });
      } catch (err) {
        console.error(`Error running ${t}:`, err);
      }
    }
  }

  setTimeout(fetchLatestData, 800);
};

const fetchLatestData = async () => {
  try {
    const endpoint = API_BASE_URL.startsWith("/") ? `${API_BASE_URL}/metrics/filter` : `${API_BASE_URL}/api/metrics/filter`;
    const res = await axios.get(endpoint);
    
    if (!res.data || res.data.length === 0) return;

    const grouped = {};
    res.data.forEach(d => {
      const key = `${d.target}_${d.metricType}`;
      if (!grouped[key]) grouped[key] = [];
      grouped[key].push(d);
    });

    let needsGraphRedraw = false;
    monitors.value.forEach(m => {
      const key = `${m.target}_${m.type}`;
      if (grouped[key]) {
        const dbHistory = grouped[key].slice(0, 40).reverse();
        
        m.history = dbHistory.map(h => ({
          latency: h.value,
          status: h.status,
          time: new Date(h.timestamp).toLocaleTimeString(),
          extraData: h.extraData
        }));

        const latest = m.history[m.history.length - 1];
        if (latest) {
          m.status = latest.status;
          m.latency = latest.latency;
        }

        if (selectedMonitor.value && m.target === selectedMonitor.value.target && m.type === selectedMonitor.value.type) {
           if (m.type !== 'TRACEROUTE') needsGraphRedraw = true;
        }
      }
    });

    saveToLocal();

    if (needsGraphRedraw) {
      nextTick(() => { drawGraph(); });
    }

  } catch (err) {
    console.error("Fetch Data Error:", err);
  }
};

// --- UI Helpers ---
const getStatusColor = (status) => {
  if (status === "Success") return "up";
  if (status === "Investigate" || status === "Failed" || status === "Error") return "down";
  return "pending";
};

const getStatusTextClass = (status) => {
  if (status === "Success") return "text-green";
  if (status === "Investigate" || status === "Failed") return "text-red";
  return "text-gray";
};

const getHistoryBars = (mon) => {
  const maxBars = 40;
  const bars = [...mon.history];
  while (bars.length < maxBars) {
    bars.unshift({ status: "Pending", latency: 0, time: "-" });
  }
  return bars;
};

const calculateAvgLatency = (mon) => {
  if (!mon.history || mon.history.length === 0) return 0;
  const valid = mon.history.filter(h => h.latency > 0);
  if (valid.length === 0) return 0;
  const sum = valid.reduce((acc, curr) => acc + curr.latency, 0);
  return Math.round(sum / valid.length);
};

const calculateUptime = (mon) => {
  if (!mon.history || mon.history.length === 0) return 100;
  const valid = mon.history.filter(h => h.status !== "Pending");
  if (valid.length === 0) return 100;
  const up = valid.filter(h => h.status === "Success").length;
  return Math.round((up / valid.length) * 100);
};

// --- Graph Drawing ---
const drawGraph = () => {
  const canvas = chartCanvas.value;
  if (!canvas || !selectedMonitor.value || selectedMonitor.value.type === 'TRACEROUTE') return;
  const ctx = canvas.getContext("2d");
  const width = canvas.width;
  const height = canvas.height;
  
  const data = selectedMonitor.value.history.map(h => h.latency);
  if (data.length === 0) return;

  ctx.clearRect(0, 0, width, height);

  const maxVal = Math.max(...data, 50) * 1.2;

  ctx.strokeStyle = "rgba(255, 255, 255, 0.05)";
  ctx.lineWidth = 1;
  for (let i = 0; i <= 4; i++) {
    const y = (height / 4) * i;
    ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(width, y); ctx.stroke();
  }

  // 🚨 ถ้ายูสเซอร์ตั้ง Threshold และค่าปิงปัจจุบันดันเกิน ให้วาดเส้นเป็นสีเหลืองเตือนภัย!
  ctx.beginPath();
  let lineColor = "#50b83c"; // เขียว
  if (selectedMonitor.value.status !== 'Success') {
    lineColor = "#e74c3c"; // แดง
  } else if (selectedMonitor.value.threshold && selectedMonitor.value.latency > selectedMonitor.value.threshold) {
    lineColor = "#d29922"; // เหลืองเตือน
  }
  
  ctx.strokeStyle = lineColor;
  ctx.lineWidth = 2;
  ctx.lineJoin = "round";

  const stepX = width / Math.max(data.length - 1, 1);
  data.forEach((val, index) => {
    const x = index * stepX;
    const y = height - (val / maxVal) * height;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();

  ctx.lineTo(width, height); 
  ctx.lineTo(0, height); 
  ctx.closePath();
  const gradient = ctx.createLinearGradient(0, 0, 0, height);
  // ลงสีใต้กราฟให้เข้ากับเส้น
  if (lineColor === "#50b83c") {
    gradient.addColorStop(0, "rgba(80, 184, 60, 0.3)");
    gradient.addColorStop(1, "rgba(80, 184, 60, 0.01)");
  } else if (lineColor === "#e74c3c") {
    gradient.addColorStop(0, "rgba(231, 76, 60, 0.3)");
    gradient.addColorStop(1, "rgba(231, 76, 60, 0.01)");
  } else {
    gradient.addColorStop(0, "rgba(210, 153, 34, 0.3)");
    gradient.addColorStop(1, "rgba(210, 153, 34, 0.01)");
  }
  ctx.fillStyle = gradient; 
  ctx.fill();
};
</script>

<style scoped>
.kuma-layout {
  display: flex;
  min-height: 100vh;
  background-color: #0b0e14;
  color: #e6edf3;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
}

.sidebar-monitors { width: 280px; background-color: #161b22; border-right: 1px solid #30363d; display: flex; flex-direction: column; }
.sidebar-header { padding: 16px; border-bottom: 1px solid #30363d; }
.btn-add-monitor { width: 100%; display: flex; align-items: center; justify-content: center; gap: 8px; background-color: #50b83c; color: #fff; border: none; padding: 10px; border-radius: 20px; font-weight: 600; cursor: pointer; transition: opacity 0.2s; }
.btn-add-monitor:hover { opacity: 0.8; }
.monitor-list { flex: 1; overflow-y: auto; padding: 8px; display: flex; flex-direction: column; gap: 4px; }
.empty-list { text-align: center; color: #8b949e; font-size: 13px; padding: 20px 0; }
.monitor-item { display: flex; align-items: center; gap: 12px; padding: 10px 12px; border-radius: 8px; cursor: pointer; transition: background 0.2s; }
.monitor-item:hover { background-color: rgba(255, 255, 255, 0.05); }
.monitor-item.active { background-color: #1f242c; }
.status-pill { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.up { background-color: #50b83c; color: #50b83c; } 
.down { background-color: #e74c3c; color: #e74c3c; } 
.pending { background-color: #8b949e; color: #8b949e; } 
.monitor-info { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.monitor-name { font-size: 14px; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.monitor-type { font-size: 10px; color: #8b949e; font-family: monospace; letter-spacing: 0.5px;}
.monitor-ping { font-size: 12px; color: #8b949e; font-family: monospace; }

.main-content { flex: 1; padding: 32px 40px; overflow-y: auto; display: flex; flex-direction: column; gap: 24px; }
.empty-state { align-items: center; justify-content: center; color: #8b949e; text-align: center; }
.empty-state svg { margin-bottom: 16px; opacity: 0.5; }
.content-header { display: flex; justify-content: space-between; align-items: flex-start; }
.target-title { margin: 0; font-size: 28px; font-weight: 600; color: #fff; }
.target-subtitle { margin: 6px 0 0 0; font-size: 14px; color: #8b949e; display: flex; align-items: center; gap: 8px; }
.threshold-badge { background: rgba(210, 153, 34, 0.15); color: #d29922; padding: 2px 8px; border-radius: 12px; font-size: 12px; }

.btn-delete { display: flex; align-items: center; gap: 6px; background-color: #e74c3c; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; transition: opacity 0.2s; }
.btn-delete:hover { opacity: 0.8; }
.card { background-color: #161b22; border: 1px solid #30363d; border-radius: 12px; padding: 24px; }
.uptime-card { padding: 32px; }
.uptime-layout { display: flex; align-items: center; justify-content: space-between; gap: 40px; }
.uptime-bars-container { flex: 1; }
.uptime-bars { display: flex; gap: 4px; height: 48px; align-items: center; }
.bar-pill { flex: 1; height: 100%; border-radius: 4px; transition: all 0.2s ease; cursor: pointer; }
.bar-pill:hover { opacity: 0.7; transform: scaleY(1.1); }
.check-interval { margin: 12px 0 0 0; font-size: 12px; color: #8b949e; }
.big-badge { font-size: 24px; font-weight: 700; padding: 12px 32px; border-radius: 30px; color: white !important; text-transform: uppercase; letter-spacing: 1px; }
.big-badge.up { background-color: #50b83c; box-shadow: 0 4px 15px rgba(80, 184, 60, 0.3); }
.big-badge.down { background-color: #e74c3c; box-shadow: 0 4px 15px rgba(231, 76, 60, 0.3); }

.stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 20px; }
.stat-box { background-color: #161b22; border: 1px solid #30363d; border-radius: 12px; padding: 20px; text-align: center; }
.stat-label { margin: 0 0 12px 0; font-size: 13px; color: #8b949e; text-transform: uppercase; letter-spacing: 0.5px; }
.stat-value { margin: 0; font-size: 24px; font-weight: 600; color: #fff; }
.stat-value .unit { font-size: 14px; color: #8b949e; font-weight: normal; }
.text-green { color: #50b83c !important; }
.text-red { color: #e74c3c !important; }
.text-warning { color: #d29922 !important; }
.text-gray { color: #8b949e !important; }

.graph-card, .terminal-card { padding: 20px; }
.graph-header { margin-bottom: 20px; }
.graph-title { font-size: 14px; color: #8b949e; font-weight: 500; }
.canvas-wrapper { width: 100%; height: 200px; }
canvas { width: 100% !important; height: 100% !important; }

.terminal-box { background-color: #000000; border-radius: 8px; padding: 16px; font-family: "SFMono-Regular", Consolas, monospace; font-size: 13px; color: #e6edf3; border: 1px solid #30363d; box-shadow: inset 0 0 20px rgba(0,0,0,0.8); min-height: 200px; }
.terminal-header { color: #8b949e; margin-bottom: 12px; padding-bottom: 12px; border-bottom: 1px dashed #30363d; }
.terminal-waiting { color: #d29922; font-style: italic; animation: pulse 1.5s infinite; }
.hop-line { display: flex; gap: 15px; padding: 4px 0; line-height: 1.4; }
.hop-num { width: 25px; text-align: right; color: #8b949e; }
.hop-ip { color: #58a6ff; min-width: 280px; } 
.hop-time { color: #3fb950; } 
.timeout-ip { color: #8b949e; }
.timeout-text { color: #e74c3c; font-style: italic; }

.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 999; }
.modal-dialog { background: #161b22; border: 1px solid #30363d; border-radius: 12px; width: 400px; max-width: 90%; }
.modal-header { display: flex; justify-content: space-between; padding: 20px; border-bottom: 1px solid #30363d; }
.modal-header h2 { margin: 0; font-size: 18px; }
.close-btn { background: none; border: none; color: #8b949e; font-size: 18px; cursor: pointer; }
.modal-body { padding: 20px; }
.form-group { margin-bottom: 16px; }
.form-group label { display: block; margin-bottom: 8px; font-size: 13px; color: #c9d1d9; }
.form-control { width: 100%; padding: 10px; background: #0b0e14; border: 1px solid #30363d; color: white; border-radius: 6px; }
.form-control:focus { outline: none; border-color: #50b83c; }
.hint { display: block; margin-top: 6px; font-size: 12px; color: #8b949e; }
.modal-footer { display: flex; justify-content: flex-end; gap: 10px; padding: 20px; border-top: 1px solid #30363d; }
.btn-cancel { background: transparent; color: #c9d1d9; border: 1px solid #30363d; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
.btn-save { background: #50b83c; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; }

@keyframes pulse { 0% { opacity: 0.5; } 50% { opacity: 1; } 100% { opacity: 0.5; } }
</style>