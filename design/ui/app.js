(() => {
  'use strict';
  const root = document.getElementById('app');
  const art = '../../assets/generated/';
  const paths = {
    leaf: '<path d="M20 4C8 2 3 9 5 15c7 5 15 1 15-11Z"/><path d="m4 21 10-11"/>',
    home: '<path d="m3 10 9-7 9 7v10H4V10"/><path d="M9 20v-7h6v7"/>',
    calendar: '<rect x="4" y="5" width="16" height="16" rx="3"/><path d="M8 3v4m8-4v4M4 10h16m-11 5h2m3 0h1"/>',
    shop: '<path d="m4 9 2-5h12l2 5M4 9v11h16V9M3 9c0 4 6 4 6 0 0 4 6 4 6 0 0 4 6 4 6 0M9 20v-6h6v6"/>',
    bag: '<rect x="5" y="7" width="14" height="14" rx="4"/><path d="M9 7V5a3 3 0 0 1 6 0v2m-6 7h6m-6 3h6"/>',
    growth: '<path d="M12 21V10M12 16C4 16 3 11 4 8c5-1 8 2 8 6m0-3c0-6 4-8 8-7 1 5-3 8-8 8"/>',
    settings: '<path d="m10 3-1 3-3 1-3 3 2 2-1 4 3 1 2 4h4l1-3 4-1 2-4-2-2V7l-4-1-1-3Z"/><circle cx="11" cy="12" r="3"/>',
    coin: '<ellipse cx="12" cy="12" rx="8" ry="9"/><path d="M14 8h-3a2 2 0 0 0 0 4h2a2 2 0 0 1 0 4h-3m2-10v2m0 8v2"/>',
    milk: '<path d="M5 8h12v10a3 3 0 0 1-3 3H8a3 3 0 0 1-3-3V8Zm12 1h2a3 3 0 0 1 0 6h-2M8 3v2m4-3v3"/>',
    bubble: '<circle cx="9" cy="14" r="6"/><circle cx="17" cy="6" r="3"/><circle cx="20" cy="17" r="2"/><path d="M6 13a3 3 0 0 1 3-3"/>',
    heart: '<path d="M20 5c-4-4-8 1-8 1S8 1 4 5c-5 5 8 15 8 15S25 10 20 5Z"/>',
    health: '<path d="M9 3h6v6h6v6h-6v6H9v-6H3V9h6Z"/>',
    energy: '<path d="m13 2-8 12h6l-1 8 9-13h-7Z"/>',
    moon: '<path d="M20 15A9 9 0 0 1 9 3a9 9 0 1 0 11 12Z"/>',
    book: '<path d="M12 5C8 2 4 3 3 4v15c3-2 6-1 9 1 3-2 6-3 9-1V4c-3-2-6-1-9 1Zm0 0v15"/>',
    work: '<rect x="3" y="7" width="18" height="14" rx="3"/><path d="M8 7V4h8v3M3 12c6 3 12 3 18 0m-10 1h2v3h-2Z"/>',
    clock: '<circle cx="12" cy="12" r="9"/><path d="M12 6v6l4 2"/>',
    arrow: '<path d="m9 5 7 7-7 7"/>',
    lock: '<rect x="6" y="10" width="12" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3m-4 5v2"/>',
    ball: '<circle cx="12" cy="12" r="9"/><path d="M6 5c7 2 9 7 10 15M3 13c7-4 13-4 18-1"/>',
    star: '<path d="m12 3 3 6 7 1-5 5 1 7-6-3-6 3 1-7-5-5 7-1Z"/>',
    check: '<path d="m5 12 4 4L19 6"/>',
    desktop: '<rect x="3" y="3" width="18" height="13" rx="2"/><path d="M12 16v5m-5 0h10"/>',
    sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M2 12h2m16 0h2M5 5l1 1m12 12 1 1M5 19l1-1M18 6l1-1"/>',
  };
  const icon = name => `<svg class="icon" viewBox="0 0 24 24" aria-hidden="true">${paths[name] || paths.leaf}</svg>`;
  const items = [
    { id: 'milk', name: '小杯牛奶', file: 'milk-cup.png', type: 'food', price: 10, effect: '饱食 +30 · 心情 +5', stats: { hunger: 30, mood: 5 }, pose: 'eat', line: '咕嘟咕嘟，是喜欢的牛奶！' },
    { id: 'pudding', name: '奶香布丁', file: 'milk-pudding.png', type: 'food', price: 18, effect: '饱食 +20 · 心情 +15', stats: { hunger: 20, mood: 15 }, pose: 'pudding', line: '甜甜的，今天也很幸福。' },
    { id: 'dumplings', name: '青草团子', file: 'grass-dumplings.png', type: 'food', price: 14, effect: '饱食 +25 · 健康 +3', stats: { hunger: 25, health: 3 }, pose: 'snack', line: '一口一个小团子。' },
    { id: 'bubble', name: '小泡泡', file: 'bubble-wand.png', type: 'cleaning', price: 8, effect: '清洁 +25', stats: { cleanliness: 25 }, pose: 'bath-happy', line: '泡泡飞呀飞，变干净啦！' },
    { id: 'wash', name: '奶香浴液', file: 'bath-wash.png', type: 'cleaning', price: 15, effect: '清洁 +45 · 心情 +5', stats: { cleanliness: 45, mood: 5 }, pose: 'bath-happy', line: '现在是一只香香的奶蛙。' },
    { id: 'ball', name: '薄荷小球', file: 'toy-ball.png', type: 'toy', price: 30, effect: '心情 +15 · 精力 −5', stats: { mood: 15, energy: -5 }, pose: 'play', line: '接住了！再来一次～', permanent: true },
  ];
  const courses = [
    { id: 'count', name: '数数课', icon: 'book', time: 10, reward: '计算 +1', skill: '计算', description: '从一颗奶豆，数到一整罐的快乐。', kind: 'study' },
    { id: 'manners', name: '礼貌课', icon: 'heart', time: 10, reward: '社交 +1', skill: '社交', description: '把每一句「你好」都说得暖暖的。', kind: 'study' },
    { id: 'welcome', name: '奶站迎宾', icon: 'work', time: 5, reward: '8 奶豆', coins: 8, description: '在小小奶站，迎接今天的第一位客人。', kind: 'work' },
    { id: 'cloud', name: '云朵擦拭员', icon: 'bubble', time: 10, reward: '18 奶豆', coins: 18, skillNeed: '社交', need: 2, description: '把天空的云朵擦得蓬松又干净。', kind: 'work' },
    { id: 'stamps', name: '邮票整理员', icon: 'bag', time: 15, reward: '30 奶豆', coins: 30, skillNeed: '计算', need: 3, description: '整理远方寄来的信，也收集一点想念。', kind: 'work' },
    { id: 'focus', name: '一起专注', icon: 'clock', time: 25, reward: '专注 +2 · 经验 +5', skill: '专注', description: '你认真工作，它安安静静地陪着。', kind: 'study', level: 7 },
  ];
  const initial = () => ({ page: 'home', panel: null, mode: 'desktop', category: 'all', coins: 120, exp: 132, stats: { hunger: 48, cleanliness: 66, mood: 82, health: 100, energy: 61 }, bag: { milk: 2, pudding: 1, bubble: 2 }, pose: 'idle', speech: '今天也要一起，慢慢长大呀。', activity: null, skills: { '计算': 2, '社交': 2, '专注': 0 }, diary: ['它认真记住了数字「三」。', '今天的第一杯牛奶，很香。'], settings: { reduced: false, bubbles: true, size: 210 } });
  let state = initial();
  let poseTimeout, toastTimeout, menuOpen=false, pointerDragging=false;
  let speechUntil=Date.now()+6500;
  let framePlayer=null,framePose=null,motionEpoch=0,appliedMotionEpoch=-1,afterMotion=null;
  const reducedMotion=()=>state.settings.reduced||window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  // The frame player loads only the action that is needed, plus a small warm-up set.
  function levelInfo() {
    let level = 1, base = 0;
    while (level < 10 && state.exp >= base + 20 + level * 10) { base += 20 + level * 10; level++; }
    return { level, current: state.exp - base, need: 20 + level * 10, total: state.exp };
  }
  function wallet() { return `<div class="wallet">${icon('coin')}<b>${state.coins}</b> 奶豆</div>`; }
  function heading(eyebrow, title, subtitle) { return `<header class="page-heading"><div><p class="eyebrow">${eyebrow}</p><h1>${title}</h1><p class="subheading">${subtitle}</p></div>${wallet()}</header>`; }
  function quickAction(action, symbol, label) { return `<button type="button" class="quick-action" data-action="${action}">${icon(symbol)}${label}</button>`; }
  function nav() {
    const entries = [['home','home','奶蛙小屋'],['schedule','calendar','学习打工'],['shop','shop','小小商店'],['bag','bag','我的背包'],['growth','growth','成长手记']];
    return entries.map(([page,symbol,label]) => `<button type="button" class="nav-button" data-page="${page}" aria-label="${label}" ${state.page===page?'aria-current="page"':''}>${icon(symbol)}<span>${label}</span></button>`).join('');
  }
  function statusRows() {
    return [['hunger','milk','饱食'],['cleanliness','bubble','清洁'],['mood','heart','心情'],['health','health','健康'],['energy','energy','精力']].map(([key,symbol,label]) => `<div class="stat-line ${state.stats[key]<55?'attention':''}">${icon(symbol)}<div><div class="stat-header"><span>${label}</span><span>${state.stats[key]} / 100</span></div><div class="meter" role="meter" aria-label="${label}" aria-valuenow="${state.stats[key]}" aria-valuemin="0" aria-valuemax="100"><span style="width:${state.stats[key]}%"></span></div></div></div>`).join('');
  }
  function home() {
    const lv = levelInfo();
    return heading('A LITTLE LIFE, TOGETHER', '今天的奶蛙，很想你', '陪伴第 4 天 · 幼年期 · 每一天都有一点小成长') + `
      <div class="home-grid"><div><section class="pet-room" aria-label="奶蛙活动区"><div class="room-sun"></div><div class="room-plant"></div><div class="room-header"><span class="stage-label">${state.activity?state.activity.name:'正在享受慢悠悠的时光'}</span><button type="button" class="text-button" data-action="desktop">回到桌面 ${icon('arrow')}</button></div><div class="speech">${state.speech}</div><div class="pet-shadow"></div><button type="button" class="pet-button" data-action="pet" aria-label="摸摸奶蛙"><img class="pet-image" src="${art}young/${state.pose}.png" alt="幼年奶蛙"></button><span class="pet-caption">轻轻点一下，回应它的小心情</span></section><div class="quick-actions">${quickAction('feed','milk','喂点好吃的')}${quickAction('bath','bubble','洗个泡泡澡')}${quickAction('play','ball','一起玩一会')}${quickAction('sleep','moon',state.activity?.kind==='sleep'?'叫它起床':'睡个小觉')}</div></div>
      <div class="home-right"><section class="status-card"><div class="section-head"><h3>它的小状态</h3><span class="stage-label">${state.stats.mood>=70?'心情不错':'想要陪伴'}</span></div><div class="status-list">${statusRows()}</div><div class="status-hint">${icon('milk')}<span>${state.stats.hunger<55?'肚子有一点空，来杯牛奶吧。':'圆圆的小肚子，暖暖的小心情。'}</span></div></section><section class="growth-mini"><div class="section-head"><strong>Lv.${lv.level} · 还在慢慢长大</strong><small>${lv.current} / ${lv.need}</small></div><div class="meter"><span style="width:${Math.min(100,lv.current/lv.need*100)}%"></span></div><p>Lv.5 解锁桌面蹦跳漫游 <button type="button" class="text-button" data-page="growth">看看成长 ${icon('arrow')}</button></p></section></div></div>
      <div class="home-bottom"><section class="soft-section"><div class="section-head"><h3>给今天一点安排</h3><button class="text-button" type="button" data-page="schedule">全部安排 ${icon('arrow')}</button></div>${state.activity?activeActivity():`<div class="activity-row"><span class="activity-icon">${icon('book')}</span><div class="activity-details"><strong>上一堂数数课</strong><p>10 分钟 · 计算 +1</p></div><button type="button" class="text-button" data-start="count">去学习 ${icon('arrow')}</button></div><div class="activity-row"><span class="activity-icon">${icon('work')}</span><div class="activity-details"><strong>奶站的小小迎宾员</strong><p>5 分钟 · 赚取 8 奶豆</p></div><button type="button" class="text-button" data-start="welcome">去打工 ${icon('arrow')}</button></div>`}</section><section class="soft-section"><div class="section-head"><h3>最近的小小回忆</h3><span class="eyebrow" style="margin:0">OUR DAYS</span></div><div class="diary"><p>${state.diary[0]}</p><small>今天 · 一起成长的第 4 天</small></div><div class="diary" style="margin-top:16px"><p>${state.diary[1]||'又多了一件值得记住的小事。'}</p><small>今天 · 好好生活的小证据</small></div></section></div>`;
  }
  function tabs() {
    return `<div class="filter-tabs" aria-label="物品分类">${[['all','全部'],['food','食物'],['cleaning','清洁'],['toy','玩具']].map(([id,label])=>`<button type="button" class="filter-tab" data-category="${id}" aria-pressed="${state.category===id}">${label}</button>`).join('')}<span class="shop-info">小小的东西，装着满满的喜欢</span></div>`;
  }
  function shopOrBag(bag) {
    const filtered = items.filter(i=>(state.category==='all'||state.category===i.type)&&(!bag||(state.bag[i.id]||0)>0));
    return heading(bag?'LITTLE THINGS, BIG HAPPINESS':'SOMETHING SWEET FOR TODAY',bag?'奶蛙的百宝袋':'小小商店，刚好喜欢',bag?'把喜欢的东西收好，想用的时候就拿出来。':'一杯牛奶，一点泡泡，给它一个好心情。')+tabs()+`<div class="item-grid">${filtered.map(i=>`<article class="item-card"><div class="item-art"><img src="${art}items/${i.file}" alt="${i.name}"><span class="item-badge">${bag?`拥有 ${state.bag[i.id]} 件`:i.permanent?'永久玩具':'日常好物'}</span></div><h3 class="item-name">${i.name}</h3><p class="item-effect">${i.effect}</p><div class="item-bottom"><span class="price">${bag?(i.permanent?'可以一直玩':'用一份，暖一下'):icon('coin')+i.price+' 奶豆'}</span><button type="button" class="buy-button" ${bag?`data-use="${i.id}"`:`data-buy="${i.id}"`} ${!bag&&i.permanent&&state.bag[i.id]?'disabled':''}>${bag?(i.permanent?'一起玩':'使用'):(i.permanent&&state.bag[i.id]?'已拥有':'买一份')}</button></div></article>`).join('')||`<div class="empty-bag"><h3>这里还空着呢</h3><p>去商店挑一点奶蛙喜欢的小东西吧。</p><button type="button" class="primary-button" data-page="shop">逛逛小商店 ${icon('arrow')}</button></div>`}</div><div class="tip-strip">${icon('leaf')}<span>${bag?'物品使用后，奶蛙的动作和小状态会一起变化。':'购买后会放入背包。薄荷小球是永久玩具，只需购买一次。'}</span></div>`;
  }
  function remaining(activity) { const seconds=Math.max(0,Math.ceil((activity.endsAt-Date.now())/1000)); return `${String(Math.floor(seconds/60)).padStart(2,'0')}:${String(seconds%60).padStart(2,'0')}`; }
  function activeActivity() {
    const a=state.activity;
    if(!a)return '';
    return `<div class="activity-banner"><img src="${art}young/${a.kind==='sleep'?'sleep':a.kind}.png" alt="${a.name}"><div class="activity-banner-content"><strong>${a.name}</strong><p>还剩 <span data-countdown>${remaining(a)}</span> · ${a.kind==='sleep'?'醒来后精力满满':'认真做一件小事，也是一种长大'}</p></div><div class="activity-banner-actions"><button type="button" class="text-button" data-action="stop-activity">${a.kind==='sleep'?'叫它起床':'结束安排'}</button></div></div>`;
  }
  function schedule() {
    const lv=levelInfo();
    return heading('GROW A LITTLE EVERY DAY','小奶蛙，也有自己的日程','学一点新东西，做一份小工作，累了就好好休息。')+activeActivity()+`<div class="filter-tabs"><span class="filter-tab" aria-pressed="true">${icon('book')} 计算 ${state.skills['计算']}</span><span class="filter-tab">社交 ${state.skills['社交']}</span><span class="filter-tab">专注 ${state.skills['专注']}</span></div><div class="schedule-grid">${courses.map(c=>{const locked=(c.level&&lv.level<c.level)||(c.skillNeed&&state.skills[c.skillNeed]<c.need);return `<article class="schedule-card"><div class="schedule-top"><span class="activity-icon">${icon(c.icon)}</span><span class="schedule-meta">${c.kind==='study'?'学习':'打工'} · ${c.time} 分钟</span></div><h3>${c.name}</h3><p>${c.description}</p><div class="schedule-bottom"><span>${c.reward}</span><button type="button" class="buy-button" data-start="${c.id}" ${locked||state.activity?'disabled':''}>${locked?(c.level?`Lv.${c.level} 解锁`:`${c.skillNeed} ${c.need} 解锁`):'开始安排'}</button></div></article>`;}).join('')}</div><div class="tip-strip">${icon('moon')}<span>状态不佳时先照顾它。学习、打工和睡觉同时只能进行一项。</span></div>`;
  }
  function growth() {
    const lv=levelInfo();
    const milestones=[{level:1,title:'初次见面，请多照顾',text:'第一次喝奶、第一次洗澡，都值得记住。'},{level:4,title:'开始有了自己的小主意',text:'解锁自信叉腰 · 学会认真做小工作'},{level:5,title:'想去桌面上逛一逛',text:'解锁短距离蹦跳漫游'},{level:7,title:'陪你一起认真长大',text:'解锁专注陪伴模式'},{level:10,title:'准备好，长成更大的自己',text:'成长为标准形态 · 留下幼年纪念照'}];
    return heading('EVERY LITTLE MOMENT COUNTS','我们的成长手记','它长大的每一点，都有你的陪伴。')+`<section class="growth-hero"><img src="${art}young/confident.png" alt="叉腰站立的幼年奶蛙"><div><p class="eyebrow">YOUNG MILK FROG</p><h2>Lv.${lv.level}，世界还很新鲜</h2><p>从小小一团，到独当一面。<br>不急，我们一起慢慢来。</p><div class="meter"><span style="width:${Math.min(100,lv.current/lv.need*100)}%"></span></div><small>${lv.current} / ${lv.need} 经验 · 下一站 Lv.${Math.min(10,lv.level+1)}</small><div class="growth-facts"><div><b>4</b><small>相伴天数</small></div><div><b>${state.exp}</b><small>成长经验</small></div><div><b>Lv.10</b><small>成长为标准形态</small></div></div></div></section><section class="milestone-list"><div class="section-head"><h3>下一点小期待</h3><span class="eyebrow">MILESTONES</span></div>${milestones.map(m=>`<div class="milestone ${m.level>lv.level?'locked':''}"><span class="milestone-level">${m.level}</span><div><strong>${m.title}</strong><p>${m.text}</p></div><span class="milestone-label">${m.level<=lv.level?icon('check')+' 已解锁':icon('lock')+' 待成长'}</span></div>`).join('')}</section><div class="tip-strip">${icon('star')}<span>达到 Lv.10 后，会等你亲手开启成长仪式。标准形态外观沿用已提供的独立设定。</span></div>`;
  }
  function desktop() {
    return `<section class="desktop-scene" aria-label="桌面陪伴预览"><div class="desktop-intro"><p class="eyebrow">MILK FROG</p><h1>忙你的，我就在这里。</h1><p>鼠标移到奶蛙身边，就能照顾它。</p></div><div class="desktop-pet" id="desktop-pet"><div class="speech"></div><button type="button" class="pet-button" id="desktop-drag" aria-label="拖动或摸摸桌面奶蛙"><canvas class="pet-image frame-sprite" id="pet-canvas" width="512" height="512" role="img" aria-label="幼年奶蛙连续动画"></canvas></button><div class="desktop-tools"></div><p class="desktop-label"></p><div class="pet-menu" id="pet-menu" role="menu" aria-label="奶蛙操作" hidden></div></div><aside id="pet-panel" class="pet-panel" aria-label="奶蛙小面板" hidden></aside><div class="desktop-access">单击摸摸 · 按住拖动 · 松手回弹 · 右键打开菜单</div></section>`;
  }
  function compactStatus(){
    const lv=levelInfo();
    return `${heading('','奶蛙的小状态','幼年期 · 陪伴第 4 天')}<div class="status-list">${statusRows()}</div><section class="growth-mini"><div class="section-head"><strong>Lv.${lv.level}</strong><small>${lv.current} / ${lv.need} 经验</small></div><div class="meter"><span style="width:${Math.min(100,lv.current/lv.need*100)}%"></span></div><button type="button" class="text-button" data-page="growth">看看成长 ${icon('arrow')}</button></section>${activeActivity()}`;
  }
  function updatePetImage(name){
    const canvas=document.getElementById('pet-canvas');
    canvas.style.width=state.settings.size+'px';canvas.style.height=state.settings.size+'px';
    if(!framePlayer){
      framePlayer=new window.MilkFrogFramePlayer(canvas,{base:art+'young/animations/',catalog:window.MilkFrogAnimations,onError:error=>toast(error.message)});
      framePlayer.warm(['idle','wave','happy','drag']);
    }
    framePlayer.settings({reduced:reducedMotion(),speed:state.settings.speed||1});
    if(framePose===name&&appliedMotionEpoch===motionEpoch)return;
    framePose=name;appliedMotionEpoch=motionEpoch;canvas.dataset.pose=name;
    const epoch=motionEpoch;
    framePlayer.play(framePlayer.has(name)?name:'idle',{
      loop:state.previewOnce?false:undefined,
      onComplete:()=>{if(epoch===motionEpoch&&state.pose===name&&!pointerDragging&&!state.activity){if(afterMotion&&afterMotion.epoch===epoch){const run=afterMotion.run;afterMotion=null;run();return;}state.pose='idle';state.previewOnce=false;motionEpoch++;render();}},
    });
  }
  function animationPanel(){
    const clips=window.MilkFrogAnimations.clips;
    return `${heading('','动作点播','点一下，在桌面奶蛙身上完整播放一遍。')}<label class="motion-speed">播放速度<select id="motion-speed" aria-label="动作播放速度"><option value="0.75" ${(state.settings.speed||1)===.75?'selected':''}>0.75× 轻慢</option><option value="1" ${(state.settings.speed||1)===1?'selected':''}>1× 自然</option><option value="1.25" ${state.settings.speed===1.25?'selected':''}>1.25× 活泼</option></select></label><div class="motion-grid">${Object.entries(clips).map(([id,clip])=>`<button type="button" data-preview="${id}"><strong>${clip.label}</strong><small>${(clip.durationMs/1000).toFixed(1)} 秒</small></button>`).join('')}</div>`;
  }
  function render() {
    document.body.classList.toggle('reduced',state.settings.reduced);
    document.body.classList.toggle('no-bubbles',!state.settings.bubbles);
    if(!document.getElementById('desktop-pet')){root.innerHTML=desktop();wireDrag();}
    if(!pointerDragging)updatePetImage(state.pose);
    const speech=document.querySelector('.desktop-pet .speech');speech.textContent=state.speech;speech.hidden=Date.now()>speechUntil;
    document.querySelector('.desktop-tools').innerHTML=[['feed','milk','喂食'],['bath','bubble','洗澡'],['play','ball','玩耍'],['sleep','moon',state.activity?.kind==='sleep'?'唤醒':'睡觉'],['bag','bag','背包'],['more','leaf','更多']].map(([act,i,t])=>`<button type="button" data-action="${act}" aria-label="${t}">${icon(i)}${t}</button>`).join('');
    document.querySelector('.desktop-label').textContent=`奶蛙 · Lv.${levelInfo().level}${state.activity?' · '+state.activity.name:''}`;
    const menu=document.getElementById('pet-menu');menu.hidden=!menuOpen;
    menu.innerHTML=[['home','heart','小状态'],['schedule','calendar','学习 / 打工'],['shop','shop','商店'],['bag','bag','背包'],['growth','growth','成长记录']].map(([page,i,t])=>`<button type="button" role="menuitem" data-page="${page}">${icon(i)}${t}</button>`).join('')+`<button type="button" role="menuitem" data-action="settings">${icon('settings')}设置</button>`;
    const panel=document.getElementById('pet-panel'),scroll=panel.querySelector('.panel-content')?.scrollTop||0;
    panel.hidden=!state.panel;
    if(state.panel){
      const content=state.panel==='shop'?shopOrBag(false):state.panel==='bag'?shopOrBag(true):state.panel==='schedule'?schedule():state.panel==='growth'?growth():state.panel==='animations'?animationPanel():compactStatus();
      panel.innerHTML=`<div class="panel-chrome"><button type="button" class="text-button" data-action="more">${icon('leaf')}奶蛙</button><button type="button" class="icon-button" data-action="close-panel" aria-label="关闭小面板">×</button></div><div class="panel-content">${content}</div>`;panel.querySelector('.panel-content').scrollTop=scroll;
    }else panel.replaceChildren();
  }
  function toast(message) { const node=document.getElementById('toast');node.textContent=message;node.classList.add('visible');clearTimeout(toastTimeout);toastTimeout=setTimeout(()=>node.classList.remove('visible'),3000); }
  function pose(name,line,ms=3500,options={}){clearTimeout(poseTimeout);afterMotion=null;motionEpoch++;state.pose=name;state.previewOnce=!!options.once;state.speech=line;speechUntil=Date.now()+Math.max(ms,window.MilkFrogAnimations.clips[window.MilkFrogAnimations.aliases[name]||name]?.durationMs||0)+1500;render();}
  function earn(amount){state.exp+=amount;}
  function addMemory(text){state.diary.unshift(text);state.diary=state.diary.slice(0,5);}
  function go(page,category='all'){state.page=page;state.panel=page;state.category=category;menuOpen=false;render();document.getElementById('pet-panel').querySelector('button[aria-label="关闭小面板"]').focus();}
  async function confirmActivity(title,body){
    const d=document.getElementById('confirm-dialog');document.getElementById('confirm-title').textContent=title;document.getElementById('confirm-body').textContent=body;
    d.showModal();return new Promise(resolve=>d.addEventListener('close',()=>resolve(d.returnValue==='accept'),{once:true}));
  }
  function stopActivity(){const a=state.activity;if(!a)return; if(a.coins){const ratio=Math.max(0,Math.min(1,(Date.now()-a.startedAt)/(a.endsAt-a.startedAt)));const pay=Math.floor(a.coins*ratio*.5);state.coins+=pay;toast(`结束安排，按进度结算 ${pay} 奶豆。`);}else toast(a.kind==='sleep'?'醒来啦，伸个小懒腰。':'这次先休息一下。');state.activity=null;clearTimeout(poseTimeout);if(a.kind==='sleep')pose('wake','睡饱了，伸个小懒腰。');else {state.pose='idle';state.previewOnce=false;motionEpoch++;render();}}
  async function use(id){
    const item=items.find(i=>i.id===id);if(!item)return;
    if(afterMotion){toast('先让奶蛙醒一醒，再继续照顾它。');return;}
    if(!(state.bag[id]>0)){go('shop',item.type);toast(`先给奶蛙挑一份${item.name}吧。`);return;}
    if(item.type==='food'&&state.stats.hunger>90){toast('肚子已经圆圆的啦，晚一点再吃。');return;}
    if(item.type==='cleaning'&&state.stats.cleanliness>=95){toast('已经香香净净啦，留着下次用吧。');return;}
    const waking=state.activity?.kind==='sleep';
    if(state.activity){const accepted=await confirmActivity('先结束当前安排吗？',`奶蛙正在${state.activity.name}。使用物品会结束这次安排，打工将按进度结算部分奶豆。`);if(!accepted)return;stopActivity();}
    if(!item.permanent)state.bag[id]--;
    for(const [key,value] of Object.entries(item.stats)){state.stats[key]=Math.max(0,Math.min(100,state.stats[key]+value));}
    earn(3);addMemory(`和你一起${item.type==='food'?'吃了'+item.name:item.type==='cleaning'?'洗了个香香的澡':'玩了最喜欢的小球'}。`);
    const show=()=>{state.panel=null;menuOpen=false;pose(item.pose,item.line);toast(`${item.name} · ${item.effect} · 成长 +3`);};
    if(waking)afterMotion={epoch:motionEpoch,run:show};else show();
  }
  function buy(id){const item=items.find(i=>i.id===id);if(!item)return;if(item.permanent&&state.bag[id])return;if(state.coins<item.price){toast('奶豆有点不够，去安排一份小工作吧。');return;}state.coins-=item.price;state.bag[id]=(state.bag[id]||0)+1;render();toast(`${item.name}已放进背包 · −${item.price} 奶豆`);}
  function start(id){const c=courses.find(c=>c.id===id);if(!c||state.activity)return;if(c.level&&levelInfo().level<c.level)return;if(c.skillNeed&&state.skills[c.skillNeed]<c.need)return;if(state.stats.health<40||state.stats.energy<20){toast('先照顾好身体，再安排今天的小事。');return;}clearTimeout(poseTimeout);state.previewOnce=false;motionEpoch++;speechUntil=Date.now()+6500;state.activity={...c,startedAt:Date.now(),endsAt:Date.now()+c.time*60000};state.pose=c.kind;state.speech=c.kind==='study'?'我会认真学的，你也加油呀。':'今天也要努力赚小奶豆！';addMemory(`开始了今天的${c.name}。`);render();toast(`${c.name}已开始 · ${c.time} 分钟`);}
  function finishActivity(){const a=state.activity;if(!a)return;if(a.coins)state.coins+=a.coins;if(a.skill)state.skills[a.skill]+=a.id==='focus'?2:1;if(a.kind==='sleep')state.stats.energy=Math.min(100,state.stats.energy+20);else {state.stats.energy=Math.max(0,state.stats.energy-5);earn(5);}state.activity=null;addMemory(`完成了${a.name}，又长大了一点。`);pose(a.kind==='sleep'?'wake':'victory-v2',a.kind==='sleep'?'睡饱啦，又能陪你了。':'做完啦！你看，我是不是很棒？');toast(`${a.name}完成 · ${a.reward||'精力 +20'}`);}
  function pet(){if(state.activity?.kind==='sleep'){stopActivity();return;}if(state.activity||afterMotion||['eat','drink','pudding','snack','bath','bath-happy'].includes(state.pose)){toast('等奶蛙忙完这一小会儿，再摸摸它吧。');return;}state.stats.mood=Math.min(100,state.stats.mood+4);pose('happy','嘿嘿，被你摸摸就很开心。',1800);}
  function action(name){
    if(name==='feed')return use('milk');if(name==='bath')return use('bubble');if(name==='play')return use('ball');if(name==='pet')return pet();
    if(name==='bag')return go('bag');
    if(name==='more'){state.panel=null;menuOpen=!menuOpen;render();return;}
    if(name==='desktop'||name==='close-panel'){state.panel=null;menuOpen=false;render();document.getElementById('desktop-drag').focus();return;}
    if(name==='house')return go('home');
    if(name==='settings'){menuOpen=false;render();document.getElementById('settings-dialog').showModal();return;}
    if(name==='stop-activity'){confirmActivity('结束这次安排？','当前进度不会继续累积。打工会按已完成进度结算 50% 的收入。').then(ok=>{if(ok)stopActivity();});return;}
    if(name==='sleep'){
      if(state.activity?.kind==='sleep'){stopActivity();return;}
      if(state.activity){toast('先结束当前安排，再去睡个小觉吧。');return;}
      clearTimeout(poseTimeout);motionEpoch++;state.previewOnce=false;state.activity={kind:'sleep',name:'睡一个暖暖的小觉',startedAt:Date.now(),endsAt:Date.now()+600000};state.pose='sleep';state.speech='呼……醒来再陪你。';speechUntil=Date.now()+6000;render();toast('安安静静睡 10 分钟 · 精力 +20');
    }
  }
  document.addEventListener('click',event=>{
    const b=event.target.closest('button');if(!b||b.disabled)return;
    if(b.dataset.page)go(b.dataset.page);
    else if(b.dataset.mode){state.mode=b.dataset.mode;render();}
    else if(b.dataset.category){state.category=b.dataset.category;render();}
    else if(b.dataset.buy)buy(b.dataset.buy);
    else if(b.dataset.use)use(b.dataset.use);
    else if(b.dataset.start)start(b.dataset.start);
    else if(b.dataset.preview){if(state.activity){toast('先结束当前安排，再点播小动作吧。');return;}state.panel=null;pose(b.dataset.preview,'给你表演一个小动作。',2500,{once:true});}
    else if(b.dataset.action)action(b.dataset.action);
  });
  document.addEventListener('keydown',event=>{if(event.key==='Escape'&&!document.querySelector('dialog[open]')){state.panel=null;menuOpen=false;render();}});
  document.addEventListener('change',event=>{if(event.target.id==='motion-speed'){state.settings.speed=Number(event.target.value);framePlayer?.settings({reduced:reducedMotion(),speed:state.settings.speed});}});
  document.getElementById('reduce-motion').addEventListener('change',e=>{state.settings.reduced=e.target.checked;render();});
  document.getElementById('show-bubbles').addEventListener('change',e=>{state.settings.bubbles=e.target.checked;render();});
  document.getElementById('pet-size').addEventListener('input',e=>{state.settings.size=Number(e.target.value);const img=document.querySelector('.desktop-pet .pet-image');if(img){img.style.width=state.settings.size+'px';img.style.height=state.settings.size+'px';}});
  document.getElementById('reset-demo').addEventListener('click',()=>{clearTimeout(poseTimeout);afterMotion=null;motionEpoch++;state=initial();document.getElementById('reduce-motion').checked=false;document.getElementById('show-bubbles').checked=true;document.getElementById('pet-size').value=210;document.getElementById('settings-dialog').close();render();toast('预览数据已恢复。');});
  let desktopPosition=null;
  function wireDrag(){
    const box=document.getElementById('desktop-pet'),button=document.getElementById('desktop-drag'),scene=box.parentElement;
    if(desktopPosition){box.style.left=desktopPosition.x+'px';box.style.top=desktopPosition.y+'px';}
    let drag=null;
    button.addEventListener('pointerdown',e=>{if(e.button!==0)return;button.setPointerCapture(e.pointerId);drag={x:e.clientX,y:e.clientY,left:box.offsetLeft,top:box.offsetTop,moved:false};});
    button.addEventListener('pointermove',e=>{if(!drag)return;const dx=e.clientX-drag.x,dy=e.clientY-drag.y;if(Math.hypot(dx,dy)>5&&!drag.moved){drag.moved=true;pointerDragging=true;menuOpen=false;document.getElementById('pet-menu').hidden=true;clearTimeout(poseTimeout);afterMotion=null;state.previewOnce=false;motionEpoch++;updatePetImage('drag');}if(!drag.moved)return;box.style.left=Math.max(0,Math.min(scene.clientWidth-box.offsetWidth,drag.left+dx))+'px';box.style.top=Math.max(65,Math.min(scene.clientHeight-box.offsetHeight-30,drag.top+dy))+'px';const img=button.querySelector('.pet-image');img.style.setProperty('--drag-angle',Math.max(-14,Math.min(14,dx*.07))+'deg');});
    button.addEventListener('pointerup',()=>{if(!drag)return;const moved=drag.moved;drag=null;pointerDragging=false;if(moved){desktopPosition={x:box.offsetLeft,y:box.offsetTop};if(state.activity){state.pose=state.activity.kind;motionEpoch++;render();}else pose('land','稳稳落地啦。',1000);}else pet();});
    button.addEventListener('pointercancel',()=>{drag=null;pointerDragging=false;render();});
    button.addEventListener('contextmenu',e=>{e.preventDefault();menuOpen=!menuOpen;render();});
    button.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();pet();}});
  }
  function queueIdleAction(){setTimeout(()=>{if(!document.hidden&&!pointerDragging&&!state.activity&&!state.panel&&state.pose==='idle'&&!document.querySelector('dialog[open]')&&!reducedMotion()){const name=Math.random()<.65?'wave':'stretch';pose(name,name==='wave'?'嘿，我在这里呀。':'伸个懒腰，继续陪你。');}queueIdleAction();},24000+Math.random()*24000);}
  setInterval(()=>{const speech=document.querySelector('.desktop-pet .speech');if(speech&&Date.now()>speechUntil)speech.hidden=true;},1000);
  setInterval(()=>{if(!state.activity)return;if(Date.now()>=state.activity.endsAt)finishActivity();else document.querySelectorAll('[data-countdown]').forEach(el=>{el.textContent=remaining(state.activity);});},1000);
  render();
  queueIdleAction();
})();
