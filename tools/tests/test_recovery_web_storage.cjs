// Run: node tools/tests/test_recovery_web_storage.cjs
// Exercise the actual .jslib bridge with fresh JS heaps and origin storage.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../Assets/Online/SharedReturnStorage.jslib'), 'utf8');
const prefix = 'DeadBoat.Recovery.origin.v1:';
function client(storage, directory) {
  const library = {}, memory = new Map();
  let pointer = 1;
  const context = {
    LibraryManager: { library }, mergeInto: Object.assign,
    localStorage: storage, location: { pathname: directory }, console: { warn() {} },
    UTF8ToString: value => value,
    lengthBytesUTF8: value => Buffer.byteLength(value, 'utf8'),
    _malloc: () => pointer++, stringToUTF8: (value, ptr) => memory.set(ptr, value)
  };
  vm.runInNewContext(source, context);
  return {
    write: (key, value) => library.DB_ReturnStorage_Set(key, value),
    read: key => { const ptr = library.DB_ReturnStorage_Get(key); return ptr === 0 ? null : memory.get(ptr); }
  };
}
function origin() {
  const records = new Map();
  return { records, getItem: key => records.has(key) ? records.get(key) : null,
    setItem: (key, value) => records.set(key, value) };
}
const storage = origin();
const ticket = 'DeadBoat.SharedReturn.v1', identity = 'DeadBoat.FusionIdentity.v1';
const before = client(storage, '/424778/old-build/index.html');
assert.equal(before.read(ticket), null); // permits legacy migration, unlike a deleted ticket
assert.equal(before.write(identity, '-9135724296312345678'), 1);
const saved = JSON.stringify({ Room: 'test-only', PlayerId: '-9135724296312345678', Seed: 47, Label: 'Экипаж 🛶' });
assert.equal(before.write(ticket, saved), 1);
// New page/heap, different build URL, same origin: immediate committed metadata.
const after = client(storage, '/424778/new-build/index.html');
assert.equal(after.read(identity), '-9135724296312345678');
assert.equal(after.read(ticket), saved);
assert.equal(storage.records.get(prefix + ticket), saved);
// An explicit deletion marker must block migration of stale URL-local PlayerPrefs.
assert.equal(after.write(ticket, ''), 1);
const restart = client(storage, '/424778/old-build/index.html');
assert.equal(restart.read(ticket), '');
assert.notEqual(restart.read(ticket), null);
assert.equal(restart.read(identity), '-9135724296312345678');
assert.equal(client(origin(), '/424778/new-build/index.html').read(ticket), null);
const unavailable = client({ getItem() { throw Error('blocked'); }, setItem() { throw Error('quota'); } }, '/test');
assert.equal(unavailable.read(ticket), null);
assert.equal(unavailable.write(ticket, saved), 0);
assert.equal(unavailable.write(ticket, ''), 0);
console.log('PASS: missing vs tombstone, immediate commit, fresh heap/different build URL, 64-bit identity, Unicode ticket, origin isolation, denied/quota errors');
