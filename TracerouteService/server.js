const http = require('http');
const { exec } = require('child_process');

const PORT = 5051;
const isWindows = process.platform === 'win32';

function parseWindows(output) {
  const hops = [];
  const lines = output.split('\n');
  for (const line of lines) {
    const match = line.match(/^\s*(\d+)\s+([\d<]+\s+ms\s+[\d<]+\s+ms\s+[\d<]+\s+ms|\*\s+\*\s+\*)\s+(.+)?/);
    if (!match) continue;
    const hopNum = parseInt(match[1]);
    const timePart = match[2].trim();
    const ipPart = (match[3] || '').trim();

    if (timePart === '* * *' || !ipPart) {
      hops.push({ hop: hopNum, ip: '*', status: 'TimedOut', time: 0 });
    } else {
      const timeMatch = timePart.match(/([\d<]+)\s+ms/);
      const time = timeMatch ? parseInt(timeMatch[1].replace('<', '')) : 0;
      hops.push({ hop: hopNum, ip: ipPart.split(' ')[0], status: 'Success', time });
    }
  }
  return hops;
}

function parseLinux(output) {
  const hops = [];
  const lines = output.split('\n');
  for (const line of lines) {
    if (line.trim().startsWith('traceroute')) continue;
    const match = line.match(/^\s*(\d+)\s+(.+)$/);
    if (!match) continue;
    const hopNum = parseInt(match[1]);
    const rest = match[2].trim();
    if (rest.startsWith('*')) {
      hops.push({ hop: hopNum, ip: '*', status: 'TimedOut', time: 0 });
    } else {
      const parts = rest.split(/\s+/);
      const ip = parts[0];
      const time = parts.length >= 2 ? Math.round(parseFloat(parts[1])) : 0;
      hops.push({ hop: hopNum, ip, status: 'Success', time });
    }
  }
  return hops;
}

const server = http.createServer((req, res) => {
  if (req.method !== 'POST' || req.url !== '/trace') {
    res.writeHead(404);
    res.end();
    return;
  }

  let body = '';
  req.on('data', chunk => { body += chunk; });
  req.on('end', () => {
    let target;
    try {
      target = JSON.parse(body).target;
    } catch {
      res.writeHead(400);
      res.end(JSON.stringify({ error: 'Invalid JSON' }));
      return;
    }

    if (!target || !/^[a-zA-Z0-9.\-_]+$/.test(target)) {
      res.writeHead(400);
      res.end(JSON.stringify({ error: 'Invalid target' }));
      return;
    }

    const cmd = isWindows
      ? `tracert -d -h 30 -w 1000 ${target}`
      : `traceroute -n -q 1 -w 2 ${target}`;

    console.log(`[Trace] ${target} → ${cmd}`);

    exec(cmd, { timeout: 90000 }, (err, stdout) => {
      const hops = isWindows ? parseWindows(stdout || '') : parseLinux(stdout || '');
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ hops }));
      console.log(`[Trace] ${target} → ${hops.length} hops`);
    });
  });
});

server.listen(PORT, () => {
  console.log(`TracerouteService running on port ${PORT} (${isWindows ? 'Windows/tracert' : 'Linux/traceroute'})`);
});
