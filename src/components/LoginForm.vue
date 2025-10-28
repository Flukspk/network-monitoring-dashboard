<template>
  <div class="login-wrapper">
    <div class="login-card">
      <h1 class="login-title">Login</h1>
      <p class="login-subtitle">Welcome back! Please login to your account</p>

      <form class="form-area" @submit.prevent="onSubmit">
        <!-- Email -->
        <div class="form-group">
          <label class="field-label" for="email">Email Address</label>
          <input
            id="email"
            v-model="email"
            class="input-box"
            type="email"
            placeholder="ABC123@gmail.com"
            required
            autocomplete="username"
          />
        </div>

        <!-- Password -->
        <div class="form-group">
          <label class="field-label" for="password">Password</label>
          <input
            id="password"
            v-model="password"
            class="input-box"
            type="password"
            placeholder="Enter your password"
            required
            autocomplete="current-password"
          />
        </div>

        <!-- Button -->
        <button class="login-btn" type="submit">Log In</button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const email = ref("");
const password = ref("");

async function onSubmit() {
  try {
    const response = await fetch("http://localhost:5001/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Username: email.value, Password: password.value }),
    });

    if (response.ok) {
      const data = await response.json();
      console.log("Login success:", data);
      alert(`Welcome ${data.username}!`);
      localStorage.setItem("user", JSON.stringify(data));
      router.push("/dashboard");
    } else if (response.status === 401) {
      alert("Invalid username or password.");
    } else {
      alert("Login failed. Please try again later.");
    }
  } catch (err) {
    console.error("Error logging in:", err);
    alert("Error connecting to server.");
  }
}
</script>

<style scoped>
.login-wrapper {
  height: 100vh;
  display: flex;
  justify-content: center; /* แนวนอน */
  align-items: center;     /* แนวตั้ง */
  background-color: #111;  /* สีพื้นหลัง */
}

.login-card {
  background-color: #1e1e1e;
  padding: 40px;
  border-radius: 12px;
  width: 350px;
  box-shadow: 0 8px 20px rgba(0,0,0,0.5);
  color: white;
  text-align: center;
}

.login-title {
  font-size: 24px;
  font-weight: bold;
  margin-bottom: 10px;
}

.login-subtitle {
  font-size: 14px;
  margin-bottom: 30px;
}

.form-group {
  margin-bottom: 20px;
  text-align: left;
}

.field-label {
  display: block;
  margin-bottom: 5px;
  font-size: 14px;
}

.input-box {
  width: 100%;
  padding: 10px;
  border-radius: 6px;
  border: none;
  background-color: #333;
  color: white;
}

.input-box::placeholder {
  color: #aaa;
}

.login-btn {
  width: 100%;
  padding: 12px;
  background-color: #00bcd4;
  border: none;
  border-radius: 6px;
  color: white;
  font-weight: bold;
  cursor: pointer;
}

.login-btn:hover {
  background-color: #00acc1;
}
</style>
