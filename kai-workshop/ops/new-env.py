#!/usr/bin/env python3
import os, secrets
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'.env'
if p.exists(): raise SystemExit('.env already exists. Keep your current token.')
fd=os.open(p,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o600)
with os.fdopen(fd,'w') as f:f.write('LAB_TOKEN='+secrets.token_urlsafe(32)+'\nSQL_SA_PASSWORD=Aa1!'+secrets.token_urlsafe(24)+'\nSQL_APP_PASSWORD=Aa1!'+secrets.token_urlsafe(24)+'\n')
for name in ['app','mock']:
 d=root/'data'/name;d.mkdir(parents=True,exist_ok=True)
 # .NET container APP_UID is 1654. Change ownership explicitly on Linux.
 if hasattr(os,'geteuid') and os.geteuid()==0: os.chown(d,1654,1654)
print('Created .env with local secrets. Do not upload it. For rootless Docker adjust data directory ownership to match APP_UID.')
