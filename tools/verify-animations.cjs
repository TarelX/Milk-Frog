const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const project = path.resolve(__dirname, '..');
const assetRoot = path.join(project, 'assets/generated/young/animations');
const catalog = JSON.parse(fs.readFileSync(path.join(assetRoot, 'animations.json'), 'utf8'));
const files = new Set();
for (const [name, clip] of Object.entries(catalog.clips)) {
  assert(clip.frames.length > 1, name + ' has no frame sequence');
  assert.equal(clip.durationMs, clip.frames.reduce((sum, frame) => sum + frame.ms, 0), name);
  assert(!clip.loopStart || clip.loopStart < clip.frames.length, name + ' loop start');
  for (const frame of clip.frames) {
    assert(frame.ms > 0, name + ' duration');
    files.add(frame.file);
    const png = fs.readFileSync(path.join(assetRoot, frame.file));
    assert.equal(png.subarray(0, 8).toString('hex'), '89504e470d0a1a0a', frame.file);
    assert.equal(png.readUInt32BE(16), 512, frame.file);
    assert.equal(png.readUInt32BE(20), 512, frame.file);
    assert.equal(png[25], 6, frame.file + ' must contain alpha');
  }
}
assert.equal(files.size, catalog.uniqueFrames, 'catalog must cover every active frame');
const context = {
  clearRect() {}, save() {}, restore() {}, drawImage() {}, translate() {}, scale() {},
};
const canvas = () => ({ width: 512, height: 512, getContext: () => ({...context}) });
let rafId = 0;
const sandbox = {
  window: {},
  document: { hidden: false, addEventListener() {}, removeEventListener() {}, createElement: canvas },
  requestAnimationFrame() { return ++rafId; }, cancelAnimationFrame() {},
  Image: class { set src(file) { queueMicrotask(() => fs.existsSync(file) ? this.onload() : this.onerror()); } },
};
vm.runInNewContext(fs.readFileSync(path.join(project, 'design/ui/frame-player.js'), 'utf8'), sandbox);
const FramePlayer = sandbox.window.MilkFrogFramePlayer;
async function run() {
  const player = new FramePlayer(canvas(), {base: assetRoot + path.sep, catalog, onError: error => {throw error;}});
  let finished = 0;
  await player.play('wave', {onComplete: () => {finished++;}});
  for (let t = 0; t < 4000; t += 80) player.tick(t);
  assert.equal(finished, 1, 'one-shot must finish exactly once');
  await player.play('sleep');
  for (let t = 5000; t < 16000; t += 80) player.tick(t);
  assert.equal(player.current.finished, false);
  const prefix = catalog.clips.sleep.frames.slice(0, 6).reduce((s, f) => s + f.ms, 0);
  assert(player.current.elapsed >= prefix, 'sleep loop must not repeat sitting down');
  await player.play('walk', {loop:false,onComplete:()=>{finished++;}});
  for (let t = 16000; t < 19000; t += 80) player.tick(t);
  assert.equal(finished, 2, 'preview once must override loop');
  const old = player.play('drink');
  const latest = player.play('bath');
  await Promise.all([old, latest]);
  assert.equal(player.current.clip.name, 'bath', 'latest action wins while assets load');
  player.settings({reduced:true,speed:.75});
  player.paint();
  assert.equal(player.speed, .75);
  assert.equal(player.reduced, true);
  player.destroy();
  console.log(JSON.stringify({clips:Object.keys(catalog.clips).length,uniqueFrames:files.size,canvas:'512x512 RGBA',checks:'asset references, timing, one-shot, intro loop, override, async cancellation, reduced motion: passed'}));
}
run().catch(error => {console.error(error);process.exitCode=1;});
