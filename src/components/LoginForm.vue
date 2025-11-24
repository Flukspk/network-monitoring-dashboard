<template>
  <div class="login-page">
    <div class="login-shell panel">
      <div class="floating-badge">Senior project</div>
      <header class="form-head">
        <p class="eyebrow">Network monitoring</p>
        <h2>Welcome back</h2>
        <p class="text-muted">
          Sign in with the demo credentials to explore the project dashboard.
        </p>
      </header>

      <form class="form" @submit.prevent="onSubmit">
        <label class="field-label" for="email">Username</label>
        <input
          id="email"
          v-model="email"
          class="input-field"
          type="text"
          placeholder="fluk"
          required
          autocomplete="username"
        />

        <label class="field-label" for="password">Password</label>
        <input
          id="password"
          v-model="password"
          class="input-field"
          type="password"
          placeholder="Enter your password"
          required
          autocomplete="current-password"
        />

        <button class="btn-primary submit-btn" type="submit">Log in</button>
      </form>

      <p class="helper-text">
        Trouble signing in?
        <a class="support-link" href="mailto:support@pulsewatch.io"
          >Contact support</a
        >
      </p>
      <button
        class="btn-ghost skip-btn"
        type="button"
        @click="continueWithoutAuth"
      >
        Preview dashboard without login
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";

const router = useRouter();
const email = ref("");
const password = ref("");

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5162";

async function onSubmit() {
  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Username: email.value, Password: password.value }),
    });

    if (response.ok) {
      const data = await response.json();
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

function continueWithoutAuth() {
  router.push("/dashboard");
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  padding: 64px clamp(20px, 6vw, 120px);
  display: flex;
  align-items: center;
  justify-content: center;
}

.login-shell {
  width: min(520px, 100%);
  padding: clamp(32px, 5vw, 48px);
  border-radius: var(--radius-xl);
  border: 1px solid rgba(255, 255, 255, 0.08);
  background: rgba(4, 6, 12, 0.9);
  position: relative;
}

.floating-badge {
  position: absolute;
  top: 28px;
  right: 32px;
  padding: 6px 18px;
  border-radius: 999px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.06);
  font-size: 12px;
  letter-spacing: 0.2em;
  text-transform: uppercase;
}

.form-head {
  margin-bottom: 28px;
  padding-right: 72px;
}

.form-head h2 {
  font-size: clamp(28px, 4vw, 34px);
  margin: 8px 0;
}

.eyebrow {
  text-transform: uppercase;
  letter-spacing: 0.18em;
  font-size: 12px;
  color: var(--text-muted);
}

.form {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.field-label {
  font-size: 13px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  color: var(--text-muted);
}

.input-field {
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: var(--radius-md);
  background: rgba(10, 13, 23, 0.9);
  padding: 16px 18px;
  font-size: 16px;
  color: var(--text-primary);
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.input-field:focus {
  border-color: var(--accent);
  box-shadow: 0 0 0 3px rgba(77, 165, 221, 0.25);
  outline: none;
}

.submit-btn {
  width: 100%;
  margin-top: 12px;
  height: 56px;
  font-size: 16px;
}

.helper-text {
  margin-top: 20px;
  font-size: 14px;
  color: var(--text-muted);
  text-align: center;
}

.support-link {
  color: var(--accent-strong);
  text-decoration: none;
  font-weight: 600;
}

.support-link:hover {
  text-decoration: underline;
}

.skip-btn {
  width: 100%;
  margin-top: 12px;
}

@media (max-width: 640px) {
  .login-page {
    padding: 32px 20px;
  }

  .login-shell {
    padding: 28px 20px;
  }

  .floating-badge {
    position: static;
    margin-bottom: 16px;
    display: inline-flex;
  }

  .form-head {
    padding-right: 0;
  }
}
</style>
