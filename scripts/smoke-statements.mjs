// Authenticated read-only check; requires an existing Admin account.
import assert from 'node:assert/strict';
import { request } from '../frontend/Import-pdf-excel-master/src/services/api.ts';

assert.ok(process.env.SMOKE_EMAIL && process.env.SMOKE_PASSWORD,
  'Définir SMOKE_EMAIL et SMOKE_PASSWORD pour un administrateur existant.');
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
assert.equal(session.user.role, 'Admin');
sessionStorage.setItem('bank-converter-session', JSON.stringify(session));
const [statements, archives, templates, profiles, fields] = await Promise.all([
  request('/statements?page=1'), request('/archives?page=1'),
  request('/export-templates'), request('/statement-profiles'), request('/export-templates/fields'),
]);
assert.equal(statements.pageSize, 20);
assert.ok(archives.items.every(s => s.archivedAt !== null));
assert.ok(Array.isArray(templates) && Array.isArray(profiles));
assert.ok(fields.sources.includes('Debit') && fields.sources.includes('Credit'));
if (statements.items.length) {
  const detail = await request('/statements/' + statements.items[0].id);
  assert.ok(Array.isArray(detail.transactions) && Array.isArray(detail.exports));
  assert.ok(Array.isArray(detail.check.issues) && Array.isArray(detail.history));
  assert.equal('storageKey' in detail, false);
}
console.log(`API documentaire vérifiée : ${statements.total} relevés, ${archives.total} archives, ${templates.length} modèles, ${profiles.length} profils. Aucune donnée modifiée.`);
