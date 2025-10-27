import express from 'express';
import bodyParser from 'body-parser';
import pkg from 'pg';
const { Pool } = pkg;

const app = express();
app.use(bodyParser.json());

// อ่านค่าจาก environment
const pool = new Pool({
  host: process.env.DB_HOST,
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  port: 5432
});

// สร้าง table users
pool.query(`
CREATE TABLE IF NOT EXISTS users (
  user_id SERIAL PRIMARY KEY,     
  username TEXT NOT NULL UNIQUE,   
  password TEXT NOT NULL,          
  role TEXT NOT NULL DEFAULT 'user', 
  created_at TIMESTAMP DEFAULT NOW()
)
`).then(() => {
  return pool.query(`
    INSERT INTO users (username, password, role)
    SELECT 'fluk', '1234', 'admin'
    WHERE NOT EXISTS (
      SELECT 1 FROM users WHERE username='fluk'
    )
  `);
}).catch(err => console.error('Error creating user table:', err));

// สร้าง table ping_metrics พร้อม target และ target_type
pool.query(`
CREATE TABLE IF NOT EXISTS ping_metrics (
  id SERIAL PRIMARY KEY,
  target TEXT NOT NULL,
  target_type TEXT NOT NULL,        -- e.g., "ping", "http", "api"
  latency_ms FLOAT,
  packet_loss FLOAT,
  response_time_ms FLOAT,
  timestamp TIMESTAMP DEFAULT NOW()
)
`).catch(err => console.error('Error creating ping_metrics table:', err));

// API สำหรับรับ metrics
app.post('/api/metrics/ping', async (req, res) => {
  try {
    for (const metric of req.body) {
      await pool.query(
        `INSERT INTO ping_metrics(target, target_type, latency_ms, packet_loss, response_time_ms, timestamp) 
         VALUES($1,$2,$3,$4,$5,$6)`,
        [
          metric.target,
          metric.target_type,
          metric.latency_ms,
          metric.packet_loss,
          metric.response_time_ms,
          metric.timestamp
        ]
      );
    }
    res.sendStatus(200);
  } catch (err) {
    console.error(err);
    res.sendStatus(500);
  }
});

app.listen(5000, () => {
  console.log('Backend running on http://localhost:5000');
});
