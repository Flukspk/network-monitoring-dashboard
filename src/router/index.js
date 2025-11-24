import { createRouter, createWebHistory } from "vue-router";
import LoginForm from "../components/LoginForm.vue";
import MainLayout from "../layouts/MainLayout.vue";
import Dashboard from "../Dashboard.vue";
import Testing from "../pages/Testing.vue";
import Events from "../pages/Events.vue";
import Alerts from "../pages/Alerts.vue";

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
    ],
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
