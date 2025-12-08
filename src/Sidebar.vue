<template>
  <aside class="sidebar">
    <div class="brand">
      <p class="brand-name">Network Monitoring</p>
      <p class="brand-tag text-muted">Senior project console</p>
    </div>

    <nav class="nav">
      <p class="nav-label">Monitor</p>
      <router-link
        v-for="item in navItems"
        :key="item.path"
        :to="item.path"
        class="nav-item"
      >
        <span class="nav-icon">{{ item.icon }}</span>
        <span>{{ item.label }}</span>
        <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
      </router-link>
    </nav>

    <div class="sidebar-footer">
      <div class="status-card">
        <div>
          <p class="status-title">Edge agent</p>
          <p class="text-muted">Synced 2 minutes ago</p>
        </div>
        <span class="pill soft-pill">
          <span class="status-dot status-success"></span>
          Healthy
        </span>
      </div>
      <button class="logout-btn" @click="logout">Log out</button>
    </div>
  </aside>
</template>

<script setup>
import { useRouter } from "vue-router";

const router = useRouter();

const navItems = [
  { path: "/dashboard", label: "Dashboard", icon: "📊" },
  { path: "/test", label: "Testing", icon: "🧪", badge: "6" },
  { path: "/event", label: "Events", icon: "📅", badge: "2" },
  { path: "/alert", label: "Alerts", icon: "🚨", badge: "4" },
  { path: "/users", label: "Users", icon: "👤" },
];

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
}

.brand-tag {
  font-size: 12px;
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
  color: var(--text-muted);
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--text-primary);
  position: relative;
  transition: background 0.2s ease;
  font-size: 14px;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.05);
}

.nav-item.router-link-active {
  background: rgba(77, 165, 221, 0.18);
  border: 1px solid rgba(77, 165, 221, 0.35);
}

.nav-icon {
  font-size: 18px;
}

.nav-badge {
  margin-left: auto;
  font-size: 12px;
  padding: 2px 8px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
}

.sidebar-footer {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.status-card {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: var(--radius-lg);
  padding: 12px;
  background: rgba(255, 255, 255, 0.02);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.status-title {
  font-size: 13px;
  font-weight: 600;
}

.logout-btn {
  width: 100%;
  border: none;
  border-radius: var(--radius-md);
  padding: 10px 0;
  font-weight: 600;
  font-size: 14px;
  background: rgba(255, 255, 255, 0.08);
  color: var(--text-primary);
  cursor: pointer;
  transition: background 0.2s ease;
}

.logout-btn:hover {
  background: rgba(255, 255, 255, 0.14);
}

@media (max-width: 960px) {
  .sidebar {
    display: none;
  }
}
</style>
