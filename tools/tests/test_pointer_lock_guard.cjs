const assert = require('node:assert/strict');
const { install } = require('../online-webgl/pointer-lock-guard.js');
const tick = () => new Promise(resolve => setImmediate(resolve));
(async () => {
  const unhandled = [], warnings = [];
  const listener = error => unhandled.push(error);
  process.on('unhandledRejection', listener);
  const logger = { warn: text => warnings.push(text) };
  let reject, calls = 0, context, argumentsSeen;
  const first = new Promise((_, fail) => { reject = fail; });
  const canvas = { requestPointerLock(options) {
    calls++; context = this; argumentsSeen = options;
    return calls === 1 ? first : Promise.resolve();
  } };
  assert.equal(install(canvas, logger), true);
  const wrapped = canvas.requestPointerLock;
  assert.equal(install(canvas, logger), true);
  assert.equal(canvas.requestPointerLock, wrapped);
  const options = { unadjustedMovement: false };
  assert.equal(canvas.requestPointerLock(options), first);
  assert.equal(canvas.requestPointerLock(), first);
  assert.equal(calls, 1); // coalesces in-flight capture, preserves native Promise
  assert.equal(context, canvas); assert.equal(argumentsSeen, options);
  reject(Object.assign(Error('browser refusal'), { name: 'UnknownError' }));
  await tick();
  assert.equal(unhandled.length, 0); assert.equal(warnings.length, 1);
  await canvas.requestPointerLock(); assert.equal(calls, 2);
  const sync = { requestPointerLock() { throw Object.assign(Error('denied'), { name: 'NotAllowedError' }); } };
  install(sync, logger); assert.doesNotThrow(() => sync.requestPointerLock());
  let legacyCalls = 0;
  const legacy = { requestPointerLock() { legacyCalls++; } };
  install(legacy, logger); assert.equal(legacy.requestPointerLock(), undefined); assert.equal(legacyCalls, 1);
  const unsupported = {}; assert.equal(install(unsupported, logger), false);
  const unexpected = Error('unrelated implementation bug');
  const broken = { requestPointerLock() { return Promise.reject(unexpected); } };
  install(broken, logger); broken.requestPointerLock(); await tick();
  assert.deepEqual(unhandled, [unexpected]); // no blanket error suppression
  process.off('unhandledRejection', listener);
  console.log('PASS: native Promise rejection handled, coalescing, retry, API context/options, sync denial, legacy void, idempotence, unexpected errors preserved');
})().catch(error => { console.error(error); process.exitCode = 1; });
