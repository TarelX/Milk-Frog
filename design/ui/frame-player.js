/* Frame playback uses generated PNG drawings. CSS movement is optional accent only. */
(() => {
  'use strict';
  class MilkFrogFramePlayer {
    constructor(canvas, { base, catalog, onError } = {}) {
      this.canvas = canvas;
      this.ctx = canvas.getContext('2d');
      this.base = base;
      this.catalog = catalog;
      this.onError = onError || (() => {});
      this.images = new Map();
      this.clips = new Map();
      this.speed = 1;
      this.reduced = false;
      this.requestId = 0;
      this.current = null;
      this.raf = 0;
      this.stopped = false;
      this.last = 0;
      this.tick = this.tick.bind(this);
      this.visibility = () => {
        this.last = 0;
        if (document.hidden) { cancelAnimationFrame(this.raf); this.raf = 0; }
        else this.schedule();
      };
      document.addEventListener('visibilitychange', this.visibility);
    }
    resolve(name) { return this.catalog.aliases[name] || name; }
    has(name) { return !!this.catalog.clips[this.resolve(name)]; }
    loadImage(file) {
      if (!this.images.has(file)) {
        const promise = new Promise((resolve, reject) => {
          const img = new Image();
          img.onload = () => resolve(img);
          img.onerror = () => reject(new Error('无法加载动作帧：' + file));
          img.src = this.base + file;
        });
        this.images.set(file, promise);
      }
      return this.images.get(file);
    }
    async prepare(name) {
      name = this.resolve(name);
      if (!this.clips.has(name)) {
        const clip = this.catalog.clips[name];
        if (!clip) throw new Error('未知动作：' + name);
        const promise = Promise.all(clip.frames.map(async frame => ({ ...frame, image: await this.loadImage(frame.file) })))
          .then(frames => ({ ...clip, name, frames }));
        this.clips.set(name, promise);
      }
      return this.clips.get(name);
    }
    warm(names) { return Promise.allSettled(names.map(name => this.prepare(name))); }
    async play(name, options = {}) {
      const request = ++this.requestId;
      let clip;
      try { clip = await this.prepare(name); }
      catch (error) { if (request === this.requestId) this.onError(error); return false; }
      if (request !== this.requestId || this.stopped) return false;
      let previous = null;
      if (this.current) {
        previous = document.createElement('canvas');
        previous.width = this.canvas.width; previous.height = this.canvas.height;
        previous.getContext('2d').drawImage(this.canvas, 0, 0);
      }
      this.current = {
        clip, elapsed: 0, finished: false, options,
        previous, transition: 0, travel: options.travel || false,
      };
      this.last = 0;
      this.paint();
      this.schedule();
      return true;
    }
    settings({ reduced = false, speed = 1 } = {}) {
      this.reduced = reduced;
      this.speed = Math.max(.5, Math.min(1.5, Number(speed) || 1));
      this.last = 0;
      this.schedule();
    }
    schedule() {
      if (!this.raf && !this.stopped && !document.hidden) this.raf = requestAnimationFrame(this.tick);
    }
    tick(now) {
      this.raf = 0;
      const active = this.current;
      if (!active) return;
      const delta = this.last ? Math.min(100, now - this.last) : 0;
      this.last = now;
      active.elapsed += delta * this.speed;
      active.transition += delta;
      const clip = active.clip;
      const duration = clip.durationMs;
      const looping = active.options.loop === undefined ? !!clip.loop : active.options.loop;
      if (active.elapsed >= duration && !active.finished) {
        if (looping) {
          const loopStart = clip.loopStart || 0;
          const prefix = clip.frames.slice(0, loopStart).reduce((sum, frame) => sum + frame.ms, 0);
          const cycle = duration - prefix;
          active.elapsed = prefix + (active.elapsed - duration) % cycle;
        } else {
          active.elapsed = duration;
          active.finished = true;
        }
      }
      this.paint();
      if (active.finished && !active.notified) {
        active.notified = true;
        if (active.options.onComplete) active.options.onComplete();
      }
      // Activity loops keep running; one-shot clips stop on their final drawing.
      if (this.current !== active || !active.finished) this.schedule();
    }
    sample(active) {
      const frames = active.clip.frames;
      if (this.reduced) {
        const i = active.clip.name === 'sleep' ? frames.length - 1 : Math.floor(frames.length / 2);
        return { frame: frames[i], next: frames[i], blend: 0 };
      }
      let t = active.elapsed;
      for (let i = 0; i < frames.length; i++) {
        if (t < frames[i].ms || i === frames.length - 1) {
          const next = i + 1 < frames.length ? frames[i + 1] : (active.clip.loop ? frames[active.clip.loopStart || 0] : frames[i]);
          // Brief blending softens the cut without covering the drawing in ghost images.
          const blendTime = Math.min(45, frames[i].ms * .35);
          const blend = Math.max(0, Math.min(1, (t - frames[i].ms + blendTime) / blendTime));
          return { frame: frames[i], next, blend: blend * blend * (3 - 2 * blend) };
        }
        t -= frames[i].ms;
      }
    }
    paint() {
      const active = this.current;
      if (!active) return;
      const { frame, next, blend } = this.sample(active);
      const mix = next.image !== frame.image ? blend : 0;
      const ctx = this.ctx, w = this.canvas.width, h = this.canvas.height;
      ctx.clearRect(0, 0, w, h);
      ctx.save();
      ctx.imageSmoothingEnabled = true;
      ctx.imageSmoothingQuality = 'high';
      if (!this.reduced && ['idle', 'sleep', 'sick'].includes(active.clip.name)) {
        const breath = Math.sin(active.elapsed / (active.clip.name === 'sleep' ? 850 : 700)) * .004;
        ctx.translate(w / 2, 462); ctx.scale(1 + breath * .4, 1 + breath); ctx.translate(-w / 2, -462);
      }
      ctx.globalAlpha = 1 - mix;
      ctx.drawImage(frame.image, 0, 0, w, h);
      if (mix > 0) {
        ctx.globalCompositeOperation = 'lighter';
        ctx.globalAlpha = mix;
        ctx.drawImage(next.image, 0, 0, w, h);
      }
      ctx.restore();
      if (active.previous && active.transition < 150 && !this.reduced) {
        ctx.globalAlpha = Math.max(0, 1 - active.transition / 150);
        ctx.drawImage(active.previous, 0, 0, w, h);
        ctx.globalAlpha = 1;
      } else active.previous = null;
    }
    cancel() { this.requestId++; }
    destroy() {
      this.stopped = true; this.requestId++; cancelAnimationFrame(this.raf);
      document.removeEventListener('visibilitychange', this.visibility);
    }
  }
  window.MilkFrogFramePlayer = MilkFrogFramePlayer;
})();
