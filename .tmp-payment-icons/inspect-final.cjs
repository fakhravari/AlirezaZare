const fs=require('fs'), path=require('path');
for(const f of fs.readdirSync(__dirname).filter(x=>x.endsWith('.svg')&&x!=='azki-vam.svg'&&x!=='azki-vam-favicon.svg')) { const s=fs.readFileSync(path.join(__dirname,f),'utf8'); console.log(f,[...new Set([...s.matchAll(/<\/?([A-Za-z][\w:-]*)/g)].map(x=>x[1]))].join(','),[...new Set([...s.matchAll(/\s([A-Za-z][\w:-]*)=/g)].map(x=>x[1]))].join(',')); }
