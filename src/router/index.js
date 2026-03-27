import { createRouter, createWebHistory } from "vue-router";
import LoginForm from "../components/LoginForm.vue";
import MainLayout from "../layouts/MainLayout.vue";
import Dashboard from "../Dashboard.vue";
import Testing from "../pages/Testing.vue";
import Events from "../pages/Events.vue";
import Alerts from "../pages/Alerts.vue";
import Users from "../pages/Users.vue"; 
import SettingsPage from "../pages/SettingsPage.vue"; 
import AcceptInvite from "../pages/AcceptInvite.vue"; // 🔴 1. Import ไฟล์ AcceptInvite.vue เข้ามา

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
      { path: "dashboard", name: "Dashboard", component: Dashboard },
      { path: "test", name: "Testing", component: Testing },
      { path: "event", name: "Events", component: Events },
      { path: "alert", name: "Alerts", component: Alerts },
      { path: "users", name: "Users", component: Users }, 
      { path: "settings", name: "Settings", component: SettingsPage }, 
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;