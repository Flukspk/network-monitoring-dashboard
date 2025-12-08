<template>
  <div class="page-shell">
    <header class="page-header">
      <div>
        <p class="eyebrow">Directory</p>
        <h1>User access</h1>
        <p class="text-muted">
          Overview of all accounts that can access the monitoring workspace.
        </p>
      </div>
      <div class="header-actions">
        <button class="btn-ghost">Export CSV</button>
        <button class="btn-primary">Invite user</button>
      </div>
    </header>

    <section class="panel users-table">
      <div v-if="loading" class="loading-state">
        Loading users...
      </div>

      <div v-else-if="users.length === 0" class="no-data">
        No users found in database.
      </div>

      <table v-else>
        <thead>
          <tr>
            <th>User</th>
            <th>Email</th>
            <th>Role</th>
            <th>Created</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td class="user-cell">
              <div class="avatar">{{ initials(user.name) }}</div>
              <div>
                <p class="user-name">{{ user.name }}</p>
                <p class="text-muted">{{ user.handle }}</p>
              </div>
            </td>
            <td>{{ user.email }}</td>
            <td>
              <span class="role-badge">{{ user.role }}</span>
            </td>
            <td class="text-muted">{{ user.created }}</td>
            <td>
              <span class="pill soft-pill">
                <span class="status-dot" :class="user.statusClass"></span>
                {{ user.status }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const users = ref([])
const loading = ref(true)

// --- Config API ---
const getApiBaseUrl = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
  if (window.location.port === "8080" || window.location.hostname !== "localhost") return "/api"
  return "http://localhost:5050"
}
const API_URL = getApiBaseUrl()

// --- Helper Functions ---
const initials = (name) => {
  if (!name) return '?'
  return name.slice(0, 2).toUpperCase()
}

// แปลง RoleId เป็นชื่อตำแหน่ง
const getRoleName = (roleId) => {
  if (roleId === 1) return 'Administrator'
  if (roleId === 2) return 'Operator'
  return 'Viewer'
}

// สร้างชื่อเล่นจาก Email (เช่น intouch@gmail.com -> Intouch)
const formatName = (email) => {
  if (!email) return 'Unknown'
  const namePart = email.split('@')[0]
  return namePart.charAt(0).toUpperCase() + namePart.slice(1)
}

// --- Fetch Data ---
const fetchUsers = async () => {
  try {
    loading.value = true
    const endpoint = API_URL.startsWith("/") ? `${API_URL}/users` : `${API_URL}/api/users`
    const res = await axios.get(endpoint)
    
    // แปลงข้อมูลจาก DB ให้เข้ากับ UI
    users.value = res.data.map(u => ({
      id: u.userId,
      email: u.username,
      name: formatName(u.username),
      handle: '@' + formatName(u.username).toLowerCase(),
      role: getRoleName(u.roleId),
      created: new Date(u.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }),
      
      // Mock Status (เพราะใน DB ยังไม่มี field นี้ ใส่เป็น Active ไปก่อน)
      status: 'Active',
      statusClass: 'status-success'
    }))

  } catch (err) {
    console.error("Failed to fetch users:", err)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchUsers()
})
</script>

<style scoped>
.page-shell {
  padding: 48px clamp(24px, 6vw, 72px);
  display: flex;
  flex-direction: column;
  gap: 28px;
  color: white;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 24px;
}

.header-actions { display: flex; gap: 12px; }

/* Buttons */
.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; }
.btn-ghost { background: transparent; border: 1px solid #30363d; color: #c9d1d9; padding: 8px 16px; border-radius: 6px; cursor: pointer; }

h1 { margin: 6px 0; font-size: clamp(28px, 4vw, 36px); }

/* Table Styles */
.panel { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 0 20px; overflow: hidden; }
.users-table table { width: 100%; border-collapse: collapse; }

th {
  text-align: left;
  font-size: 12px;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: #8b949e;
  padding: 20px 0;
  border-bottom: 1px solid #30363d;
}

td {
  padding: 16px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  color: #e6edf3;
}
tr:last-child td { border-bottom: none; }

.user-cell { display: flex; align-items: center; gap: 12px; }

.avatar {
  width: 40px; height: 40px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.1);
  color: #e6edf3;
  display: grid;
  place-items: center;
  font-weight: 600;
  font-size: 14px;
}

.user-name { font-weight: 600; font-size: 15px; }
.text-muted { color: #8b949e; font-size: 13px; }

/* Status Badge */
.pill { padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-flex; align-items: center; gap: 6px; }
.soft-pill { background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); }
.status-success { color: #3fb950; background: currentColor; }
.status-warning { color: #d29922; background: currentColor; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; }

.loading-state, .no-data { padding: 60px; text-align: center; color: #8b949e; font-style: italic; }

@media (max-width: 768px) {
  .page-header { flex-direction: column; }
  .users-table { overflow-x: auto; }
  table { min-width: 600px; }
}
</style>