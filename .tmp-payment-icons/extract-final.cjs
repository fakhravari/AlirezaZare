const fs = require('fs');
const path = require('path');
const m = require(path.join(__dirname, 'package/dist/cjs/index.js'));
const map = { zarinpal: 'ZarrinpalColorIcon', idpay: 'IdpayColorIcon', vandar: 'VandarColorIcon', payping: 'PaypingColorIcon', zibal: 'ZibalColorIcon', nextpay: 'NextPayColorIcon', shaparak: 'ShaparakColorIcon', digipay: 'DigipayColorIcon', 'snapp-pay': 'SnappPayColorIcon' };
for (const [code, key] of Object.entries(map)) {
  const icon = m[key]; if (!icon) throw new Error(key);
  fs.writeFileSync(path.join(__dirname, `${code}.svg`), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${icon.viewBox}">${icon.content}</svg>`);
}
const fsText = fs.readFileSync(path.join(__dirname,'azki-vam-favicon.svg'),'utf8');
const png = fsText.match(/<image[^>]+href="data:image\/png;base64,([^"]+)/);
if (!png) throw new Error('AzkiVam favicon PNG payload not found');
fs.writeFileSync(path.join(__dirname,'azki-vam.png'), Buffer.from(png[1], 'base64'));
