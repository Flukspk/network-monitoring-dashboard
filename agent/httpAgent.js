import axios from 'axios';
import config from './config.json' assert { type: "json" };

async function runHttpTests() {
  const results = [];
  for (const url of config.httpTargets) {
    const start = Date.now();
    try {
      const response = await axios.get(url);
      const duration = Date.now() - start;
      results.push({
        url,
        status: response.status,
        responseTime: duration,
        timestamp: new Date()
      });
    } catch (err) {
      results.push({
        url,
        status: err.response?.status || 'Error',
        responseTime: null,
        timestamp: new Date()
      });
    }
  }

  // ส่งข้อมูลไป backend
  await axios.post(`${config.backendUrl}/api/metrics/http`, results);
}

setInterval(runHttpTests, config.intervalMs);
