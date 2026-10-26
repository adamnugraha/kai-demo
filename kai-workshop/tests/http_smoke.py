import json, os, urllib.request, urllib.error, uuid, time
base=os.getenv('BASE_URL','http://127.0.0.1:8080');mock=os.getenv('MOCK_URL','http://127.0.0.1:8082');token=os.environ['LAB_TOKEN']
def call(path,data=None,key=None,host=base):
 h={'X-Lab-Token':token,'Content-Type':'application/json'}
 if key:h['Idempotency-Key']=key
 req=urllib.request.Request(host+path,None if data is None else json.dumps(data).encode(),h,method='GET' if data is None else 'POST')
 try:
  with urllib.request.urlopen(req,timeout=20) as r:return r.status,json.load(r)
 except urllib.error.HTTPError as e:return e.code,json.loads(e.read() or '{}')
assert call('/api/trains')[0]==200
key=str(uuid.uuid4());payload={'trainId':'DEMO-01','passengerAlias':'smoke','seats':1}
assert call('/admin/fault',{'enabled':True},host=mock)[0]==200
status,book=call('/api/bookings',payload,key);assert status==201,(status,book)
status,replay=call('/api/bookings',payload,key);assert status==200 and replay['id']==book['id']
assert call('/api/ops')[1]['counts']['pending']>=1
call('/api/ops/dispatch',{});assert call('/api/ops')[1]['counts']['pending']>=1
call('/admin/fault',{'enabled':False},host=mock);time.sleep(2);assert call('/api/ops/dispatch',{})[0]==200
assert call('/api/ops')[1]['counts']['pending']==0
before=call('/stats',host=mock)[1]['uniqueNotifications'];call('/api/ops/dispatch',{});after=call('/stats',host=mock)[1]['uniqueNotifications'];assert before==after
print('PASS: search, reservation, replay, dependency outage, durable backlog, recovery, duplicate suppression')
