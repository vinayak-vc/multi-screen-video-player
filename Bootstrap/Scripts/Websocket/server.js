const WebSocket = require('ws');

const HOST = '0.0.0.0';
const PORT = 8484;

const wss = new WebSocket.Server({ host: HOST, port: PORT });

// Track connected clients
const clients = new Set();

console.log(`WebSocket server running on ws://${HOST}:${PORT}`);

wss.on('connection', (ws, req) => {
  clients.add(ws);
  console.log('Client connected:', req.socket.remoteAddress);

  ws.on('message', message => {
    // Broadcast to all OTHER clients
    for (const client of clients) {
      if (client !== ws && client.readyState === WebSocket.OPEN) {
        client.send(message);
      }
    }
  });

  ws.on('close', () => {
    clients.delete(ws);
    console.log('Client disconnected');
  });

  ws.on('error', () => {
    clients.delete(ws);
  });
});
