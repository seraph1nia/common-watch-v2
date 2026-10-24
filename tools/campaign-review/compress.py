#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Losslessly archive finished generated traces; verify decoded bytes before removing originals."""
import gzip
import hashlib
import json
from pathlib import Path
import sys
root=Path(sys.argv[1]).resolve()
rows=[]
for p in sorted(root.rglob('*')):
 if not p.is_file() or not (p.suffix=='.ndjson' or p.name in ('joined.json','before.json','after.json','final.json')):continue
 target=p.with_name(p.name+'.gz');temporary=target.with_name(target.name+'.tmp')
 if target.exists():raise RuntimeError(f'Refusing to overwrite existing archive: {target}')
 original=hashlib.sha256()
 with p.open('rb') as source,temporary.open('wb') as raw,gzip.GzipFile(filename='',mode='wb',compresslevel=1,fileobj=raw,mtime=0) as destination:
  for chunk in iter(lambda:source.read(1024*1024),b''):original.update(chunk);destination.write(chunk)
 restored=hashlib.sha256()
 with gzip.open(temporary,'rb') as source:
  for chunk in iter(lambda:source.read(1024*1024),b''):restored.update(chunk)
 if restored.digest()!=original.digest():raise RuntimeError(f'Archive readback differs: {p}')
 temporary.replace(target)
 rows.append(dict(file=str(p.relative_to(root)),sha256=original.hexdigest(),rawBytes=p.stat().st_size,gzipBytes=target.stat().st_size,gzipSha256=hashlib.sha256(target.read_bytes()).hexdigest()))
 p.unlink()
(root/'compression-manifest.json').write_text(json.dumps(rows,indent=2)+'\n')
print(f'ARCHIVE_COMPRESSION_PASS files={len(rows)} rawBytes={sum(r["rawBytes"] for r in rows)} gzipBytes={sum(r["gzipBytes"] for r in rows)} exact-readback')
