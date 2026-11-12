const fs = require('fs');
const path = require('path');
const m = require(path.join(__dirname, 'package/dist/cjs/index.js'));
const map = { zarinpal: 'ZarrinpalColorIcon', idpay: 'IdpayColorIcon', vandar: 'VandarColorIcon', payping: 'PaypingColorIcon', zibal: 'ZibalColorIcon', nextpay: 'NextPayColorIcon', shaparak: 'ShaparakColorIcon' };
for (const [code, key] of Object.entries(map)) {
  const icon = m[key]; if (!icon) throw new Error(key);
  fs.writeFileSync(path.join(__dirname, `${code}.svg`), `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${icon.viewBox}">${icon.content}</svg>`);
}
