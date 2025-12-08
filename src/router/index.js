import { createRouter, createWebHistory } from "vue-router";
import LoginForm from "../components/LoginForm.vue";
import MainLayout from "../layouts/MainLayout.vue";
import Dashboard from "../Dashboard.vue";
import Testing from "../pages/Testing.vue";
import Events from "../pages/Events.vue";
import Alerts from "../pages/Alerts.vue";
import Users from "../pages/Users.vue"; // ✅ 1. Import ไฟล์ Users.vue

const routes = [
  { path: "/", redirect: "/login" },
  { path: "/login", name: "Login", component: LoginForm },
  {
    path: "/",
    component: MainLayout,
    children: [
      { path: "dashboard", name: "Dashboard", component: Dashboard },
      { path: "test", name: "Testing", component: Testing },
      { path: "event", name: "Events", component: Events },
      { path: "alert", name: "Alerts", component: Alerts },
      { path: "users", name: "Users", component: Users }, // ✅ 2. เพิ่ม Route ตรงนี้
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;