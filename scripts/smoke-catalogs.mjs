// Read-only check using the real TypeScript HTTP client (Node.js 24).
import assert from 'node:assert/strict';
import { request, all } from '../frontend/Import-pdf-excel-master/src/services/api.ts';

if (!process.env.SMOKE_EMAIL || !process.env.SMOKE_PASSWORD) {
  throw new Error('Définir SMOKE_EMAIL et SMOKE_PASSWORD pour un compte existant.');
}
const values = new Map();
globalThis.window = new EventTarget();
globalThis.sessionStorage = {
  getItem: key => values.get(key) ?? null,
  setItem: (key, value) => values.set(key, value),
};
const session = await request('/auth/login', {
  method: 'POST',
  body: JSON.stringify({ email: process.env.SMOKE_EMAIL, password: process.env.SMOKE_PASSWORD }),
});
sessionStorage.setItem('bank-converter-session', JSON.stringify(session));
const [clients, banks, accounts, dashboard] = await Promise.all([
  all('/clients'), all('/banks'), all('/bank-accounts'), request('/dashboard'),
]);
assert.equal(dashboard.clients, clients.length);
assert.equal(dashboard.banks, banks.length);
assert.equal(dashboard.accounts, accounts.length);
assert.deepEqual(Object.keys(dashboard).sort(), ['accounts', 'banks', 'clients']);
for (const bank of banks) {
  const related = await request(`/banks/${bank.id}/accounts?pageSize=1`);
  assert.equal(related.total, bank.accounts);
  await request(`/banks/${bank.id}/logo`);
}
for (const client of clients) await request(`/clients/${client.id}/photo`);
console.log(`API vérifiée : connexion, ${clients.length} clients, ${banks.length} banques, ${accounts.length} comptes, images et tableau de bord.`);
