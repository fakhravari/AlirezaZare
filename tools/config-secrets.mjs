const secretKey = /(?:password|passwd|pwd|secret|token|api[-_]?key|connection[-_]?strings?|private[-_]?key|client[-_]?secret|access[-_]?key|signing[-_]?key|credential)/i;
const placeholder = /^(?:\$\{[^}]+\}|CHANGE_ME|REPLACE_ME|YOUR_[A-Z0-9_]+)$/i;

export function findSecrets(value, prefix = '', inheritedSecret = false) {
  const findings = [];
  if (!value || typeof value !== 'object') return findings;

  for (const [key, child] of Object.entries(value)) {
    const field = prefix ? `${prefix}:${key}` : key;
    const isSecretField = inheritedSecret || secretKey.test(key);
    if (isSecretField && typeof child === 'string' && child.trim() && !placeholder.test(child.trim())) {
      findings.push(field);
    } else if (child && typeof child === 'object') {
      findings.push(...findSecrets(child, field, isSecretField));
    }
  }
  return findings;
}
