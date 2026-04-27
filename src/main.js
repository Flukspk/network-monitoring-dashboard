import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import "./assets/main.css";
import axios from "axios";
axios.defaults.headers.common['ngrok-skip-browser-warning'] = 'true';

const app = createApp(App);
app.use(router);
app.mount("#app");
