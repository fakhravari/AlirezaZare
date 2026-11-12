const fs = require('fs');
for (const n of ['zarinpal','idpay','vandar','payping','zibal','nextpay','shaparak']) {
 const s=fs.readFileSync(`.tmp-payment-icons/${n}.svg`,'utf8');
 console.log(n,[...new Set([...s.matchAll(/<\/?([A-Za-z][\w:-]*)/g)].map(x=>x[1]))].join(','),[...new Set([...s.matchAll(/\s([A-Za-z][\w:-]*)=/g)].map(x=>x[1]))].join(','));
}
