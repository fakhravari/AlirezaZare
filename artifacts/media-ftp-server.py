import os,socket,socketserver
from pathlib import Path
ROOT=Path('artifacts/media-ftp').resolve(); ROOT.mkdir(parents=True,exist_ok=True)
class Handler(socketserver.StreamRequestHandler):
 def handle(self):
  self.cwd='/'; self.data=None; self.wfile.write(b'220 test FTP ready\r\n')
  while True:
   line=self.rfile.readline()
   if not line: break
   cmd,_,arg=line.decode('utf8').strip().partition(' '); cmd=cmd.upper()
   def reply(s): self.wfile.write((s+'\r\n').encode())
   def path(s):
    p=(ROOT / (s if s.startswith('/') else self.cwd+'/'+s).lstrip('/')).resolve()
    if not p.is_relative_to(ROOT): raise ValueError('invalid path')
    return p
   try:
    if cmd=='USER': reply('331 password required')
    elif cmd=='PASS': reply('230 logged in')
    elif cmd=='SYST': reply('215 UNIX Type: L8')
    elif cmd=='FEAT': self.wfile.write(b'211-Features\r\n UTF8\r\n EPSV\r\n SIZE\r\n211 End\r\n')
    elif cmd in ('OPTS','TYPE','NOOP'): reply('200 OK')
    elif cmd=='PWD': reply('257 "'+self.cwd+'"')
    elif cmd=='CWD':
     p=path(arg)
     if p.is_dir(): self.cwd='/'+p.relative_to(ROOT).as_posix().strip('.'); reply('250 directory changed')
     else: reply('550 directory missing')
    elif cmd=='MKD': path(arg).mkdir(parents=True,exist_ok=True); reply('257 created')
    elif cmd in ('PASV','EPSV'):
     self.data=socket.socket(); self.data.bind(('127.0.0.1',0)); self.data.listen(1); port=self.data.getsockname()[1]
     reply('229 Entering Extended Passive Mode (|||%d|)'%port if cmd=='EPSV' else '227 Entering Passive Mode (127,0,0,1,%d,%d)'%(port//256,port%256))
    elif cmd in ('NLST','LIST'):
     p=path(arg or self.cwd)
     if not p.exists(): reply('550 missing')
     else:
      reply('150 opening data'); conn,_=self.data.accept()
      with conn:
       if p.is_dir(): conn.sendall(('\r\n'.join(x.name for x in p.iterdir())+'\r\n').encode())
       else: conn.sendall((p.name+'\r\n').encode())
      self.data.close(); self.data=None; reply('226 listing complete')
    elif cmd=='SIZE':
     p=path(arg); reply('213 %d'%p.stat().st_size if p.is_file() else '550 missing')
    elif cmd=='STOR':
     p=path(arg); reply('150 opening data'); conn,_=self.data.accept()
     with conn, p.open('wb') as f:
      while chunk:=conn.recv(65536): f.write(chunk)
     self.data.close(); self.data=None; reply('226 stored')
    elif cmd=='QUIT': reply('221 bye'); break
    else: reply('502 unsupported')
   except Exception: reply('550 failed')
class Server(socketserver.ThreadingTCPServer): allow_reuse_address=True; daemon_threads=True
Server(('127.0.0.1',21219),Handler).serve_forever()
