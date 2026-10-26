#!/usr/bin/env python3
"""Client-side search probe. Includes HTTP failures, timeouts and response validation."""
import json,os,time,threading,urllib.request
from http.server import BaseHTTPRequestHandler,HTTPServer
base=os.getenv('BASE_URL','http://app:8080').rstrip('/');token=os.environ['LAB_TOKEN'];lock=threading.Lock();counts={'eligible':0,'bad':0,'good_under_1s':0}
def run():
 while True:
  t=time.monotonic();good=False
  try:
   h={'X-Lab-Token':token}
   if os.getenv('GOOGLE_ID_TOKEN'):h['Authorization']='Bearer '+os.environ['GOOGLE_ID_TOKEN']
   with urllib.request.urlopen(urllib.request.Request(base+'/api/trains',headers=h),timeout=5) as r:
    data=json.load(r);good=r.status==200 and isinstance(data,list) and len(data)>0 and 'id' in data[0]
  except Exception:pass
  elapsed=time.monotonic()-t
  with lock:
   counts['eligible']+=1;counts['bad']+=not good;counts['good_under_1s']+=good and elapsed<=1
  time.sleep(max(0.05,0.25-elapsed))
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):
  if self.path!='/metrics':self.send_error(404);return
  with lock:body=''.join(f'# TYPE rail_probe_{k}_total counter\nrail_probe_{k}_total {v}\n' for k,v in counts.items()).encode()
  self.send_response(200);self.send_header('Content-Type','text/plain');self.end_headers();self.wfile.write(body)
 def log_message(self,*args):pass
threading.Thread(target=run,daemon=True).start();HTTPServer(('0.0.0.0',9100),Handler).serve_forever()
