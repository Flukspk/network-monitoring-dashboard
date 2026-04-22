const apiUrl = import.meta.env.VITE_API_URL;

export async function pingBackend() {
  const res = await fetch(`${apiUrl}/api/ping`);  // endpoint ของ backend
  return res.json();
}
