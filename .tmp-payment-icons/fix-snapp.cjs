const fs = require('fs');
const file = '.tmp-payment-icons/snapp-pay.svg';
let svg = fs.readFileSync(file, 'utf8');
svg = svg.replace('<svg xmlns="http://www.w3.org/2000/svg"', '<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink"');
fs.writeFileSync(file, svg);
if (!svg.includes('xmlns:xlink="http://www.w3.org/1999/xlink"')) throw new Error('xlink namespace missing');
