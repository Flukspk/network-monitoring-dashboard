import { createRouter, createWebHistory } from "vue-router";
import LoginForm from "../components/LoginForm.vue";
import MainLayout from "../layouts/MainLayout.vue";
import Dashboard from "../Dashboard.vue";
import Testing from "../pages/Testing.vue";
import Events from "../pages/Events.vue";
import Alerts from "../pages/Alerts.vue";
import Users from "../pages/Users.vue"; 
import SettingsPage from "../pages/SettingsPage.vue"; 
import AcceptInvite from "../pages/AcceptInvite.vue";
import ProfilePage from "../pages/ProfilePage.vue";

const routes = [
  { path: "/", redirect: "/login" },
  { path: "/login", name: "Login", component: LoginForm },
  
  // 🔴 2. เพิ่ม Route ใหม่ตรงนี้ (อยู่แยกออกมาเดี่ยวๆ แบบเดียวกับหน้า Login)
  { 
    path: "/accept-invite", 
    name: "AcceptInvite", 
    component: AcceptInvite 
  },

  {
    path: "/",
    component: MainLayout,
    children: [
      { path: "dashboard", name: "Dashboard", component: Dashboard, meta: { requiresAuth: true } },
      { path: "test", name: "Testing", component: Testing, meta: { requiresAuth: true } },
      { path: "event", name: "Events", component: Events, meta: { requiresAuth: true } },
      { path: "alert", name: "Alerts", component: Alerts, meta: { requiresAuth: true } },
      { path: "users", name: "Users", component: Users, meta: { requiresAuth: true, roles: [1] } }, 
      { path: "settings", name: "Settings", component: SettingsPage, meta: { requiresAuth: true, roles: [1] } },
      { path: "profile", name: "Profile", component: ProfilePage, meta: { requiresAuth: true } },
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

const getCurrentUser = () => {
  try {
    const raw = localStorage.getItem("user");
    if (!raw) return null;
    return JSON.parse(raw);
  } catch {
    return null;
  }
};

router.beforeEach((to) => {
  const isPublic = to.path === "/login" || to.path === "/accept-invite";
  const user = getCurrentUser();
  const isLoggedIn = !!user?.userId;

  if (!isPublic && to.meta?.requiresAuth && !isLoggedIn) {
    return { path: "/login" };
  }

  const allowedRoles = to.meta?.roles;
  if (allowedRoles && allowedRoles.length > 0) {
    const roleId = user?.roleId;
    if (!allowedRoles.includes(roleId)) {
      return { path: "/dashboard" };
    }
  }

  return true;
});

export default router;