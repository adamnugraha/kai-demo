#!/usr/bin/env python3
import argparse,json,os,urllib.request,urllib.error,uuid,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
token=os.getenv('LAB_TOKEN','')
if not token and (root/'.env').exists():
 for line in (root/'.env').read_text().splitlines():
  if line.startswith('LAB_TOKEN='):token=line.split('=',1)[1]
p=argparse.ArgumentParser();p.add_argument('action',choices=['status','book','fault-on','fault-off','dispatch']);p.add_argument('--url',default='http://127.0.0.1:8080');p.add_argument('--mock',default='http://127.0.0.1:8082');a=p.parse_args()
if not token:raise SystemExit('Set LAB_TOKEN locally or create .env.')
data=None;method='GET';url=a.url+'/api/ops';headers={'X-Lab-Token':token,'Content-Type':'application/json'}
if a.action=='book':url=a.url+'/api/bookings';method='POST';data={'trainId':'DEMO-01','passengerAlias':'workshop-demo','seats':1};headers['Idempotency-Key']=str(uuid.uuid4())
if a.action.startswith('fault-'):url=a.mock+'/admin/fault';method='POST';data={'enabled':a.action=='fault-on'}
if a.action=='dispatch':url=a.url+'/api/ops/dispatch';method='POST';data={}
req=urllib.request.Request(url,json.dumps(data).encode() if data is not None else None,headers,method=method)
try:
 with urllib.request.urlopen(req,timeout=180) as r:print(r.status,r.read().decode())
except urllib.error.HTTPError as e: print(e.code,e.read().decode());raise SystemExit(1)
