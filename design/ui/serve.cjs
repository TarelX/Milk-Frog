// Optional local preview server; the prototype also opens directly from index.html.
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const mime = { '.html':'text/html; charset=utf-8', '.css':'text/css; charset=utf-8', '.js':'text/javascript; charset=utf-8', '.png':'image/png' };
http.createServer((req, res) => {
  if(req.method !== 'GET' && req.method !== 'HEAD'){res.writeHead(405);res.end();return;}
  let pathname;
  try{pathname=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname);}catch{res.writeHead(400);res.end();return;}
  const target = path.resolve(root, '.' + (pathname === '/' ? '/design/ui/index.html' : pathname));
  if(!target.startsWith(root + path.sep)){res.writeHead(403);res.end();return;}
  fs.readFile(target,(err,data)=>{if(err){res.writeHead(404);res.end('Not found');return;}res.writeHead(200,{'Content-Type':mime[path.extname(target)]||'application/octet-stream','Cache-Control':'no-store'});res.end(req.method==='HEAD'?undefined:data);});
}).listen(4177,'127.0.0.1',()=>process.stdout.write('Milk Frog design preview: http://127.0.0.1:4177/design/ui/index.html\n'));
