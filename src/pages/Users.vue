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
        <button class="btn-primary" @click="openInviteModal">Invite user</button>
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
            <th style="width: 50px;"></th> 
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
                <span :class="{'text-warning': user.status === 'Pending', 'text-success': user.status === 'Active'}">
                  {{ user.status }}
                </span>
              </span>
            </td>
            <td>
              <button class="btn-icon-delete" @click="deleteUser(user)" title="Delete User">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M3 6h18"></path><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path><line x1="10" y1="11" x2="10" y2="17"></line><line x1="14" y1="11" x2="14" y2="17"></line>
                </svg>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </section>

    <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
      <div class="modal-dialog">
        <div class="modal-header">
          <h2>Invite New User</h2>
          <button class="close-btn" @click="closeModal">✕</button>
        </div>
        
        <form @submit.prevent="submitInvite">
          <div class="modal-body">
            <div class="form-group">
              <label>Email Address (Username)</label>
              <input v-model="inviteForm.email" type="email" class="form-control" placeholder="user@company.com" required />
            </div>
            
            <div class="form-group">
              <label>Full Name</label>
              <input v-model="inviteForm.name" type="text" class="form-control" placeholder="John Doe" required />
            </div>

            <div class="form-group">
              <label>Role</label>
              <select v-model="inviteForm.roleId" class="form-control">
                <option value="1">Administrator</option>
                <option value="2">Operator</option>
                <option value="3">Viewer</option>
              </select>
            </div>
            
            <p class="hint text-muted" style="margin-top: 20px;">
              An email with an activation link will be sent to this user to set their own password.
            </p>
          </div>
          
          <div class="modal-footer">
            <button type="button" class="btn-cancel" @click="closeModal">Cancel</button>
            <button type="submit" class="btn-save" :disabled="isSubmitting">
              {{ isSubmitting ? 'Sending...' : 'Send Invitation' }}
            </button>
          </div>
        </form>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'

const users = ref([])
const loading = ref(true)

// --- Modal State ---
const showModal = ref(false)
const isSubmitting = ref(false)
const inviteForm = ref({
  email: '',
  name: '',
  roleId: 2 // Default: Operator
})

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

const getRoleName = (roleId) => {
  if (roleId === 1) return 'Administrator'
  if (roleId === 2) return 'Operator'
  return 'Viewer'
}

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
    
    users.value = res.data.map(u => ({
      id: u.userId,
      email: u.username,
      name: u.name || formatName(u.username),
      handle: '@' + (u.name ? u.name.split(' ')[0] : formatName(u.username)).toLowerCase(),
      role: getRoleName(u.roleId),
      created: new Date(u.createdAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }),
      
      status: u.status || 'Active',
      statusClass: u.status === 'Pending' ? 'status-warning' : 'status-success'
    }))
  } catch (err) {
    console.error("Failed to fetch users:", err)
  } finally {
    loading.value = false
  }
}

// 🔴 ฟังก์ชันลบ User
const deleteUser = async (user) => {
  if (!confirm(`Are you sure you want to delete user: ${user.email}? \nThis action cannot be undone.`)) {
    return;
  }

  try {
    const endpoint = API_URL.startsWith("/") ? `${API_URL}/users/${user.id}` : `${API_URL}/api/users/${user.id}`;
    await axios.delete(endpoint);
    
    // ดึงข้อมูลใหม่หลังจากลบสำเร็จ
    fetchUsers(); 
  } catch (err) {
    console.error("Failed to delete user:", err);
    alert(err.response?.data?.message || "Failed to delete user.");
  }
}

// --- Modal Functions ---
const openInviteModal = () => {
  inviteForm.value = { email: '', name: '', roleId: 2 }
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
}

// ส่งข้อมูลไปบันทึกและส่ง Email ที่ Backend
const submitInvite = async () => {
  isSubmitting.value = true
  try {
    const endpoint = API_URL.startsWith("/") ? `${API_URL}/users/invite` : `${API_URL}/api/users/invite`
    
    await axios.post(endpoint, {
      Username: inviteForm.value.email,
      Name: inviteForm.value.name,
      RoleId: parseInt(inviteForm.value.roleId)
    })

    alert("Invitation sent! The user will receive an email to set their password.")
    closeModal()
    fetchUsers() 
  } catch (err) {
    console.error("Invite error:", err)
    alert(err.response?.data?.message || "Failed to invite user.")
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  fetchUsers()
})
</script>

<style scoped>
/* สไตล์ตารางหลัก */
.page-shell { padding: 48px clamp(24px, 6vw, 72px); display: flex; flex-direction: column; gap: 28px; color: white; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 24px; }
.header-actions { display: flex; gap: 12px; }
.btn-primary { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; }
.btn-ghost { background: transparent; border: 1px solid #30363d; color: #c9d1d9; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
h1 { margin: 6px 0; font-size: clamp(28px, 4vw, 36px); }
.panel { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 0 20px; overflow: hidden; }
.users-table table { width: 100%; border-collapse: collapse; }
th { text-align: left; font-size: 12px; letter-spacing: 0.16em; text-transform: uppercase; color: #8b949e; padding: 20px 0; border-bottom: 1px solid #30363d; }
td { padding: 16px 0; border-bottom: 1px solid rgba(255, 255, 255, 0.06); color: #e6edf3; }
tr:last-child td { border-bottom: none; }
.user-cell { display: flex; align-items: center; gap: 12px; }
.avatar { width: 40px; height: 40px; border-radius: 10px; background: rgba(255, 255, 255, 0.1); color: #e6edf3; display: grid; place-items: center; font-weight: 600; font-size: 14px; }
.user-name { font-weight: 600; font-size: 15px; }
.text-muted { color: #8b949e; font-size: 13px; }

/* 🟢🟡 สถานะ (Pills) */
.pill { padding: 4px 12px; border-radius: 20px; font-size: 12px; display: inline-flex; align-items: center; gap: 6px; font-weight: 500;}
.soft-pill { background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); }
.status-success { color: #3fb950; background: currentColor; } 
.status-warning { color: #d29922; background: currentColor; } 
.text-success { color: #3fb950; }
.text-warning { color: #d29922; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; }
.loading-state, .no-data { padding: 60px; text-align: center; color: #8b949e; font-style: italic; }

/* 🔴 สไตล์ปุ่มลบ (ถังขยะ) */
.btn-icon-delete { background: transparent; border: none; color: #8b949e; padding: 8px; border-radius: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
.btn-icon-delete:hover { background: rgba(248, 81, 73, 0.1); color: #f85149; }

/* 🔵 Modal Styles */
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.8); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; z-index: 999; }
.modal-dialog { background: #161b22; border: 1px solid #30363d; border-radius: 12px; width: 450px; max-width: 90%; }
.modal-header { display: flex; justify-content: space-between; padding: 20px; border-bottom: 1px solid #30363d; }
.modal-header h2 { margin: 0; font-size: 18px; }
.close-btn { background: none; border: none; color: #8b949e; font-size: 18px; cursor: pointer; }
.modal-body { padding: 20px; }
.form-group { margin-bottom: 16px; }
.form-group label { display: block; margin-bottom: 8px; font-size: 13px; color: #c9d1d9; }
.form-control { width: 100%; padding: 10px; background: #0b0e14; border: 1px solid #30363d; color: white; border-radius: 6px; }
.form-control:focus { outline: none; border-color: #58a6ff; }
.hint { display: block; margin-top: 6px; font-size: 12px; color: #8b949e; }
.modal-footer { display: flex; justify-content: flex-end; gap: 10px; padding: 20px; border-top: 1px solid #30363d; }
.btn-cancel { background: transparent; color: #c9d1d9; border: 1px solid #30363d; padding: 8px 16px; border-radius: 6px; cursor: pointer; }
.btn-save { background: #238636; color: white; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; }
.btn-save:disabled { opacity: 0.6; cursor: not-allowed; }
</style>