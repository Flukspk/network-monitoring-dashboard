<template>
  <aside class="sidebar">
    <div class="brand">
      <p class="brand-name">Network Monitoring</p>
      <p class="brand-tag text-muted">Senior project console</p>
    </div>

    <nav class="nav">
      <p class="nav-label">Monitor</p>
      <router-link
        v-for="item in visibleNavItems"
        :key="item.path"
        :to="item.path"
        class="nav-item"
        :class="{ 'active-link': $route.path.startsWith(item.path) }"
      >
        <span class="nav-icon">{{ item.icon }}</span>
        <span>{{ item.label }}</span>
        <span v-if="item.badgeRef?.value" class="nav-badge">{{ item.badgeRef.value }}</span>
      </router-link>
    </nav>

    <div class="sidebar-footer">
      <div class="status-card">
        <div>
          <p class="status-title">{{ latestTarget || 'No data' }}</p>
          <p class="text-muted">{{ latestLabel }}</p>
        </div>
        <span class="pill soft-pill">
          <span class="status-dot status-success"></span>
          {{ latestType || 'Healthy' }}
        </span>
      </div>
      <button class="logout-btn" @click="logout">Log out</button>
    </div>
  </aside>
</template>

<script setup>
import { computed, ref, onMounted } from "vue";
import { useRouter, useRoute } from "vue-router";

const router = useRouter();
const route = useRoute();

const latestTarget = ref('');
const latestType = ref('');
const latestLabel = ref('Loading...');
const targetCount = ref(null);
const downCount = ref(null);
const recentEventCount = ref(null);

function timeAgo(dateStr) {
  const diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  return `${Math.floor(diff / 3600)}h ago`;
}

async function fetchLatest() {
  try {
    const res = await fetch('/api/metrics/summary');
    const json = await res.json();
    const data = json?.data ?? json;
    if (!data?.length) return;
    targetCount.value = data.length;
    downCount.value = data.filter(d => d.status === 'Failed').length || null;
    const latest = data.reduce((a, b) =>
      new Date(a.lastProbe) > new Date(b.lastProbe) ? a : b
    );
    latestTarget.value = latest.target;
    latestType.value = latest.metricType;
    latestLabel.value = `Synced ${timeAgo(latest.lastProbe)}`;
  } catch {
    latestLabel.value = 'Unavailable';
  }
}

async function fetchRecentEvents() {
  try {
    const res = await fetch('/api/metrics/filter');
    const json = await res.json();
    const data = json?.data ?? json;
    if (!Array.isArray(data)) return;
    const cutoff = Date.now() - 24 * 60 * 60 * 1000;
    const count = data.filter(e =>
      e.status === 'Failed' && new Date(e.timestamp) > cutoff
    ).length;
    recentEventCount.value = count || null;
  } catch { /* silent */ }
}

onMounted(() => {
  fetchLatest();
  fetchRecentEvents();
  setInterval(fetchLatest, 15000);
  setInterval(fetchRecentEvents, 15000);
});

const navItems = [
  { path: "/dashboard", label: "Dashboard", icon: "📊", badgeRef: downCount },
  { path: "/test", label: "Testing", icon: "🧪", badgeRef: targetCount },
  { path: "/event", label: "Events", icon: "📅", badgeRef: recentEventCount },
  { path: "/users", label: "Users", icon: "👤" },
  { path: "/settings", label: "Settings", icon: "⚙️" },
  { path: "/profile", label: "My Profile", icon: "👤" },
];

const roleId = computed(() => {
  try {
    const raw = localStorage.getItem("user");
    if (!raw) return null;
    const u = JSON.parse(raw);
    return u?.roleId ?? null;
  } catch {
    return null;
  }
});

const visibleNavItems = computed(() => {
  const isAdmin = roleId.value === 1;
  return navItems.filter((item) => {
    if (item.path === "/users" || item.path === "/settings") return isAdmin;
    if (item.path === "/profile") return true;
    return true;
  });
});

function logout() {
  localStorage.removeItem("user");
  router.push("/login");
}
</script>

<style scoped>
.sidebar {
  width: clamp(200px, 14vw, 220px);
  min-height: 100vh;
  padding: 24px 20px;
  background: rgba(5, 6, 8, 0.9);
  border-right: 1px solid rgba(255, 255, 255, 0.04);
  backdrop-filter: blur(28px);
  display: flex;
  flex-direction: column;
  gap: 28px;
}

.brand {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.brand-name {
  font-size: 16px;
  font-weight: 600;
  color: white;
}

.brand-tag {
  font-size: 12px;
  color: #8b949e;
}

.nav {
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

.nav-label {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.28em;
  color: #8b949e;
  margin-bottom: 8px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 6px;
  text-decoration: none;
  color: #c9d1d9; /* text-primary default */
  position: relative;
  transition: all 0.2s ease;
  font-size: 14px;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.05);
  color: white;
}

/* Active State */
.nav-item.router-link-active,
.active-link {
  background: rgba(77, 165, 221, 0.15);
  border: 1px solid rgba(77, 165, 221, 0.3);
  color: #4da5dd;
}

.nav-icon {
  font-size: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.nav-badge {
  margin-left: auto;
  font-size: 11px;
  font-weight: 600;
  padding: 2px 8px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.sidebar-footer {
  display: flex;
  flex-direction: column;
  gap: 16px;
  margin-top: auto;
}

.status-card {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 8px;
  padding: 12px;
  background: rgba(255, 255, 255, 0.02);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.status-title {
  font-size: 13px;
  font-weight: 600;
  color: white;
  margin: 0;
}

.text-muted {
  font-size: 11px;
  color: #8b949e;
  margin: 0;
}

.pill {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: #3fb950;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: #3fb950;
  box-shadow: 0 0 8px rgba(63, 185, 80, 0.4);
}

.logout-btn {
  width: 100%;
  border: 1px solid rgba(248, 81, 73, 0.2);
  border-radius: 6px;
  padding: 10px 0;
  font-weight: 600;
  font-size: 14px;
  background: rgba(248, 81, 73, 0.1);
  color: #f85149;
  cursor: pointer;
  transition: all 0.2s ease;
}

.logout-btn:hover {
  background: rgba(248, 81, 73, 0.2);
  transform: translateY(-1px);
}

@media (max-width: 960px) {
  .sidebar {
    display: none;
  }
}
</style>